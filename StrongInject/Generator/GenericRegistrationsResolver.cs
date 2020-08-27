using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;

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
                    if (Matches(type, factoryMethod, out var constructedFactoryMethod, out var constraintsDoNotMatch))
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
                    if (Matches(type, factoryMethod, out var constructedFactoryMethod, out _))
                    {
                        instanceSources.Add(constructedFactoryMethod);
                    }
                }

                foreach (var childResolver in _childResolvers)
                {
                    childResolver.ResolveAll(type, instanceSources);
                }
            }

            private bool Matches(ITypeSymbol type, FactoryMethod factoryMethod, out FactoryMethod constructedFactoryMethod, out bool constraintsDoNotMatch)
            {
                if (!CanConstructFromReturnType(type, factoryMethod.Method, out var typeArguments))
                {
                    constructedFactoryMethod = null!;
                    constraintsDoNotMatch = false;
                    return false;
                }

                var typeParameters = factoryMethod.Method.TypeParameters;
                for (int i = 0; i < typeParameters.Length; i++)
                {
                    var typeParameter = typeParameters[i];
                    var typeArgument = typeArguments[i];

                    if (typeArgument.IsPointerOrFunctionPointer() || type.IsRefLikeType)
                    {
                        constructedFactoryMethod = null!;
                        constraintsDoNotMatch = false;
                        return false;
                    }

                    if (typeParameter.HasReferenceTypeConstraint && !typeArgument.IsReferenceType
                        || typeParameter.HasValueTypeConstraint && !typeArgument.IsNonNullableValueType()
                        || typeParameter.HasUnmanagedTypeConstraint && !(typeArgument.IsUnmanagedType && typeArgument.IsNonNullableValueType())
                        || typeParameter.HasConstructorConstraint && !SatisfiesConstructorConstraint(typeArgument))
                    {
                        constructedFactoryMethod = null!;
                        constraintsDoNotMatch = true;
                        return false;
                    }

                    foreach (var typeConstraint in typeParameter.ConstraintTypes)
                    {
                        var conversion = _compilation.ClassifyConversion(typeArgument, typeConstraint);
                        if (typeArgument.IsNullableType() || conversion is not ({ IsIdentity: true } or { IsImplicit: true, IsReference: true } or { IsBoxing: true }))
                        {
                            constructedFactoryMethod = null!;
                            constraintsDoNotMatch = true;
                            return false;
                        }
                    }
                }

                constructedFactoryMethod = factoryMethod with
                {
                    ReturnType = type,
                    Method = factoryMethod.Method.Construct(typeArguments),
                    IsOpenGeneric = false
                };
                constraintsDoNotMatch = false;
                return true;
            }

            private bool CanConstructFromReturnType(ITypeSymbol toConstruct, IMethodSymbol method, out ITypeSymbol[] typeArguments)
            {
                typeArguments = null!;
                return CanConstructFrom(toConstruct, method.ReturnType, method, ref typeArguments);
                static bool CanConstructFrom(ITypeSymbol toConstruct, ITypeSymbol from, IMethodSymbol method, ref ITypeSymbol[] typeArguments)
                {
                    switch (from)
                    {
                        case ITypeParameterSymbol typeParameterSymbol:

                            if (!SymbolEqualityComparer.Default.Equals(typeParameterSymbol.DeclaringMethod, method))
                            {
                                return SymbolEqualityComparer.Default.Equals(toConstruct, from);
                            }

                            var currentTypeArgumentForOrdinal = typeArguments?[typeParameterSymbol.Ordinal] ?? null;
                            if (currentTypeArgumentForOrdinal is null)
                            {
                                (typeArguments ??= new ITypeSymbol[method.TypeParameters.Length])[typeParameterSymbol.Ordinal] = toConstruct;
                                return true;
                            }
                            return SymbolEqualityComparer.Default.Equals(toConstruct, currentTypeArgumentForOrdinal);

                        case IArrayTypeSymbol { Rank: var rank, ElementType: var elementType }:

                            if (toConstruct is IArrayTypeSymbol { Rank: var toConstructRank, ElementType: var elementTypeToConstruct })
                            {
                                return rank == toConstructRank && CanConstructFrom(elementTypeToConstruct, elementType, method, ref typeArguments);
                            }
                            return false;

                        case INamedTypeSymbol { OriginalDefinition: var originalDefinition, TypeArguments: var fromTypeArguments }:

                            if (!SymbolEqualityComparer.Default.Equals(originalDefinition, toConstruct.OriginalDefinition))
                            {
                                return false;
                            }

                            var typeArgumentsToConstruct = ((INamedTypeSymbol)toConstruct).TypeArguments;

                            for (var i = 0; i < fromTypeArguments.Length; i++)
                            {
                                var typeArgument = fromTypeArguments[i];
                                var typeArgumentToConstruct = typeArgumentsToConstruct[i];

                                if (!CanConstructFrom(typeArgumentToConstruct, typeArgument, method, ref typeArguments))
                                {
                                    return false;
                                }
                            }

                            return true;
                    }

                    return false;
                }
            }

            private static bool SatisfiesConstructorConstraint(ITypeSymbol typeArgument)
            {
                switch (typeArgument.TypeKind)
                {
                    case TypeKind.Struct:
                    case TypeKind.Enum:
                    case TypeKind.Dynamic:
                        return true;

                    case TypeKind.Class:
                        return HasPublicParameterlessConstructor((INamedTypeSymbol)typeArgument) && !typeArgument.IsAbstract;

                    case TypeKind.TypeParameter:
                        {
                            var typeParameter = (ITypeParameterSymbol)typeArgument;
                            return typeParameter.HasConstructorConstraint || typeParameter.IsValueType;
                        }

                    default:
                        return false;
                }
            }

            private static bool HasPublicParameterlessConstructor(INamedTypeSymbol type)
            {
                foreach (var constructor in type.InstanceConstructors)
                {
                    if (constructor.Parameters.Length == 0)
                    {
                        return constructor.DeclaredAccessibility == Accessibility.Public;
                    }
                }
                return false;
            }
        }
    }
}
