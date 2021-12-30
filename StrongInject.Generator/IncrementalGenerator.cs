using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;

namespace StrongInject.Generator
{
    [Generator]
    internal class IncrementalGenerator : IIncrementalGenerator
    {
        public void Initialize(IncrementalGeneratorInitializationContext context)
        {
            var wellKnownTypesProviderAndDiagsProvider = context.CompilationProvider.Select((comp, _) =>
            {
                var diags = new List<Diagnostic>();
                WellKnownTypes.TryCreate(comp, diags.Add, out WellKnownTypes? wellKnownTypes);
                return (diags: diags.ToEquatable(), wellKnownTypes);
            });

            context.RegisterSourceOutput(wellKnownTypesProviderAndDiagsProvider, (ctx, wkt) =>
            {
                foreach (var diag in wkt.diags)
                {
                    ctx.ReportDiagnostic(diag);
                }

            });

            var wellKnownTypesProvider = wellKnownTypesProviderAndDiagsProvider.Select((x, _) => x.wellKnownTypes);

            var classAndMemberAttributesProvider = wellKnownTypesProvider.Select((wkt, _) =>
                (classAtts: wkt?.GetClassAttributes().ToEquatable(), memberAtts: wkt?.GetMemberAttributes().ToEquatable()));

            var modulesAndContainersProvider = context.SyntaxProvider.CreateSyntaxProvider(
                    (node, _) => node is ClassDeclarationSyntax,
                    (ctx, cancellationToken) => ctx.SemanticModel.GetDeclaredSymbol(ctx.Node, cancellationToken))
                .Where(x => x is INamedTypeSymbol)
                .Combine(wellKnownTypesProvider)
                .Select((x, _) =>
                {
                    var (symbol, wellKnownTypes) = x;
                    if (wellKnownTypes is null)
                    {
                        return default;
                    }

                    var type = (INamedTypeSymbol)symbol!;
                    var isContainer = type!.AllInterfaces.Any(x
                        => x.OriginalDefinition.Equals(wellKnownTypes.IContainer)
                           || x.OriginalDefinition.Equals(wellKnownTypes.IAsyncContainer));
                    return (type, isContainer);
                })
                .Combine(classAndMemberAttributesProvider)
                .Where(x =>
                {
                    var ((type, isContainer), (classAtts, memberAtts)) = x;
                    if (classAtts is null || memberAtts is null)
                    {
                        return false;
                    }

                    return isContainer
                           || type.GetAttributes().Any(x =>
                               x.AttributeClass is { } attribute &&
                               classAtts.Contains(attribute))
                           || type.GetMembers().Any(x => x.GetAttributes().Any(x =>
                               x.AttributeClass is { } attribute &&
                               memberAtts.Contains(attribute)));
                })
                .Select((x, _) => x.Left);

            context.RegisterSourceOutput(
                wellKnownTypesProvider
                    .Combine(context.CompilationProvider)
                    .Combine(modulesAndContainersProvider.Collect()), (context, x) =>
                {
                    var ((wellKnownTypes, compilation), modules) = x;
                    if (wellKnownTypes is null)
                    {
                        return;
                    }

                    var registrationCalculator = new RegistrationCalculator(compilation, wellKnownTypes, context.ReportDiagnostic, context.CancellationToken);
                    foreach (var (module, isContainer) in modules)
                    {
                        context.CancellationToken.ThrowIfCancellationRequested();

                        if (!module.IsInternal() && !module.IsPublic())
                        {
                            context.ReportDiagnostic(ModuleNotPublicOrInternal(
                                module,
                                ((TypeDeclarationSyntax)module.DeclaringSyntaxReferences[0].GetSyntax()).Identifier
                                .GetLocation()));
                        }

                        if (isContainer)
                        {
                            var file = ContainerGenerator.GenerateContainerImplementations(
                                module,
                                registrationCalculator.GetContainerRegistrations(module),
                                wellKnownTypes,
                                context.ReportDiagnostic,
                                context.CancellationToken);

                            var source = CSharpSyntaxTree.ParseText(SourceText.From(file, Encoding.UTF8)).GetRoot()
                                .NormalizeWhitespace().SyntaxTree.GetText();
                            context.AddSource(
                                GenerateNameHint(module),
                                source);
                        }
                        else
                        {
                            registrationCalculator.ValidateModuleRegistrations(module);
                        }
                    }
                }
            );
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
                module);
        }
    }

    internal class EquatableEnumerable<T> : IEnumerable<T>, IEquatable<EquatableEnumerable<T>>
    {
        private readonly IEnumerable<T> _underlying;

        public EquatableEnumerable(IEnumerable<T> underlying)
        {
            _underlying = underlying;
        }

        public IEnumerator<T> GetEnumerator()
        {
            return _underlying.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return ((IEnumerable)_underlying).GetEnumerator();
        }

        public bool Equals(EquatableEnumerable<T>? other)
        {
            if (ReferenceEquals(null, other))
            {
                return false;
            }

            if (ReferenceEquals(this, other))
            {
                return true;
            }

            using var enumerator = GetEnumerator();
            using var otherEnumerator = GetEnumerator();
            while (true)
            {
                var hasAny = enumerator.MoveNext();
                var otherHasAny = otherEnumerator.MoveNext();

                if (hasAny && otherHasAny)
                {
                    if (!EqualityComparer<T>.Default.Equals(enumerator.Current, otherEnumerator.Current))
                    {
                        return false;
                    }
                }
                else
                {
                    return hasAny == otherHasAny;
                }
            }
        }

        public override bool Equals(object? obj)
        {
            if (ReferenceEquals(null, obj))
            {
                return false;
            }

            if (ReferenceEquals(this, obj))
            {
                return true;
            }

            if (obj.GetType() != this.GetType())
            {
                return false;
            }

            return Equals((EquatableEnumerable<T>)obj);
        }

        public override int GetHashCode()
        {
            return _underlying.GetHashCode();
        }
    }

    internal static class EquatableEnumerable
    {
        public static EquatableEnumerable<T> ToEquatable<T>(this IEnumerable<T> @this)
            => new EquatableEnumerable<T>(@this);
    }
}