using Microsoft.CodeAnalysis;
using System;
using System.Collections.Generic;
using static StrongInject.Generator.GenericResolutionHelpers;

namespace StrongInject.Generator
{
    internal class GenericDecoratorsResolver
    {
        private readonly Dictionary<INamedTypeSymbol, List<DecoratorFactoryMethod>> _namedTypeDecoratorFactories;
        private readonly List<DecoratorFactoryMethod> _arrayDecoratorFactories;
        private readonly List<DecoratorFactoryMethod> _typeParameterDecoratorFactories;
        private readonly Compilation _compilation;

        public GenericDecoratorsResolver(Compilation compilation, IEnumerable<DecoratorFactoryMethod> decoratorFactoryMethods)
        {
            _namedTypeDecoratorFactories = new Dictionary<INamedTypeSymbol, List<DecoratorFactoryMethod>>(SymbolEqualityComparer.Default);
            _arrayDecoratorFactories = new List<DecoratorFactoryMethod>();
            _typeParameterDecoratorFactories = new List<DecoratorFactoryMethod>();

            foreach(var decoratorFactoryMethod in decoratorFactoryMethods)
            {
                var list = decoratorFactoryMethod.DecoratedType switch
                {
                    INamedTypeSymbol namedTypeSymbol => _namedTypeDecoratorFactories.GetOrCreate(namedTypeSymbol.OriginalDefinition, _ => new()),
                    IArrayTypeSymbol => _arrayDecoratorFactories,
                    ITypeParameterSymbol => _typeParameterDecoratorFactories,
                    var typeSymbol => throw new InvalidOperationException($"Unexpected TypeSymbol {typeSymbol}"),
                };
                list.Add(decoratorFactoryMethod);
            }
            _compilation = compilation;
        }

        public IEnumerable<DecoratorFactoryMethod> ResolveDecorators(ITypeSymbol type)
        {
            if (type is INamedTypeSymbol namedType)
            {
                if (_namedTypeDecoratorFactories.TryGetValue(namedType.OriginalDefinition, out var decoratorFactories))
                {
                    foreach (var factory in decoratorFactories)
                    {
                        if (CanConstructFromGenericFactoryMethod(type, factory, out var constructedFactoryMethod))
                        {
                            yield return constructedFactoryMethod;
                        }
                    }
                }
            }
            else if (type is IArrayTypeSymbol)
            {
                foreach (var factory in _arrayDecoratorFactories)
                {
                    if (CanConstructFromGenericFactoryMethod(type, factory, out var constructedFactoryMethod))
                    {
                        yield return constructedFactoryMethod;
                    }
                }
            }

            foreach (var factory in _typeParameterDecoratorFactories)
            {
                if (CanConstructFromGenericFactoryMethod(type, factory, out var constructedFactoryMethod))
                {
                    yield return constructedFactoryMethod;
                }
            }
        }

        private bool CanConstructFromGenericFactoryMethod(ITypeSymbol toConstruct, DecoratorFactoryMethod factoryMethod, out DecoratorFactoryMethod constructedFactoryMethod)
        {
            if (!CanConstructFromGenericMethodReturnType(_compilation, toConstruct, factoryMethod.DecoratedType, factoryMethod.Method, out var constructedMethod, out _))
            {
                constructedFactoryMethod = null!;
                return false;
            }

            constructedFactoryMethod = factoryMethod with
            {
                DecoratedType = toConstruct,
                Method = constructedMethod,
                IsOpenGeneric = false
            };
            return true;
        }
    }
}
