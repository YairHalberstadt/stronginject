using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;

namespace StrongInject.Generator
{
    [Generator]
    internal class IncrementalGenerator : IIncrementalGenerator
    {
        public void Initialize(IncrementalGeneratorInitializationContext context)
        {
            var diagnosticsAndSources = context.SyntaxProvider.CreateSyntaxProvider<(DiagnosticCollection? diagnostics, SourceText? source,  string? sourceHintName)>(
                (node, _) => node is ClassDeclarationSyntax,
                (ctx, cancellationToken) =>
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    if (ctx.SemanticModel.GetDeclaredSymbol(ctx.Node, cancellationToken) is not INamedTypeSymbol type)
                    {
                        return default;
                    }

                    var isContainer = type.AllInterfaces.Any(x =>
                        x.OriginalDefinition.MetadataName is WellKnownTypes.ICONTAINER_MD_NAME or WellKnownTypes.IASYNC_CONTAINER_MD_NAME);

                    cancellationToken.ThrowIfCancellationRequested();
                    if (!isContainer
                        && !type.GetAttributes().Any(x => WellKnownTypes.IsClassAttribute(x.AttributeClass))
                        && !type.GetMembers().Any(x =>
                        {
                            if (x is IFieldSymbol or IPropertySymbol && x.GetAttributes().Any(x => WellKnownTypes.IsInstanceAttribute(x.AttributeClass)))
                            {
                                return true;
                            }

                            return x is IMethodSymbol && x.GetAttributes().Any(x => WellKnownTypes.IsMethodAttribute(x.AttributeClass));
                        }))
                    {
                        return default;
                    }

                    var diagnostics = new List<Diagnostic>();
                    var reportDiagnostic = diagnostics.Add;
                    if (!type.IsInternal() && !type.IsPublic())
                    {
                        reportDiagnostic(ModuleNotPublicOrInternal(
                            type,
                            ((TypeDeclarationSyntax)ctx.Node).Identifier
                            .GetLocation()));
                    }

                    cancellationToken.ThrowIfCancellationRequested();
                    if (!WellKnownTypes.TryCreate(ctx.SemanticModel.Compilation, reportDiagnostic, out var wellKnownTypes))
                    {
                        return (new DiagnosticCollection(diagnostics), null, null);
                    }

                    var registrationCalculator = new RegistrationCalculator(ctx.SemanticModel.Compilation, wellKnownTypes, reportDiagnostic, cancellationToken);
                    if (!isContainer)
                    {
                        registrationCalculator.ValidateModuleRegistrations(type);
                        return (new DiagnosticCollection(diagnostics), null, null);
                    }

                    var file = ContainerGenerator.GenerateContainerImplementations(
                        type,
                        registrationCalculator.GetContainerRegistrations(type),
                        wellKnownTypes,
                        reportDiagnostic,
                        cancellationToken);

                    var source = CSharpSyntaxTree.ParseText(SourceText.From(file, Encoding.UTF8)).GetRoot()
                        .NormalizeWhitespace().SyntaxTree.GetText();

                    return (new DiagnosticCollection(diagnostics), source, sourceHintName: GenerateNameHint(type));
                });

            var allDiagnostics = diagnosticsAndSources.Select((x, _) => x.diagnostics)
                .Collect()
                .Select((x, _) =>
                    {
                        var diags = new HashSet<Diagnostic>(DiagnosticEqualityComparer.Instance);
                        foreach (var collection in x)
                        {
                            if (collection is not null)
                            {
                                foreach (var diag in collection.Diagnostics)
                                {
                                    diags.Add(diag);
                                }
                            }
                        }

                        return diags;
                    }
                );

            var sources = diagnosticsAndSources.Select((x, _) => (x.source, x.sourceHintName));
            context.RegisterSourceOutput(sources, (context, x) =>
            {
                if (x.source is { } source)
                {
                    context.AddSource(x.sourceHintName!, source);
                }
            });
            
            context.RegisterSourceOutput(allDiagnostics, (context, x) =>
            {
                foreach (var diag in x)
                {
                    context.ReportDiagnostic(diag);
                }
            });
        }

        private string GenerateNameHint(INamedTypeSymbol container)
        {
            var stringBuilder = new StringBuilder(container.ContainingNamespace.FullName());
            foreach (var type in container.GetContainingTypesAndThis().Reverse())
            {
                stringBuilder.Append(".");
                stringBuilder.Append(type.Name);
                if (type.TypeParameters.Length > 0)
                {
                    stringBuilder.Append("_");
                    stringBuilder.Append(type.TypeParameters.Length);
                }
            }

            stringBuilder.Append(".g.cs");
            return stringBuilder.ToString();
        }

        private Diagnostic ModuleNotPublicOrInternal(ITypeSymbol module, Location location)
        {
            return Diagnostic.Create(
                new DiagnosticDescriptor(
                    "SI0401",
                    "Module must be public or internal.",
                    "Module '{0}' must be public or internal.",
                    "StrongInject",
                    DiagnosticSeverity.Error,
                    isEnabledByDefault: true),
                location,
                module.ToDisplayString());
        }

        private class DiagnosticCollection : IEquatable<DiagnosticCollection>
        {
            public DiagnosticCollection(List<Diagnostic> diagnostics)
            {
                Diagnostics = diagnostics;
            }

            public List<Diagnostic> Diagnostics { get; }


            public bool Equals(DiagnosticCollection? other)
            {
                if (other is null)
                {
                    return false;
                }

                if (ReferenceEquals(this, other))
                {
                    return true;
                }

                if (Diagnostics.Count != other.Diagnostics.Count)
                {
                    return false;
                }

                for (int i = 0; i < Diagnostics.Count; i++)
                {
                    if (DiagnosticEqualityComparer.Instance.Equals(Diagnostics[i], other.Diagnostics[i]))
                    {
                        return false;
                    }
                }

                return true;
            }

            public override bool Equals(object? obj)
            {
                if (obj is not DiagnosticCollection other)
                {
                    return false;
                }
                return Equals(other);
            }

            public override int GetHashCode()
            {
                if (Diagnostics.Count == 0)
                {
                    return 0;
                }
                
                return (Diagnostics.Count.GetHashCode() * -1521134295 + DiagnosticEqualityComparer.Instance.GetHashCode(Diagnostics[0])) * -1521134295;
            }
        }

        private class DiagnosticEqualityComparer : IEqualityComparer<Diagnostic>
        {
            private DiagnosticEqualityComparer() {}

            public static DiagnosticEqualityComparer Instance { get; } = new();
            
            private static readonly Func<Diagnostic, IReadOnlyList<object?>> _getArguments = (Func<Diagnostic, IReadOnlyList<object?>>)Delegate.CreateDelegate(
                typeof(Func<Diagnostic, IReadOnlyList<object?>>),
                typeof(Diagnostic).GetProperty("Arguments", BindingFlags.NonPublic | BindingFlags.Instance)!.GetMethod!);
            
            public bool Equals(Diagnostic x, Diagnostic y)
            {
                if (ReferenceEquals(x, y))
                {
                    return true;
                }

                if (ReferenceEquals(x, null))
                {
                    return false;
                }

                if (ReferenceEquals(y, null))
                {
                    return false;
                }

                if (x.GetType() != y.GetType())
                {
                    return false;
                }

                return x.Id == y.Id && x.Location.Equals(y.Location) && _getArguments(x).SequenceEqual(_getArguments(y));
            }

            public int GetHashCode(Diagnostic obj)
            {
                unchecked
                {
                    return (obj.Id.GetHashCode() * 397) ^ obj.Location.GetHashCode();
                }
            }
        }
    }
}