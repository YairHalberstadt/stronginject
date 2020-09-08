using Microsoft.CodeAnalysis;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using static StrongInject.Generator.GenericResolutionHelpers;

namespace StrongInject.Generator
{
    internal class GenericRegistrationsResolver
    {
        private readonly Dictionary<INamedTypeSymbol, Bucket> _namedTypeBuckets;
        private readonly Bucket _arrayTypeBucket;
        private readonly Bucket _typeParameterBuckets;

        private GenericRegistrationsResolver(Dictionary<INamedTypeSymbol, Bucket> namedTypeBuckets, Bucket arrayTypeBucket, Bucket typeParameterBuckets)
        {
            _namedTypeBuckets = namedTypeBuckets;
            _arrayTypeBucket = arrayTypeBucket;
            _typeParameterBuckets = typeParameterBuckets;
        }

        public bool TryResolve(ITypeSymbol type, out FactoryMethod instanceSource, out bool isAmbiguous, out IEnumerable<FactoryMethod> sourcesNotMatchingConstraints)
        {
            instanceSource = null!;
            sourcesNotMatchingConstraints = Array.Empty<FactoryMethod>();
            if (type is INamedTypeSymbol namedType)
            {
                if (_namedTypeBuckets.TryGetValue(namedType.OriginalDefinition, out var bucket))
                {
                    if (!bucket.TryResolve(type, out instanceSource, out isAmbiguous, out sourcesNotMatchingConstraints))
                    {
                        if (isAmbiguous)
                            return false;
                    }
                }
            }
            else if (type is IArrayTypeSymbol)
            {
                if (!_arrayTypeBucket.TryResolve(type, out instanceSource, out isAmbiguous, out sourcesNotMatchingConstraints))
                {
                    if (isAmbiguous)
                        return false;
                }
            }

            if (_typeParameterBuckets.TryResolve(type, out var typeParamInstanceSource, out isAmbiguous, out var typeParamSourcesNotMatchingConstraints))
            {
                if (isAmbiguous)
                    return false;
            }

            if (typeParamInstanceSource is null && instanceSource is null)
            {
                sourcesNotMatchingConstraints = sourcesNotMatchingConstraints.Concat(typeParamSourcesNotMatchingConstraints);
                return false;
            }

            if (typeParamInstanceSource is null)
            {
                return true;
            }

            if (instanceSource is null)
            {
                sourcesNotMatchingConstraints = Array.Empty<FactoryMethod>();
                instanceSource = typeParamInstanceSource;
                return true;
            }

            sourcesNotMatchingConstraints = Array.Empty<FactoryMethod>();
            instanceSource = null!;
            isAmbiguous = true;
            return false;
        }

        public IEnumerable<FactoryMethod> ResolveAll(ITypeSymbol type)
        {
            var instanceSources = new HashSet<FactoryMethod>();

            if (type is INamedTypeSymbol namedType)
            {
                if (_namedTypeBuckets.TryGetValue(namedType.OriginalDefinition, out var bucket))
                {
                    bucket.ResolveAll(type, instanceSources);
                }
            }
            else if (type is IArrayTypeSymbol)
            {
                _arrayTypeBucket.ResolveAll(type, instanceSources);
            }
            _typeParameterBuckets.ResolveAll(type, instanceSources);

            return instanceSources;
        }

        public class Builder
        {
            private readonly List<Builder> _children = new();
            private readonly List<FactoryMethod> _factoryMethods = new();

            public void Add(Builder child) => _children.Add(child);
            public void Add(FactoryMethod factoryMethod) => _factoryMethods.Add(factoryMethod);

