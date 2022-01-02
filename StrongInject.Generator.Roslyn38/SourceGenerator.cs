using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;

namespace StrongInject.Generator
{
    [Generator]
    internal class SourceGenerator : ISourceGenerator
    {
        public void Execute(GeneratorExecutionContext context)
        {
            var cancellationToken = context.CancellationToken;
            var compilation = context.Compilation;
            Action<Diagnostic> reportDiagnostic = context.ReportDiagnostic;
            if (!WellKnownTypes.TryCreate(compilation, reportDiagnostic, out var wellKnownTypes))
            {
                return;
            }

            var registrationCalculator =
                new RegistrationCalculator(compilation, wellKnownTypes, cancellationToken);
            
            foreach (var syntaxTree in context.Compilation.SyntaxTrees)
            {
                cancellationToken.ThrowIfCancellationRequested();

                var semanticModel = context.Compilation.GetSemanticModel(syntaxTree);
                var modules = syntaxTree.GetRoot(cancellationToken).DescendantNodesAndSelf()
                    .OfType<ClassDeclarationSyntax>()
                    .Select(x => semanticModel.GetDeclaredSymbol(x, cancellationToken))
                    .Where(x => x != null)
                    .Select(x =>
                    {
                        var isContainer = x!.AllInterfaces.Any(x
                            => x.OriginalDefinition.Equals(wellKnownTypes.IContainer)
                               || x.OriginalDefinition.Equals(wellKnownTypes.IAsyncContainer));
                        return (type: x, isContainer);
                    })
                    .Where(x =>
                        x.isContainer
                        || x.type.GetAttributes().Any(x => WellKnownTypes.IsClassAttribute(x.AttributeClass))
                        || x.type.GetMembers().Any(x =>
                        {
                            if (x is IFieldSymbol or IPropertySymbol && x.GetAttributes().Any(x => WellKnownTypes.IsInstanceAttribute(x.AttributeClass)))
                            {
                                return true;
                            }

                            return x is IMethodSymbol && x.GetAttributes().Any(x => WellKnownTypes.IsMethodAttribute(x.AttributeClass));
                        }));

                foreach (var module in modules)
                {
                    if (!module.type.IsInternal() && !module.type.IsPublic())
                    {
                        reportDiagnostic(ModuleNotPublicOrInternal(
                            module.type,
                            ((TypeDeclarationSyntax)module.type.DeclaringSyntaxReferences[0].GetSyntax()).Identifier
                            .GetLocation()));
                    }

                    cancellationToken.ThrowIfCancellationRequested();
                    if (module.isContainer)
                    {
                        var file = ContainerGenerator.GenerateContainerImplementations(
                            module.type,
                            registrationCalculator.GetContainerRegistrations(module.type, reportDiagnostic),
                            wellKnownTypes,
                            reportDiagnostic,
                            cancellationToken);

                        var source = CSharpSyntaxTree.ParseText(SourceText.From(file, Encoding.UTF8)).GetRoot()
                            .NormalizeWhitespace().SyntaxTree.GetText();
                        context.AddSource(
                            GenerateNameHint(module.type),
                            source);
                    }
                    else
                    {
                        registrationCalculator.ValidateModuleRegistrations(module.type, reportDiagnostic);
                    }
                }
            }
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

        private static Diagnostic ModuleNotPublicOrInternal(ITypeSymbol module, Location location)
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

        void ISourceGenerator.Initialize(GeneratorInitializationContext context)
        {
        }
    }
}