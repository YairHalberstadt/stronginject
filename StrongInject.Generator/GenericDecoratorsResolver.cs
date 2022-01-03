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
            _namedTypeDecoratorSources = new Dictionary<INamedTypeSymbol, List<DecoratorSource>>();
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

        public ResolveDecoratorsEnumerable ResolveDecorators(ITypeSymbol type)
        {
            return new ResolveDecoratorsEnumerable(_compilation, _namedTypeDecoratorSources, _arrayDecoratorSources, _typeParameterDecoratorSources, type);
        }

        public readonly struct ResolveDecoratorsEnumerable
        {
            private readonly Compilation _compilation;
            private readonly Dictionary<INamedTypeSymbol, List<DecoratorSource>> _namedTypeDecoratorSources;
            private readonly List<DecoratorSource> _arrayDecoratorSources;
            private readonly List<DecoratorSource> _typeParameterDecoratorSources;
            private readonly ITypeSymbol _type;

            public ResolveDecoratorsEnumerable(Compilation compilation, Dictionary<INamedTypeSymbol, List<DecoratorSource>> namedTypeDecoratorSources, List<DecoratorSource> arrayDecoratorSources, List<DecoratorSource> typeParameterDecoratorSources, ITypeSymbol type)
            {
                _compilation = compilation;
                _namedTypeDecoratorSources = namedTypeDecoratorSources;
                _arrayDecoratorSources = arrayDecoratorSources;
                _typeParameterDecoratorSources = typeParameterDecoratorSources;
                _type = type;
            }

            public ResolveDecoratorsEnumerator GetEnumerator()
                => new ResolveDecoratorsEnumerator(_compilation, _namedTypeDecoratorSources, _arrayDecoratorSources, _typeParameterDecoratorSources, _type);
        }

        public struct ResolveDecoratorsEnumerator
        {
            private static readonly List<DecoratorSource> _emptyDecoratorSources = new List<DecoratorSource>();

            private readonly Compilation _compilation;
            private readonly ITypeSymbol _type;

            private List<DecoratorSource>.Enumerator _decoratorSources;
            private List<DecoratorSource>.Enumerator _arrayDecoratorSources;
            private List<DecoratorSource>.Enumerator _typeParameterDecoratorSources;
            private DecoratorSource _current;

            public ResolveDecoratorsEnumerator(Compilation compilation, Dictionary<INamedTypeSymbol, List<DecoratorSource>> namedTypeDecoratorSources, List<DecoratorSource> arrayDecoratorSources, List<DecoratorSource> typeParameterDecoratorSources, ITypeSymbol type)
            {
                _compilation = compilation;
                _type = type;

                _arrayDecoratorSources = arrayDecoratorSources.GetEnumerator();
                _typeParameterDecoratorSources = typeParameterDecoratorSources.GetEnumerator();

                if (type is INamedTypeSymbol namedType
                    && namedTypeDecoratorSources.TryGetValue(namedType.OriginalDefinition, out var decoratorSources))
                {
                    _decoratorSources = decoratorSources.GetEnumerator();
                }
                else
                {
                    _decoratorSources = _emptyDecoratorSources.GetEnumerator();
                }

                _current = null!;
            }

            public DecoratorSource Current => _current;

            public bool MoveNext()
            {
                if (_type is INamedTypeSymbol namedType)
                {
                    while (_decoratorSources.MoveNext())
                    {
                        switch (_decoratorSources.Current)
                        {
                            case DecoratorFactoryMethod decoratorFactoryMethod:
                                if (CanConstructFromGenericFactoryMethod(_compilation, _type, decoratorFactoryMethod,
                                    out var constructedFactoryMethod))
                                {
                                    _current = constructedFactoryMethod;
                                    return true;
                                }

                                break;

                            case DecoratorRegistration decoratorRegistration:
                                var constructed = decoratorRegistration.Type.Construct(namedType.TypeArguments.ToArray());
                                var originalConstructor = decoratorRegistration.Constructor;
                                var constructor = constructed.InstanceConstructors.First(
                                    x => x.OriginalDefinition.Equals(originalConstructor));

                                _current = decoratorRegistration with
                                {
                                    Constructor = constructor,
                                    Type = constructed,
                                    DecoratedType = namedType,
                                };

                                return true;

                            case var source:
                                throw new NotImplementedException(source.ToString());

                            default:
                                throw new InvalidOperationException("This case is not reachable. It exists only because Codacy doesn't understand 'case var x'.");
                        }
                    }
                }
                else if (_type is IArrayTypeSymbol)
                {
                    while (_arrayDecoratorSources.MoveNext())
                    {
                        if (CanConstructFromGenericFactoryMethod(_compilation, _type, (DecoratorFactoryMethod)_arrayDecoratorSources.Current, out var constructedFactoryMethod))
                        {
                            _current = constructedFactoryMethod;
                            return true;
                        }
                    }
                }

                while (_typeParameterDecoratorSources.MoveNext())
                {
                    if (CanConstructFromGenericFactoryMethod(_compilation, _type, (DecoratorFactoryMethod)_typeParameterDecoratorSources.Current, out var constructedFactoryMethod))
                    {
                        _current = constructedFactoryMethod;
                        return true;
                    }
                }

                return false;
            }
        }

        private static bool CanConstructFromGenericFactoryMethod(Compilation compilation, ITypeSymbol toConstruct, DecoratorFactoryMethod factoryMethod, out DecoratorFactoryMethod constructedFactoryMethod)
        {
            if (!CanConstructFromGenericMethodReturnType(compilation, toConstruct, factoryMethod.DecoratedType, factoryMethod.Method, out var constructedMethod, out _))
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
