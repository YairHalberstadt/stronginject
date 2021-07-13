using Microsoft.CodeAnalysis;
using System;
using System.Collections.Generic;
using System.Linq;
using static StrongInject.Generator.GenericResolutionHelpers;

namespace StrongInject.Generator
{
    internal class GenericDecoratorsResolver
    {
        private readonly Dictionary<INamedTypeSymbol, List<DecoratorSource>> _namedTypeDecoratorSources;
        private readonly List<DecoratorSource> _arrayDecoratorSources;
        private readonly List<DecoratorSource> _typeParameterDecoratorSources;
        private readonly Compilation _compilation;

        public GenericDecoratorsResolver(Compilation compilation, IEnumerable<DecoratorSource> decoratorFactoryMethods)
        {
            _namedTypeDecoratorSources = new Dictionary<INamedTypeSymbol, List<DecoratorSource>>(SymbolEqualityComparer.Default);
            _arrayDecoratorSources = new List<DecoratorSource>();
            _typeParameterDecoratorSources = new List<DecoratorSource>();

            foreach(var decoratorFactoryMethod in decoratorFactoryMethods)
            {
                var list = decoratorFactoryMethod.OfType switch
                {
                    INamedTypeSymbol namedTypeSymbol => _namedTypeDecoratorSources.GetOrCreate(namedTypeSymbol.OriginalDefinition, _ => new()),
                    IArrayTypeSymbol => _arrayDecoratorSources,
                    ITypeParameterSymbol => _typeParameterDecoratorSources,
                    var typeSymbol => throw new InvalidOperationException($"Unexpected TypeSymbol {typeSymbol}"),
                };
                list.Add(decoratorFactoryMethod);
            }
            _compilation = compilation;
        }

        public IEnumerable<DecoratorSource> ResolveDecorators(ITypeSymbol type)
        {
            if (type is INamedTypeSymbol namedType)
            {
                if (_namedTypeDecoratorSources.TryGetValue(namedType.OriginalDefinition, out var decoratorSources))
                {
                    foreach (var source in decoratorSources)
                    {
                        switch (source)
                        {
                            case DecoratorFactoryMethod decoratorFactoryMethod:
                                if (CanConstructFromGenericFactoryMethod(type, decoratorFactoryMethod,
                                    out var constructedFactoryMethod))
                                {
                                    yield return constructedFactoryMethod;
                                }

                                break;
                            case DecoratorRegistration decoratorRegistration:
                                var constructed = decoratorRegistration.Type.Construct(namedType.TypeArguments.ToArray());
                                var originalConstructor = decoratorRegistration.Constructor;
                                var constructor = constructed.InstanceConstructors.First(
                                    x => SymbolEqualityComparer.Default.Equals(x.OriginalDefinition, originalConstructor));

                                yield return decoratorRegistration with
                                {
                                    Constructor = constructor,
                                    Type = constructed,
                                    DecoratedType = namedType,
                                };

                                break;
                            default:
                                throw new NotImplementedException(source.ToString());
                        }
                    }
                }
            }
            else if (type is IArrayTypeSymbol)
            {
                foreach (var factory in _arrayDecoratorSources)
                {
                    if (CanConstructFromGenericFactoryMethod(type, (DecoratorFactoryMethod)factory, out var constructedFactoryMethod))
                    {
                        yield return constructedFactoryMethod;
                    }
                }
            }

            foreach (var factory in _typeParameterDecoratorSources)
            {
                if (CanConstructFromGenericFactoryMethod(type, (DecoratorFactoryMethod)factory, out var constructedFactoryMethod))
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
