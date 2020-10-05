using Microsoft.CodeAnalysis;

namespace StrongInject.Generator
{
    abstract internal record DecoratorSource(int decoratedParameter, bool IsAsync)
    {
        public abstract ITypeSymbol OfType { get; }
    }

    internal record DecoratorRegistration(
        INamedTypeSymbol Type,
        ITypeSymbol DecoratedType,
        bool RequiresInitialization,
        IMethodSymbol Constructor,
        int decoratedParameter,
        bool IsAsync) : DecoratorSource(decoratedParameter, IsAsync)
    {
        public override ITypeSymbol OfType => DecoratedType;
    }

    internal record DecoratorFactoryMethod(
        IMethodSymbol Method,
        ITypeSymbol DecoratedType,
        bool IsOpenGeneric,
        int decoratedParameter,
        bool IsAsync) : DecoratorSource(decoratedParameter, IsAsync)
    {
        public override ITypeSymbol OfType => DecoratedType;
    }
}
