using Microsoft.CodeAnalysis;
using System.Collections.Generic;
using System.Collections.Immutable;

namespace StrongInject.Generator
{
    abstract internal record InstanceSource(Scope Scope, bool IsAsync)
    {
        public abstract ITypeSymbol OfType { get; }
    }

    internal record Registration(
        INamedTypeSymbol Type,
        ITypeSymbol RegisteredAs,
        Scope Scope,
        bool RequiresInitialization,
        IMethodSymbol Constructor,
        bool IsAsync) : InstanceSource(Scope, IsAsync)
    {
        public override ITypeSymbol OfType => RegisteredAs;
    }
    internal record InstanceProvider(
        ITypeSymbol ProvidedType,
        IFieldSymbol InstanceProviderField,
        INamedTypeSymbol CastTo,
        bool IsAsync) : InstanceSource(Scope.InstancePerResolution, IsAsync)
    {
        public override ITypeSymbol OfType => ProvidedType;
    }
    internal record FactoryRegistration(
        ITypeSymbol FactoryType,
        ITypeSymbol FactoryOf,
        Scope Scope,
        bool IsAsync) : InstanceSource(Scope, IsAsync)
    {
        public override ITypeSymbol OfType => FactoryOf;
    }
    internal record DelegateSource(
        ITypeSymbol DelegateType,
        ITypeSymbol ReturnType,
        ImmutableArray<IParameterSymbol> Parameters,
        bool IsAsync) : InstanceSource(Scope.InstancePerResolution, IsAsync: IsAsync)
    {
        public override ITypeSymbol OfType => ReturnType;
    }
    internal record DelegateParameter(IParameterSymbol Parameter, string Name) : InstanceSource(Scope.InstancePerResolution, IsAsync: false)
    {
        public override ITypeSymbol OfType => Parameter.Type;
    }
    internal record FactoryMethod(
        IMethodSymbol Method,
        ITypeSymbol ReturnType,
        Scope Scope,
        bool IsOpenGeneric,
        bool IsAsync) : InstanceSource(Scope, IsAsync)
    {
        public override ITypeSymbol OfType => ReturnType;
    }
    internal record InstanceFieldOrProperty(ISymbol FieldOrPropertySymbol, ITypeSymbol Type) : InstanceSource(Scope.InstancePerDependency, IsAsync: false)
    {
        public override ITypeSymbol OfType => Type;
    }
    internal record ArraySource(IArrayTypeSymbol ArrayType, ITypeSymbol ElementType, IReadOnlyCollection<InstanceSource> Items) : InstanceSource(Scope.InstancePerDependency, IsAsync: false)
    {
        public override ITypeSymbol OfType => ArrayType;
    }
}
