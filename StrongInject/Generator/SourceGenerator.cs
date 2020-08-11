using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;

namespace StrongInject.Generator
{
    [Generator]
    internal class SourceGenerator : ISourceGenerator
    {
        public void Execute(SourceGeneratorContext context)
        {
            try
            {
                ExecuteInternal(context);
            }
            catch (Exception e)
            {
                //This is temporary till https://github.com/dotnet/roslyn/issues/46084 is fixed
                context.ReportDiagnostic(Diagnostic.Create(
                    new DiagnosticDescriptor(
                        "SI0000",
                        "An exception was thrown by the StrongInject generator",
                        "An exception was thrown by the StrongInject generator: '{0}'",
                        "StrongInject",
                        DiagnosticSeverity.Error,
                        isEnabledByDefault: true),
                    Location.None,
                    e.ToString()));
            }
        }

        //By not inlining we make sure we can catch assembly loading errors when jitting this method
        [MethodImpl(MethodImplOptions.NoInlining)]
        private void ExecuteInternal(SourceGeneratorContext context)
        {
            var cancellationToken = context.CancellationToken;
            var compilation = context.Compilation;
            Action<Diagnostic> reportDiagnostic = context.ReportDiagnostic;
            if (!WellKnownTypes.TryCreate(compilation, reportDiagnostic, out var wellKnownTypes))
            {
                return;
            }

            var registrationCalculator = new RegistrationCalculator(compilation, reportDiagnostic, cancellationToken);

            foreach (var syntaxTree in context.Compilation.SyntaxTrees)
            {
                cancellationToken.ThrowIfCancellationRequested();

                var semanticModel = context.Compilation.GetSemanticModel(syntaxTree);
                var modules = syntaxTree.GetRoot(cancellationToken).DescendantNodesAndSelf().OfType<ClassDeclarationSyntax>()
                    .Select(x => semanticModel.GetDeclaredSymbol(x, cancellationToken))
                    .OfType<INamedTypeSymbol>()
                    .Select(x =>
                    {
                        var isContainer = x.AllInterfaces.Any(x
                            => x.OriginalDefinition.Equals(wellKnownTypes.iContainer, SymbolEqualityComparer.Default)
                            || x.OriginalDefinition.Equals(wellKnownTypes.iAsyncContainer, SymbolEqualityComparer.Default));
                        return (type: x, isContainer);
                    })
                    .Where(x =>
                        x.isContainer
                        || x.type.GetAttributes().Any(x =>
                            x.AttributeClass is { } attribute &&
                            (attribute.Equals(wellKnownTypes.registrationAttribute, SymbolEqualityComparer.Default)
                            || attribute.Equals(wellKnownTypes.moduleRegistrationAttribute, SymbolEqualityComparer.Default)
                            || attribute.Equals(wellKnownTypes.factoryRegistrationAttribute, SymbolEqualityComparer.Default))));

                foreach (var module in modules)
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    // do this even if not a container to report diagnostics
                    var registrations = registrationCalculator.GetRegistrations(module.type);

                    if (module.isContainer)
                    {
                        var file = ContainerGenerator.GenerateContainerImplementations(
                            compilation,
                            module.type,
                            registrations,
                            wellKnownTypes,
                            reportDiagnostic,
                            cancellationToken);

                        var source = CSharpSyntaxTree.ParseText(SourceText.From(file, Encoding.UTF8)).GetRoot().NormalizeWhitespace().SyntaxTree.GetText();
                        context.AddSource(
                            GenerateNameHint(module.type),
                            source);
                    }
                };
            }
        }

        private string GenerateNameHint(INamedTypeSymbol container)
        {
            var stringBuilder = new StringBuilder(container.ContainingNamespace.FullName());
            foreach (var type in container.GetContainingTypesAndThis().Reverse())
            {
                stringBuilder.Append(type.Name);
                if (type.TypeParameters.Length > 0)
                {
                    stringBuilder.Append("_");
                    stringBuilder.Append(type.TypeParameters.Length);
                }
            }
            stringBuilder.Append(".generated.cs");
            return stringBuilder.ToString();
        }

        public void Initialize(InitializationContext context)
        {
        }

        private class InstanceSourceComparer : IEqualityComparer<InstanceSource>
        {
            private InstanceSourceComparer() { }

            public static InstanceSourceComparer Instance { get; } = new InstanceSourceComparer();

            public bool Equals(InstanceSource x, InstanceSource y)
            {
                return (x, y) switch
                {
                    (null, null) => true,
                    ({ scope: Scope.InstancePerDependency }, _) => false,
                    (Registration rX, Registration rY) => rX.scope == rY.scope && rX.type.Equals(rY.type, SymbolEqualityComparer.Default),
                    (FactoryRegistration fX, FactoryRegistration fY) => fX.scope == fY.scope && fX.factoryType.Equals(fY.factoryType, SymbolEqualityComparer.Default),
                    (InstanceProvider iX, InstanceProvider iY) => iX.providedType.Equals(iY.providedType, SymbolEqualityComparer.Default),
                    (DelegateSource dX, DelegateSource dY) => dX.delegateType.Equals(dY.delegateType, SymbolEqualityComparer.Default),
                    (DelegateParameter dX, DelegateParameter dY) => dX.parameter.Equals(dY.parameter, SymbolEqualityComparer.Default),
                    _ => false,
                };
            }

            public int GetHashCode(InstanceSource obj)
            {
                return obj switch
                {
                    null => 0,
                    { scope: Scope.InstancePerDependency } => new Random().Next(),
                    Registration r => 5 + r.scope.GetHashCode() * 17 + r.type.GetHashCode(),
                    InstanceProvider i => 7 + i.instanceProviderField.GetHashCode(),
                    FactoryRegistration f => 13 + f.scope.GetHashCode() * 17 + f.factoryType.GetHashCode(),
                    DelegateSource d => 17 + d.delegateType.GetHashCode(),
                    DelegateParameter dp => 19 + dp.parameter.GetHashCode(),
                    _ => throw new InvalidOperationException("This location is thought to be unreachable"),
                };
            }
        }
    }
}
