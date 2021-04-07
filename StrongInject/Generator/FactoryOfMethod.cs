using Microsoft.CodeAnalysis;

namespace StrongInject.Generator
{
    internal record FactoryOfMethod(FactoryMethod Underlying, ITypeSymbol FactoryOfType){}
}
