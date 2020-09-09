using Microsoft.CodeAnalysis;
using System.Collections.Generic;
using System.Collections.Immutable;

namespace StrongInject.Generator
{
    abstract internal record InstanceSource(Scope Scope, bool IsAsync)
    {
        public abstract ITypeSymbol OfType { get; }
        public abstract bool CanDecorate { get; }
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
        public override bool CanDecorate => true;
    }
    internal record InstanceProvider(
        ITypeSymbol ProvidedType,
        IFieldSymbol InstanceProviderField,
        INamedTypeSymbol CastTo,
        bool IsAsync) : InstanceSource(Scope.InstancePerResolution, IsAsync)
    {
        public override ITypeSymbol OfType => ProvidedType;
        public override bool CanDecorate => true;
    }
    internal record FactoryRegistration(
        ITypeSymbol FactoryType,
        ITypeSymbol FactoryOf,
        Scope Scope,
        bool IsAsync) : InstanceSource(Scope, IsAsync)
    {
        public override ITypeSymbol OfType => FactoryOf;
        public override bool CanDecorate => true;
    }
    internal record DelegateSource(
        ITypeSymbol DelegateType,
        ITypeSymbol ReturnType,
        ImmutableArray<IParameterSymbol> Parameters,
        bool IsAsync) : InstanceSource(Scope.InstancePerResolution, IsAsync: IsAsync)
    {
        public override ITypeSymbol OfType => DelegateType;
        public override bool CanDecorate => true;
    }
    internal record DelegateParameter(IParameterSymbol Parameter, string Name) : InstanceSource(Scope.InstancePerResolution, IsAsync: false)
    {
        public override ITypeSymbol OfType => Parameter.Type;
        public override bool CanDecorate => false;
    }
    internal record FactoryMethod(
        IMethodSymbol Method,
        ITypeSymbol ReturnType,
        Scope Scope,
        bool IsOpenGeneric,
        bool IsAsync) : InstanceSource(Scope, IsAsync)
    {
        public override ITypeSymbol OfType => ReturnType;
        public override bool CanDecorate => true;
    }
    internal record InstanceFieldOrProperty(ISymbol FieldOrPropertySymbol, ITypeSymbol Type) : InstanceSource(Scope.SingleInstance, IsAsync: false)
    {
        public override ITypeSymbol OfType => Type;
        public override bool CanDecorate => true;
    }
    internal record ArraySource(IArrayTypeSymbol ArrayType, ITypeSymbol ElementType, IReadOnlyCollection<InstanceSource> Items) : InstanceSource(Scope.InstancePerDependency, IsAsync: false)
    {
        public override ITypeSymbol OfType => ArrayType;
        public override bool CanDecorate => true;
    }
    internal record WrappedDecoratorInstanceSource(DecoratorSource Decorator, InstanceSource Underlying) : InstanceSource(Underlying.Scope, Decorator.IsAsync)
    {
        public override ITypeSymbol OfType => Decorator.OfType;
        public override bool CanDecorate => true;
    }
}
