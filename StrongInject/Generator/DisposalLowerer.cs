using Microsoft.CodeAnalysis;
using System;
using System.Linq;

namespace StrongInject.Generator
{
    internal class DisposalLowerer
    {
        private readonly bool _disposeAsynchronously;
        private readonly WellKnownTypes _wellKnownTypes;
        private readonly Action<Diagnostic> _reportDiagnostic;
        private readonly Location _containerDeclarationLocation;

        public DisposalLowerer(bool disposeAsynchronously, WellKnownTypes wellKnownTypes, Action<Diagnostic> reportDiagnostic, Location containerDeclarationLocation)
        {
            _disposeAsynchronously = disposeAsynchronously;
            _wellKnownTypes = wellKnownTypes;
            _reportDiagnostic = reportDiagnostic;
            _containerDeclarationLocation = containerDeclarationLocation;
        }

        public Operation AddDisposal(Statement statement)
        {
            var disposal = statement switch
            {
                DelegateCreationStatement { InternalOperations: var ops, DisposeActionsName: var disposeActionsName } => _disposeAsynchronously
                    ? ops.Any(x => x.Disposal is not null)
                        ? new Disposal.DelegateDisposal(disposeActionsName, _wellKnownTypes.ConcurrentBagOfFuncTask, IsAsync: true)
                        : null
                    : ops.Any(x => x.Disposal is { IsAsync: false })
                        ? new Disposal.DelegateDisposal(disposeActionsName, _wellKnownTypes.ConcurrentBagOfAction, IsAsync: false)
                        : null,
                DependencyCreationStatement { VariableName: var variableName, Source: var source, Dependencies: var dependencies } =>
                    source switch
                    {
                        FactorySource { IsAsync: var isAsync } => new Disposal.FactoryDisposal(variableName, dependencies[0]!, isAsync),
                        FactoryMethod { FactoryOfType: var type } => ExactTypeNotKnown(type, variableName),
                        Registration { Type: var type } => ExactTypeKnown(type, variableName),
                        WrappedDecoratorInstanceSource { Decorator: { dispose: var dispose } decorator } => dispose
                            ? decorator switch
                            {
                                DecoratorFactoryMethod { DecoratedType: var type } => ExactTypeNotKnown(type, variableName),
                                DecoratorRegistration { Type: var type } => ExactTypeKnown(type, variableName),
                                _ => throw new NotImplementedException(decorator.GetType().ToString()),
                            } 
                            : null,
                        DelegateParameter or InstanceFieldOrProperty or ArraySource or ForwardedInstanceSource => null,
                        _ => throw new NotImplementedException(source.GetType().ToString()),
                    },
                SingleInstanceReferenceStatement or InitializationStatement or DisposeActionsCreationStatement => null,
                _ => throw new NotImplementedException(statement.GetType().ToString()),
            };

            return new Operation(statement, disposal);

            Disposal? ExactTypeNotKnown(ITypeSymbol subTypeOf, string variableName)
            {
                if ((subTypeOf.IsSealed || subTypeOf.IsValueType) && subTypeOf.TypeKind != TypeKind.TypeParameter)
                {
                    return ExactTypeKnown(subTypeOf, variableName);
                }
                return new Disposal.DisposalHelpers(variableName, _disposeAsynchronously);
            }

            Disposal? ExactTypeKnown(ITypeSymbol type, string variableName)
            {
                var isAsyncDisposable = type.AllInterfaces.Contains(_wellKnownTypes.IAsyncDisposable);

                if (isAsyncDisposable && _disposeAsynchronously)
                    return new Disposal.IDisposable(variableName, IsAsync: true);

                if (type.AllInterfaces.Contains(_wellKnownTypes.IDisposable))
                    return new Disposal.IDisposable(variableName, IsAsync: false);

                if (isAsyncDisposable)
                {
                    _reportDiagnostic(WarnIAsyncDisposableInSynchronousResolution(
                        type,
                        _containerDeclarationLocation));
                }

                return null;
            }
        }

        private static Diagnostic WarnIAsyncDisposableInSynchronousResolution(ITypeSymbol type, Location location)
        {
            return Diagnostic.Create(
                new DiagnosticDescriptor(
                    "SI1301",
                    "Cannot call asynchronous dispose for Type in implementation of synchronous container",
                    "Cannot call asynchronous dispose for '{0}' in implementation of synchronous container",
                    "StrongInject",
                    DiagnosticSeverity.Warning,
                    isEnabledByDefault: true),
                location,
                type);
        }
    }
}
