using Microsoft.CodeAnalysis;
using System.Collections.Generic;
using System.Collections.Immutable;

namespace StrongInject.Generator
{
    abstract internal record InstanceSource(Scope scope, bool isAsync)
    {
        public abstract ITypeSymbol OfType { get; }
    }

    internal record Registration(
        INamedTypeSymbol type,
        ITypeSymbol registeredAs,
        Scope scope,
        bool requiresInitialization,
        IMethodSymbol constructor,
        bool isAsync) : InstanceSource(scope, isAsync)
    {
        public override ITypeSymbol OfType => registeredAs; 
    }
    internal record InstanceProvider(
        ITypeSymbol providedType,
        IFieldSymbol instanceProviderField,
        INamedTypeSymbol castTo,
        bool isAsync) : InstanceSource(Scope.InstancePerResolution, isAsync)
    {
        public override ITypeSymbol OfType => providedType;
    }
    internal record FactoryRegistration(
        ITypeSymbol factoryType,
        ITypeSymbol factoryOf,
        Scope scope,
        bool isAsync) : InstanceSource(scope, isAsync)
    {
        public override ITypeSymbol OfType => factoryOf;
    }
    internal record DelegateSource(
        ITypeSymbol delegateType,
        ITypeSymbol returnType,
        ImmutableArray<IParameterSymbol> parameters,
        bool isAsync) : InstanceSource(Scope.InstancePerResolution, isAsync: isAsync)
    {
        public override ITypeSymbol OfType => returnType;
    }
    internal record DelegateParameter(IParameterSymbol parameter, string name) : InstanceSource(Scope.InstancePerResolution, isAsync: false)
    {
        public override ITypeSymbol OfType => parameter.Type;
    }
    internal record FactoryMethod(
        IMethodSymbol method,
        ITypeSymbol returnType,
        Scope scope,
        bool isAsync) : InstanceSource(scope, isAsync)
    {
        public override ITypeSymbol OfType => returnType;
    }
    internal record InstanceFieldOrProperty(ISymbol fieldOrPropertySymbol, ITypeSymbol type) : InstanceSource(Scope.InstancePerDependency, isAsync: false)
    {
        public override ITypeSymbol OfType => type;
    }
    internal record ArraySource(IArrayTypeSymbol arrayType, ITypeSymbol elementType, IReadOnlyCollection<InstanceSource> items) : InstanceSource(Scope.InstancePerDependency, isAsync: false)
    {
        public override ITypeSymbol OfType => arrayType;
    }
}