            public GenericRegistrationsResolver Build(Compilation compilation)
            {
                var (namedTypeBuckets, arrayTypeBucket, typeParameterBucket) = Partition(this, compilation);
                return new(namedTypeBuckets, arrayTypeBucket, typeParameterBucket);

                static (Dictionary<INamedTypeSymbol, Bucket> namedTypeBuckets, Bucket arrayTypeBucket, Bucket typeParameterBucket) Partition(Builder builder, Compilation compilation)
                {
                    Dictionary<INamedTypeSymbol, (List<Bucket>? buckets, ImmutableArray<FactoryMethod>.Builder? factoryMethods)> namedTypeBucketsAndFactoryMethods = new();
                    List<Bucket>? arrayTypeBuckets = null;
                    List<Bucket>? typeParameterBuckets = null;
                    ImmutableArray<FactoryMethod>.Builder? arrayTypeFactoryMethods = null;
                    ImmutableArray<FactoryMethod>.Builder? typeParameterFactoryMethods = null;

                    foreach (var (childNamedTypeBuckets, childArrayTypeBucket, childTypeParameterBucket) in builder._children.Select(x => Partition(x, compilation)))
                    {
                        foreach (var (namedType, bucket) in childNamedTypeBuckets)
                        {
                            namedTypeBucketsAndFactoryMethods.CreateOrUpdate(
                                namedType,
                                bucket,
                                static (_, b) => (new List<Bucket> { b }, null),
                                static (_, b, l) =>
                                {
                                    l.buckets!.Add(b);
                                    return l;
                                });
                        }
                        (arrayTypeBuckets ??= new()).Add(childArrayTypeBucket);
                        (typeParameterBuckets ??= new()).Add(childTypeParameterBucket);
                    }

                    foreach (var factoryMethod in builder._factoryMethods)
                    {
                        switch (factoryMethod.ReturnType)
                        {
                            case INamedTypeSymbol namedType:
                                {
                                    namedTypeBucketsAndFactoryMethods.CreateOrUpdate(
                                        namedType.OriginalDefinition,
                                        factoryMethod,
                                        static (_, f) =>
                                        {
                                            var builder = ImmutableArray.CreateBuilder<FactoryMethod>();
                                            builder.Add(f);
                                            return (null, builder);
                                        },
                                        static (_, f, l) =>
                                        {
                                            (l.factoryMethods ??= ImmutableArray.CreateBuilder<FactoryMethod>()).Add(f);
                                            return l;
                                        });
                                    break;
                                }

                            case IArrayTypeSymbol:
                                (arrayTypeFactoryMethods ??= ImmutableArray.CreateBuilder<FactoryMethod>()).Add(factoryMethod);
                                break;

                            case ITypeParameterSymbol:
                                (typeParameterFactoryMethods ??= ImmutableArray.CreateBuilder<FactoryMethod>()).Add(factoryMethod);
                                break;
                        }
                    }

                    return
                    (
                        namedTypeBucketsAndFactoryMethods.ToDictionary(
                            x => x.Key,
                            x => new Bucket(
                                x.Value.buckets ?? Enumerable.Empty<Bucket>(),
                                x.Value.factoryMethods?.ToImmutable() ?? ImmutableArray<FactoryMethod>.Empty,
                                compilation)),
                        new Bucket(
                            arrayTypeBuckets ?? Enumerable.Empty<Bucket>(),
                            arrayTypeFactoryMethods?.ToImmutable() ?? ImmutableArray<FactoryMethod>.Empty,
                            compilation),
                        new Bucket(
                            typeParameterBuckets ?? Enumerable.Empty<Bucket>(),
                            typeParameterFactoryMethods?.ToImmutable() ?? ImmutableArray<FactoryMethod>.Empty,
                            compilation)
                    );
                }
            }
        }

        private class Bucket
        {
            public Bucket(IEnumerable<Bucket> childResolvers, ImmutableArray<FactoryMethod> factoryMethods, Compilation compilation)
            {
                var builder = ImmutableArray.CreateBuilder<Bucket>();
                foreach (var childResolver in childResolvers)
                {
                    if (childResolver._factoryMethods.IsDefaultOrEmpty)
                    {
                        builder.AddRange(childResolver._childResolvers);
                    }
                    else
                    {
                        builder.Add(childResolver);
                    }
                }
                _childResolvers = builder.ToImmutable();
                _factoryMethods = factoryMethods;
                _compilation = compilation;
            }

