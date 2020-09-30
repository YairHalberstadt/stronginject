using Microsoft.CodeAnalysis;
using System.Collections.Generic;
using System.Collections.Immutable;

namespace StrongInject.Generator
{
    abstract internal record InstanceSource(Scope Scope, bool IsAsync, bool CanDecorate)
    {
        public abstract ITypeSymbol OfType { get; }
    }

    internal record Registration(
        INamedTypeSymbol Type,
        Scope Scope,
        bool RequiresInitialization,
        IMethodSymbol Constructor,
        bool IsAsync) : InstanceSource(Scope, IsAsync, CanDecorate: true)
    {
        public override ITypeSymbol OfType => Type;
    }
    internal record FactorySource(ITypeSymbol FactoryOf, InstanceSource Underlying, Scope Scope, bool IsAsync) : InstanceSource(Scope, IsAsync, Underlying.CanDecorate)
    {
        public override ITypeSymbol OfType => FactoryOf;
    }
    internal record DelegateSource(
        ITypeSymbol DelegateType,
        ITypeSymbol ReturnType,
        ImmutableArray<IParameterSymbol> Parameters,
        bool IsAsync) : InstanceSource(Scope.InstancePerResolution, IsAsync: IsAsync, CanDecorate: true)
    {
        public override ITypeSymbol OfType => DelegateType;
    }
    internal record DelegateParameter(IParameterSymbol Parameter, string Name) : InstanceSource(Scope.InstancePerResolution, IsAsync: false, CanDecorate: false)
    {
        public override ITypeSymbol OfType => Parameter.Type;
    }
    internal record FactoryMethod(
        IMethodSymbol Method,
        ITypeSymbol ReturnType,
        Scope Scope,
        bool IsOpenGeneric,
        bool IsAsync) : InstanceSource(Scope, IsAsync, CanDecorate: true)
    {
        public override ITypeSymbol OfType => ReturnType;
    }
    internal record InstanceFieldOrProperty(ISymbol FieldOrPropertySymbol, ITypeSymbol Type) : InstanceSource(Scope.SingleInstance, IsAsync: false, CanDecorate: true)
    {
        public override ITypeSymbol OfType => Type;
    }
    internal record ArraySource(
        IArrayTypeSymbol ArrayType,
        ITypeSymbol ElementType,
        IReadOnlyCollection<InstanceSource> Items) : InstanceSource(Scope.InstancePerDependency, IsAsync: false, CanDecorate: true)
    {
        public override ITypeSymbol OfType => ArrayType;
    }
    internal record WrappedDecoratorInstanceSource(DecoratorSource Decorator, InstanceSource Underlying) : InstanceSource(Underlying.Scope, Decorator.IsAsync, CanDecorate: true)
    {
        public override ITypeSymbol OfType => Decorator.OfType;
    }
    internal record ForwardedInstanceSource(ITypeSymbol AsType, InstanceSource Underlying) : InstanceSource(Underlying.Scope, IsAsync: false, Underlying.CanDecorate)
    {
        public override ITypeSymbol OfType => AsType;
        public static ForwardedInstanceSource Create(ITypeSymbol AsType, InstanceSource Underlying)
            => new ForwardedInstanceSource(AsType, Underlying is ForwardedInstanceSource forwardedUnderlying ? forwardedUnderlying.Underlying : Underlying);
    }
}
