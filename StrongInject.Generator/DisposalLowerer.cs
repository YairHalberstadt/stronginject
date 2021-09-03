using Microsoft.CodeAnalysis;
using System;
using System.Linq;

namespace StrongInject.Generator
{
    internal class DisposalLowerer
    {
        private readonly DisposalStyle _disposalStyle;
        private readonly WellKnownTypes _wellKnownTypes;
        private readonly Action<Diagnostic> _reportDiagnostic;
        private readonly Location _containerDeclarationLocation;

        public DisposalLowerer(DisposalStyle disposalStyle, WellKnownTypes wellKnownTypes, Action<Diagnostic> reportDiagnostic, Location containerDeclarationLocation)
        {
            _disposalStyle = disposalStyle;
            _wellKnownTypes = wellKnownTypes;
            _reportDiagnostic = reportDiagnostic;
            _containerDeclarationLocation = containerDeclarationLocation;
        }

        public DisposalLowerer WithDisposalStyle(DisposalStyle disposalStyle)
        {
            return new(disposalStyle, _wellKnownTypes, _reportDiagnostic, _containerDeclarationLocation);
        }

        public Disposal? CreateDisposal(Statement statement, string variableToDisposeName)
        {
            return statement switch
            {
                DelegateCreationStatement { InternalOperations: var ops, DisposeActionsName: var disposeActionsName } => _disposalStyle.IsAsync
                    ? ops.Any(x => x.Disposal is not null)
                        ? new Disposal.DelegateDisposal(disposeActionsName, _wellKnownTypes.ConcurrentBagOfFuncTask, IsAsync: true)
                        : null
                    : ops.Any(x => x.Disposal is { IsAsync: false })
                        ? new Disposal.DelegateDisposal(disposeActionsName, _wellKnownTypes.ConcurrentBagOfAction, IsAsync: false)
                        : null,
                DependencyCreationStatement { Source: var source, Dependencies: var dependencies } =>
                    source switch
                    {
                        FactorySource { IsAsync: var isAsync } => new Disposal.FactoryDisposal(variableToDisposeName, dependencies[0]!, isAsync),
                        FactoryMethod { FactoryOfType: var type } => ExactTypeNotKnown(type, variableToDisposeName),
                        Registration { Type: var type } => ExactTypeKnown(type, variableToDisposeName),
                        WrappedDecoratorInstanceSource { Decorator: { Dispose: var dispose } decorator } => dispose
                            ? decorator switch
                            {
                                DecoratorFactoryMethod { DecoratedType: var type } => ExactTypeNotKnown(type, variableToDisposeName),
                                DecoratorRegistration { Type: var type } => ExactTypeKnown(type, variableToDisposeName),
                                _ => throw new NotImplementedException(decorator.GetType().ToString()),
                            } 
                            : null,
                        DelegateParameter or InstanceFieldOrProperty or ArraySource or ForwardedInstanceSource => null,
                        _ => throw new NotImplementedException(source.GetType().ToString()),
                    },
                SingleInstanceReferenceStatement or InitializationStatement or DisposeActionsCreationStatement or AwaitStatement or OwnedCreationStatement => null,
                _ => throw new NotImplementedException(statement.GetType().ToString()),
            };

            Disposal? ExactTypeNotKnown(ITypeSymbol subTypeOf, string variableName)
            {
                if ((subTypeOf.IsSealed || subTypeOf.IsValueType) && subTypeOf.TypeKind != TypeKind.TypeParameter)
                {
                    return ExactTypeKnown(subTypeOf, variableName);
                }
                return new Disposal.DisposalHelpers(variableName, _disposalStyle.IsAsync);
            }

            Disposal? ExactTypeKnown(ITypeSymbol type, string variableName)
            {
                var isAsyncDisposable = type.AllInterfaces.Contains(_wellKnownTypes.IAsyncDisposable);

                if (isAsyncDisposable && _disposalStyle.IsAsync)
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

        private Diagnostic WarnIAsyncDisposableInSynchronousResolution(ITypeSymbol type, Location location)
        {
            return _disposalStyle.Determinant switch
            {
                DisposalStyleDeterminant.Container => Diagnostic.Create(
                    new DiagnosticDescriptor(
                        "SI1301",
                        "Cannot call asynchronous dispose for Type in implementation of synchronous container",
                        "Cannot call asynchronous dispose for '{0}' in implementation of synchronous container",
                        "StrongInject",
                        DiagnosticSeverity.Warning,
                        isEnabledByDefault: true),
                    location,
                    type),

                DisposalStyleDeterminant.OwnedType => Diagnostic.Create(
                    new DiagnosticDescriptor(
                        "SI1301",
                        "Cannot call asynchronous dispose for Type using 'Owned<T>'; use 'AsyncOwned<T>' instead",
                        "Cannot call asynchronous dispose for '{0}' using '{1}'; use '{2}' instead",
                        "StrongInject",
                        DiagnosticSeverity.Warning,
                        isEnabledByDefault: true),
                    location,
                    type,
                    _wellKnownTypes.Owned.Construct(type),
                    _wellKnownTypes.AsyncOwned.Construct(type)),

                _ => throw new NotImplementedException(),
            };
        }
    }
}