            private ImmutableArray<Bucket> _childResolvers;
            private ImmutableArray<FactoryMethod> _factoryMethods;
            private readonly Compilation _compilation;

            public bool TryResolve(ITypeSymbol type, out FactoryMethod instanceSource, out bool isAmbiguous, out IEnumerable<FactoryMethod> sourcesNotMatchingConstraints)
            {
                instanceSource = null!;
                List<FactoryMethod>? factoriesWhereConstraintsDoNotMatch = null;
                foreach (var factoryMethod in _factoryMethods)
                {
                    if (CanConstructFromGenericFactoryMethod(type, factoryMethod, out var constructedFactoryMethod, out var constraintsDoNotMatch))
                    {
                        if (instanceSource is null)
                        {
                            instanceSource = constructedFactoryMethod;
                        }
                        else if (!instanceSource.Equals(constructedFactoryMethod))
                        {
                            instanceSource = null!;
                            isAmbiguous = true;
                            sourcesNotMatchingConstraints = Enumerable.Empty<FactoryMethod>();
                            return false;
                        }
                    }
                    else if (constraintsDoNotMatch)
                    {
                        (factoriesWhereConstraintsDoNotMatch ??= new()).Add(factoryMethod);
                    }
                }

                if (instanceSource is not null)
                {
                    isAmbiguous = false;
                    sourcesNotMatchingConstraints = Array.Empty<FactoryMethod>();
                    return true;
                }

                foreach (var childResolver in _childResolvers)
                {
                    if (childResolver.TryResolve(type, out var childInstanceSource, out var isChildAmbiguous, out var childSourcesNotMatchingConstraints))
                    {
                        if (instanceSource is null)
                        {
                            instanceSource = childInstanceSource;
                        }
                        else if (!instanceSource.Equals(childInstanceSource))
                        {
                            instanceSource = null!;
                            isAmbiguous = true;
                            sourcesNotMatchingConstraints = Enumerable.Empty<FactoryMethod>();
                            return false;
                        }
                    }
                    else if (isChildAmbiguous)
                    {
                        instanceSource = null!;
                        isAmbiguous = true;
                        sourcesNotMatchingConstraints = Enumerable.Empty<FactoryMethod>();
                        return false;
                    }
                    (factoriesWhereConstraintsDoNotMatch ??= new()).AddRange(childSourcesNotMatchingConstraints);
                }

                if (instanceSource is not null)
                {
                    isAmbiguous = false;
                    sourcesNotMatchingConstraints = Array.Empty<FactoryMethod>();
                    return true;
                }

                instanceSource = null!;
                isAmbiguous = false;
                sourcesNotMatchingConstraints = factoriesWhereConstraintsDoNotMatch ?? Enumerable.Empty<FactoryMethod>();
                return false;
            }

            public void ResolveAll(ITypeSymbol type, HashSet<FactoryMethod> instanceSources)
            {
                foreach (var factoryMethod in _factoryMethods)
                {
                    if (CanConstructFromGenericFactoryMethod(type, factoryMethod, out var constructedFactoryMethod, out _))
                    {
                        instanceSources.Add(constructedFactoryMethod);
                    }
                }

                foreach (var childResolver in _childResolvers)
                {
                    childResolver.ResolveAll(type, instanceSources);
                }
            }

            private bool CanConstructFromGenericFactoryMethod(ITypeSymbol toConstruct, FactoryMethod factoryMethod, out FactoryMethod constructedFactoryMethod, out bool constraintsDoNotMatch)
            {
                if (!CanConstructFromGenericMethodReturnType(_compilation, toConstruct, factoryMethod.Method, out var constructedMethod, out constraintsDoNotMatch))
                {
                    constructedFactoryMethod = null!;
                    return false;
                }

                constructedFactoryMethod = factoryMethod with
                {
                    ReturnType = toConstruct,
                    Method = constructedMethod,
                    IsOpenGeneric = false
                };
                constraintsDoNotMatch = false;
                return true;
            }
        }
    }
}
