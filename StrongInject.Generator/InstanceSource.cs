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

    internal sealed record Registration(
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

        public bool Equals(Registration? other)
        {
            return other is not null && Scope == other.Scope && SymbolEqualityComparer.Default.Equals(Type, other.Type);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                return (SymbolEqualityComparer.Default.GetHashCode(Type) * -1521134295
                        + Scope.GetHashCode()) * -1521134295;
            }
        }
    }
    
    internal sealed record FactorySource(ITypeSymbol FactoryOf, InstanceSource Underlying, Scope Scope, bool IsAsync) : InstanceSource(Scope, IsAsync, Underlying.CanDecorate)
    {
        public override ITypeSymbol OfType => FactoryOf;

        public override void Visit<TState>(IVisitor<TState> visitor, TState state)
        {
            visitor.Visit(this, state);
        }
        
        public bool Equals(FactorySource? other)
        {
            return other is not null && Scope == other.Scope && SymbolEqualityComparer.Default.Equals(FactoryOf, other.FactoryOf);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                return (SymbolEqualityComparer.Default.GetHashCode(FactoryOf) * -1521134295
                        + Scope.GetHashCode()) * -1521134295;
            }
        }
    }
    
    internal sealed record DelegateSource(
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

        public bool Equals(DelegateSource? other)
        {
            return other is not null && SymbolEqualityComparer.Default.Equals(DelegateType, other.DelegateType);
        }

        public override int GetHashCode()
        {
            return SymbolEqualityComparer.Default.GetHashCode(DelegateType);
        }
    }
    
    internal sealed record DelegateParameter(IParameterSymbol Parameter, string Name, int Depth) : InstanceSource(Scope.InstancePerResolution, IsAsync: false, CanDecorate: false)
    {
        public override ITypeSymbol OfType => Parameter.Type;

        public override void Visit<TState>(IVisitor<TState> visitor, TState state)
        {
            visitor.Visit(this, state);
        }
    }
    
    internal sealed record FactoryMethod(
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
        
        public bool Equals(FactoryMethod? other)
        {
            return other is not null && Scope == other.Scope && SymbolEqualityComparer.Default.Equals(Method, other.Method);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                return (SymbolEqualityComparer.Default.GetHashCode(Method) * -1521134295
                        + Scope.GetHashCode()) * -1521134295;
            }
        }
    }
    
    internal sealed record InstanceFieldOrProperty(ISymbol FieldOrPropertySymbol, ITypeSymbol Type) : InstanceSource(Scope.SingleInstance, IsAsync: false, CanDecorate: true)
    {
        public override ITypeSymbol OfType => Type;

        public override void Visit<TState>(IVisitor<TState> visitor, TState state)
        {
            visitor.Visit(this, state);
        }
        
        public bool Equals(InstanceFieldOrProperty? other)
        {
            return other is not null && SymbolEqualityComparer.Default.Equals(FieldOrPropertySymbol, other.FieldOrPropertySymbol);
        }

        public override int GetHashCode()
        {
            return SymbolEqualityComparer.Default.GetHashCode(FieldOrPropertySymbol);
        }
    }
    
    internal sealed record ArraySource(
        IArrayTypeSymbol ArrayType,
        ITypeSymbol ElementType,
        IReadOnlyCollection<InstanceSource> Items) : InstanceSource(Scope.InstancePerDependency, IsAsync: false, CanDecorate: true)
    {
        public override ITypeSymbol OfType => ArrayType;

        public override void Visit<TState>(IVisitor<TState> visitor, TState state)
        {
            visitor.Visit(this, state);
        }
        
        public bool Equals(ArraySource? other)
        {
            return other is not null && SymbolEqualityComparer.Default.Equals(ArrayType, other.ArrayType);
        }

        public override int GetHashCode()
        {
            return SymbolEqualityComparer.Default.GetHashCode(ArrayType);
        }
    }
    
    internal sealed record WrappedDecoratorInstanceSource(DecoratorSource Decorator, InstanceSource Underlying) : InstanceSource(Underlying.Scope, Decorator.IsAsync, CanDecorate: true)
    {
        public override ITypeSymbol OfType => Decorator.OfType;

        public override void Visit<TState>(IVisitor<TState> visitor, TState state)
        {
            visitor.Visit(this, state);
        }
    }
    
    internal sealed record ForwardedInstanceSource : InstanceSource
    {
        private ForwardedInstanceSource(INamedTypeSymbol asType, InstanceSource underlying) : base(underlying.Scope, IsAsync: false, underlying.CanDecorate)
            => (AsType, Underlying) = (asType, underlying);

        public void Deconstruct(out INamedTypeSymbol AsType, out InstanceSource Underlying) => (AsType, Underlying) = (this.AsType, this.Underlying);

        public INamedTypeSymbol AsType { get; init; }
        public InstanceSource Underlying { get; init; }

        public override ITypeSymbol OfType => AsType;

        public static InstanceSource Create(INamedTypeSymbol asType, InstanceSource underlying)
            => SymbolEqualityComparer.Default.Equals(underlying.OfType, asType)
                ? underlying
                : new ForwardedInstanceSource(asType, underlying is ForwardedInstanceSource forwardedUnderlying ? forwardedUnderlying.Underlying : underlying);

        public override void Visit<TState>(IVisitor<TState> visitor, TState state)
        {
            visitor.Visit(this, state);
        }
    }
    
    internal sealed record OwnedSource(
        ITypeSymbol OwnedType,
        ITypeSymbol OwnedValueType,
        bool IsAsync) : InstanceSource(Scope.InstancePerDependency, IsAsync: IsAsync, CanDecorate: true)
    {
        public override ITypeSymbol OfType => OwnedType;

        public override void Visit<TState>(IVisitor<TState> visitor, TState state)
        {
            visitor.Visit(this, state);
        }
        
        public bool Equals(OwnedSource? other)
        {
            return other is not null && SymbolEqualityComparer.Default.Equals(OwnedType, other.OwnedType);
        }

        public override int GetHashCode()
        {
            return SymbolEqualityComparer.Default.GetHashCode(OwnedType);
        }
    }
}
