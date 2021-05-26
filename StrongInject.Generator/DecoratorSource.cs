using Microsoft.CodeAnalysis;

namespace StrongInject.Generator
{
    abstract internal record DecoratorSource(int decoratedParameter, bool dispose, bool IsAsync)
    {
        public abstract ITypeSymbol OfType { get; }
    }

    internal record DecoratorRegistration(
        INamedTypeSymbol Type,
        ITypeSymbol DecoratedType,
        bool RequiresInitialization,
        IMethodSymbol Constructor,
        int decoratedParameter,
        bool dispose,
        bool IsAsync) : DecoratorSource(decoratedParameter, dispose, IsAsync)
    {
        public override ITypeSymbol OfType => DecoratedType;
    }

    internal record DecoratorFactoryMethod(
        IMethodSymbol Method,
        ITypeSymbol DecoratedType,
        bool IsOpenGeneric,
        int decoratedParameter,
        bool dispose,
        bool IsAsync) : DecoratorSource(decoratedParameter, dispose, IsAsync)
    {
        public override ITypeSymbol OfType => DecoratedType;
    }
}
