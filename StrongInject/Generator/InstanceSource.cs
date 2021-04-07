using Microsoft.CodeAnalysis;
using StrongInject.Generator.Visitors;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;

namespace StrongInject.Generator
{
    abstract internal record InstanceSource(Scope Scope, bool IsAsync, bool CanDecorate)
    {
        public abstract ITypeSymbol OfType { get; }
        public abstract void Visit<TState>(IVisitor<TState> visitor, TState state);
    }

    internal record Registration(
        INamedTypeSymbol Type,
        Scope Scope,
        bool RequiresInitialization,
        IMethodSymbol Constructor,
        bool IsAsync) : InstanceSource(Scope, IsAsync, CanDecorate: true)
    {
        public override ITypeSymbol OfType => Type;

        public override void Visit<TState>(IVisitor<TState> visitor, TState state)
        {
            visitor.Visit(this, state);
        }
    }
    internal record FactorySource(ITypeSymbol FactoryOf, InstanceSource Underlying, Scope Scope, bool IsAsync) : InstanceSource(Scope, IsAsync, Underlying.CanDecorate)
    {
        public override ITypeSymbol OfType => FactoryOf;

        public override void Visit<TState>(IVisitor<TState> visitor, TState state)
        {
            visitor.Visit(this, state);
        }
    }
    internal record DelegateSource(
        ITypeSymbol DelegateType,
        ITypeSymbol ReturnType,
        ImmutableArray<IParameterSymbol> Parameters,
        bool IsAsync) : InstanceSource(Scope.InstancePerResolution, IsAsync: IsAsync, CanDecorate: true)
    {
        public override ITypeSymbol OfType => DelegateType;

        public override void Visit<TState>(IVisitor<TState> visitor, TState state)
        {
            visitor.Visit(this, state);
        }
    }
    internal record DelegateParameter(IParameterSymbol Parameter, string Name) : InstanceSource(Scope.InstancePerResolution, IsAsync: false, CanDecorate: false)
    {
        public override ITypeSymbol OfType => Parameter.Type;

        public override void Visit<TState>(IVisitor<TState> visitor, TState state)
        {
            visitor.Visit(this, state);
        }
    }
    internal record FactoryMethod(
        IMethodSymbol Method,
        ITypeSymbol FactoryOfType,
        Scope Scope,
        bool IsOpenGeneric,
        bool IsAsync) : InstanceSource(Scope, IsAsync, CanDecorate: true)
    {
        public override ITypeSymbol OfType => FactoryOfType;

        public override void Visit<TState>(IVisitor<TState> visitor, TState state)
        {
            visitor.Visit(this, state);
        }
    }
    internal record InstanceFieldOrProperty(ISymbol FieldOrPropertySymbol, ITypeSymbol Type) : InstanceSource(Scope.SingleInstance, IsAsync: false, CanDecorate: true)
    {
        public override ITypeSymbol OfType => Type;

        public override void Visit<TState>(IVisitor<TState> visitor, TState state)
        {
            visitor.Visit(this, state);
        }
    }
    internal record ArraySource(
        IArrayTypeSymbol ArrayType,
        ITypeSymbol ElementType,
        IReadOnlyCollection<InstanceSource> Items) : InstanceSource(Scope.InstancePerDependency, IsAsync: false, CanDecorate: true)
    {
        public override ITypeSymbol OfType => ArrayType;

        public override void Visit<TState>(IVisitor<TState> visitor, TState state)
        {
            visitor.Visit(this, state);
        }
    }
    internal record WrappedDecoratorInstanceSource(DecoratorSource Decorator, InstanceSource Underlying) : InstanceSource(Underlying.Scope, Decorator.IsAsync, CanDecorate: true)
    {
        public override ITypeSymbol OfType => Decorator.OfType;

        public override void Visit<TState>(IVisitor<TState> visitor, TState state)
        {
            visitor.Visit(this, state);
        }
    }
    internal record ForwardedInstanceSource : InstanceSource
    {
        private ForwardedInstanceSource(ITypeSymbol asType, InstanceSource underlying) : base(underlying.Scope, IsAsync: false, underlying.CanDecorate)
            => (AsType, Underlying) = (asType, underlying);

        public void Deconstruct(out ITypeSymbol AsType, out InstanceSource Underlying) => (AsType, Underlying) = (this.AsType, this.Underlying);

        public ITypeSymbol AsType { get; init; }
        public InstanceSource Underlying { get; init; }

        public override ITypeSymbol OfType => AsType;

        public static InstanceSource Create(ITypeSymbol asType, InstanceSource underlying)
            => SymbolEqualityComparer.Default.Equals(underlying.OfType, asType)
                ? underlying
                : new ForwardedInstanceSource(asType, underlying is ForwardedInstanceSource forwardedUnderlying ? forwardedUnderlying.Underlying : underlying);

        public override void Visit<TState>(IVisitor<TState> visitor, TState state)
        {
            visitor.Visit(this, state);
        }
    }
}
