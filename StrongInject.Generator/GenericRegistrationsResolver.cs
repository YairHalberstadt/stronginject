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

        private readonly Bucket _otherTypesBucket;
        private readonly Bucket _typeParameterBuckets;

        private GenericRegistrationsResolver(Dictionary<INamedTypeSymbol, Bucket> namedTypeBuckets, Bucket otherTypesBucket, Bucket typeParameterBuckets)
        {
            _namedTypeBuckets = namedTypeBuckets;
            _otherTypesBucket = otherTypesBucket;
            _typeParameterBuckets = typeParameterBuckets;
        }

        public bool TryResolve(ITypeSymbol type, out InstanceSource instanceSource, out bool isAmbiguous, out IEnumerable<InstanceSource> sourcesNotMatchingConstraints)
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
            else if (type is IArrayTypeSymbol or IPointerTypeSymbol or IFunctionPointerTypeSymbol)
            {
                if (!_otherTypesBucket.TryResolve(type, out instanceSource, out isAmbiguous, out sourcesNotMatchingConstraints))
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
                sourcesNotMatchingConstraints = ConcatIfNotEmptyArray(sourcesNotMatchingConstraints, typeParamSourcesNotMatchingConstraints);
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

        private static IEnumerable<T> ConcatIfNotEmptyArray<T>(IEnumerable<T> first, IEnumerable<T> second)
        {
            if (first is T[] { Length: 0 })
                return second;

            if (second is T[] { Length: 0 })
                return first;

            return first.Concat(second);
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
            else if (type is IArrayTypeSymbol or IFunctionPointerTypeSymbol or IPointerTypeSymbol)
            {
                _otherTypesBucket.ResolveAll(type, instanceSources);
            }
            _typeParameterBuckets.ResolveAll(type, instanceSources);

            return instanceSources;
        }

        public class Builder
        {
            private readonly List<Builder> _children = new();
            private readonly List<FactoryMethod> _factoryMethods = new();
            private readonly List<FactoryOfMethod> _factoryOfMethods = new();
            private readonly List<Registration> _registrations = new();
            private readonly List<ForwardedInstanceSource> _forwardedInstanceSources = new();

            public void Add(Builder child) => _children.Add(child);
            public void Add(FactoryMethod factoryMethod) => _factoryMethods.Add(factoryMethod);
            public void Add(FactoryOfMethod factoryOfMethod) => _factoryOfMethods.Add(factoryOfMethod);
            public void Add(Registration registration) => _registrations.Add(registration);
            public void Add(ForwardedInstanceSource forwardedInstanceSource) => _forwardedInstanceSources.Add(forwardedInstanceSource);

            public GenericRegistrationsResolver Build(Compilation compilation)
            {
                var (namedTypeBuckets, otherTypesBucket, typeParameterBucket) = Partition(this, compilation);
                return new(namedTypeBuckets,
                    otherTypesBucket ?? new BucketBuilder().Build(compilation),
                    typeParameterBucket ?? new BucketBuilder().Build(compilation));

                static (Dictionary<INamedTypeSymbol, Bucket> namedTypeBuckets, Bucket? otherTypesBucket, Bucket? typeParameterBucket) Partition(Builder builder, Compilation compilation)
                {
                    Dictionary<INamedTypeSymbol, BucketBuilder> namedTypeBucketBuilders = new();
                    BucketBuilder? otherTypesBucketBuilder = null;
                    BucketBuilder? typeParameterBucketBuilder = null;

                    foreach (var (childNamedTypeBuckets, childOtherTypesBucket, childTypeParameterBucket) in builder._children.Select(x => Partition(x, compilation)))
                    {
                        foreach (var (namedType, bucket) in childNamedTypeBuckets)
                        {
                            namedTypeBucketBuilders.GetOrCreate(
                                namedType,
                                static _ => new BucketBuilder()).Add(bucket);
                        }
                        if (childOtherTypesBucket != null)
                        {
                            (otherTypesBucketBuilder ??= new()).Add(childOtherTypesBucket);
                        }

                        if (childTypeParameterBucket != null)
                        {
                            (typeParameterBucketBuilder ??= new()).Add(childTypeParameterBucket);
                        }
                    }

                    foreach (var factoryMethod in builder._factoryMethods)
                    {
                        switch (factoryMethod.FactoryOfType)
                        {
                            case INamedTypeSymbol namedType:
                                {
                                    namedTypeBucketBuilders.GetOrCreate(
                                        namedType.OriginalDefinition,
                                        static _ => new BucketBuilder()).Add(factoryMethod);
                                    break;
                                }

                            case IArrayTypeSymbol or IFunctionPointerTypeSymbol or IPointerTypeSymbol:
                                (otherTypesBucketBuilder ??= new()).Add(factoryMethod);
                                break;
                            case ITypeParameterSymbol:
                                (typeParameterBucketBuilder ??= new()).Add(factoryMethod);
                                break;
                            case var type: throw new NotImplementedException(type.ToString());
                        }
                    }

                    foreach (var factoryOfMethod in builder._factoryOfMethods)
                    {
                        if (factoryOfMethod.FactoryOfType is not INamedTypeSymbol type)
                            throw new InvalidOperationException("This location is thought to be unreachable");

                        namedTypeBucketBuilders.GetOrCreate(
                            type.OriginalDefinition,
                            static _ => new BucketBuilder()).Add(factoryOfMethod);
                    }
                    
                    foreach (var registration in builder._registrations)
                    {
                        namedTypeBucketBuilders.GetOrCreate(
                            registration.Type.OriginalDefinition,
                            static _ => new BucketBuilder()).Add(registration);
                    }
                    
                    foreach (var forwardedInstanceSource in builder._forwardedInstanceSources)
                    {
                        namedTypeBucketBuilders.GetOrCreate(
                            forwardedInstanceSource.AsType.OriginalDefinition,
                            static _ => new BucketBuilder()).Add(forwardedInstanceSource);
                    }

                    return
                    (
                        namedTypeBucketBuilders.ToDictionary(
                            x => x.Key,
                            x => x.Value.Build(compilation)),
                        otherTypesBucketBuilder?.Build(compilation),
                        typeParameterBucketBuilder?.Build(compilation)
                    );
                }
            }

            private class BucketBuilder
            {
                private List<Bucket>? _buckets;
                private ImmutableArray<FactoryMethod>.Builder? _factoryMethods;
                private ImmutableArray<FactoryOfMethod>.Builder? _factoryOfMethods;
                private ImmutableArray<Registration>.Builder? _registrations;
                private ImmutableArray<ForwardedInstanceSource>.Builder? _forwardedInstanceSources;

                public void Add(Bucket bucket)
                {
                    _buckets ??= new();
                    _buckets.Add(bucket);
                }

                public void Add(FactoryMethod factoryMethod)
                {
                    _factoryMethods ??= ImmutableArray.CreateBuilder<FactoryMethod>();
                    _factoryMethods.Add(factoryMethod);
                }

                public void Add(FactoryOfMethod factoryOfMethod)
                {
                    _factoryOfMethods ??= ImmutableArray.CreateBuilder<FactoryOfMethod>();
                    _factoryOfMethods.Add(factoryOfMethod);
                }
                
                public void Add(Registration registration)
                {
                    _registrations ??= ImmutableArray.CreateBuilder<Registration>();
                    _registrations.Add(registration);
                }

                public void Add(ForwardedInstanceSource forwardedInstanceSource)
                {
                    _forwardedInstanceSources ??= ImmutableArray.CreateBuilder<ForwardedInstanceSource>();
                    _forwardedInstanceSources.Add(forwardedInstanceSource);
                }

                public Bucket Build(Compilation compilation)
                {
                    return new Bucket(
                        _buckets ?? Enumerable.Empty<Bucket>(),
                        _factoryMethods?.ToImmutable() ?? ImmutableArray<FactoryMethod>.Empty,
                        _factoryOfMethods?.ToImmutable() ?? ImmutableArray<FactoryOfMethod>.Empty,
                        _registrations?.ToImmutable() ?? ImmutableArray<Registration>.Empty,
                        _forwardedInstanceSources?.ToImmutable() ?? ImmutableArray<ForwardedInstanceSource>.Empty,
                        compilation);
                }
            }
        }

        private class Bucket
        {
            public Bucket(
                IEnumerable<Bucket> childResolvers,
                ImmutableArray<FactoryMethod> factoryMethods,
                ImmutableArray<FactoryOfMethod> factoryOfMethods,
                ImmutableArray<Registration> registrations,
                ImmutableArray<ForwardedInstanceSource> forwardedInstanceSources,
                Compilation compilation)
            {
                var builder = ImmutableArray.CreateBuilder<Bucket>();
                foreach (var childResolver in childResolvers)
                {
                    if (childResolver.IsEmpty)
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
                _factoryOfMethods = factoryOfMethods;
                _registrations = registrations;
                _forwardedInstanceSources = forwardedInstanceSources;
                _compilation = compilation;
            }

            private readonly ImmutableArray<Bucket> _childResolvers;
            private readonly ImmutableArray<FactoryMethod> _factoryMethods;
            private readonly ImmutableArray<FactoryOfMethod> _factoryOfMethods;
            private readonly ImmutableArray<Registration> _registrations;
            private readonly ImmutableArray<ForwardedInstanceSource> _forwardedInstanceSources;
            private readonly Compilation _compilation;

            private bool IsEmpty => _factoryMethods.IsDefaultOrEmpty
                                   && _factoryOfMethods.IsDefaultOrEmpty
                                   && _registrations.IsDefaultOrEmpty
                                   && _forwardedInstanceSources.IsDefaultOrEmpty;
            
            public bool TryResolve(ITypeSymbol type, out InstanceSource instanceSource, out bool isAmbiguous, out IEnumerable<InstanceSource> sourcesNotMatchingConstraints)
            {
                instanceSource = null!;
                List<InstanceSource>? sourcesNotMatchingConstraintsTemp = null;

                foreach (var factoryMethod in GetAllRelevantFactoryMethods(type))
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
                            sourcesNotMatchingConstraints = Array.Empty<InstanceSource>();
                            return false;
                        }
                    }
                    else if (constraintsDoNotMatch)
                    {
                        (sourcesNotMatchingConstraintsTemp ??= new()).Add(factoryMethod);
                    }
                }

                foreach (var registration in _registrations)
                {
                    if (registration.Type.OriginalDefinition.Equals(type.OriginalDefinition))
                    {
                        var originalConstructor = registration.Constructor.OriginalDefinition;
                        var constructor = ((INamedTypeSymbol)type).InstanceConstructors.First(
                            x => x.OriginalDefinition.Equals(originalConstructor));

                        var updatedRegistration = registration with
                        {
                            Constructor = constructor, Type = ((INamedTypeSymbol)type)
                        };
                        if (instanceSource is null)
                        {
                            instanceSource = updatedRegistration;
                        }
                        else if (instanceSource != updatedRegistration)
                        {
                            instanceSource = null!;
                            isAmbiguous = true;
                            sourcesNotMatchingConstraints = Array.Empty<InstanceSource>();
                            return false;
                        }
                    }
                }
                
                foreach (var forwardedInstanceSource in _forwardedInstanceSources)
                {
                    if (forwardedInstanceSource.AsType.OriginalDefinition.Equals(type.OriginalDefinition))
                    {
                        if (forwardedInstanceSource.Underlying is Registration registration)
                        {
                            var typeArguments = ((INamedTypeSymbol)type).TypeArguments;
                            if (SatisfiesConstraints(registration.Type, typeArguments, _compilation))
                            {
                                var originalConstructor = registration.Constructor.OriginalDefinition;
                                var constructedRegistrationType =
                                    registration.Type.OriginalDefinition.Construct(typeArguments.ToArray());
                                var constructor = constructedRegistrationType.InstanceConstructors.First(
                                    x => x.OriginalDefinition.Equals(originalConstructor));

                                var updatedRegistration = registration with
                                {
                                    Constructor = constructor, Type = constructedRegistrationType
                                };

                                var updatedForwardedInstanceSource =
                                    ForwardedInstanceSource.Create((INamedTypeSymbol)type, updatedRegistration);
                                
                                if (instanceSource is null)
                                {
                                    instanceSource = updatedForwardedInstanceSource;
                                }
                                else if (instanceSource != updatedForwardedInstanceSource)
                                {
                                    instanceSource = null!;
                                    isAmbiguous = true;
                                    sourcesNotMatchingConstraints = Array.Empty<InstanceSource>();
                                    return false;
                                }
                            }
                            else
                            {
                                (sourcesNotMatchingConstraintsTemp ??= new()).Add(forwardedInstanceSource);
                            }
                        }
                        else
                        {
                            throw new NotImplementedException(forwardedInstanceSource.Underlying.ToString());
                        }
                    }
                }
                
                if (instanceSource is not null)
                {
                    isAmbiguous = false;
                    sourcesNotMatchingConstraints = Array.Empty<InstanceSource>();
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
                            sourcesNotMatchingConstraints = Array.Empty<InstanceSource>();
                            return false;
                        }
                    }
                    else if (isChildAmbiguous)
                    {
                        instanceSource = null!;
                        isAmbiguous = true;
                        sourcesNotMatchingConstraints = Array.Empty<InstanceSource>();
                        return false;
                    }
                    (sourcesNotMatchingConstraintsTemp ??= new()).AddRange(childSourcesNotMatchingConstraints);
                }

                if (instanceSource is not null)
                {
                    isAmbiguous = false;
                    sourcesNotMatchingConstraints = Array.Empty<InstanceSource>();
                    return true;
                }

                instanceSource = null!;
                isAmbiguous = false;
                sourcesNotMatchingConstraints = sourcesNotMatchingConstraintsTemp ?? Enumerable.Empty<InstanceSource>();
                return false;
            }

            public void ResolveAll(ITypeSymbol type, HashSet<FactoryMethod> instanceSources)
            {
                foreach (var factoryMethod in GetAllRelevantFactoryMethods(type))
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

            private RelevantFactoryMethodsEnumerable GetAllRelevantFactoryMethods(ITypeSymbol toConstruct)
            {
                return new RelevantFactoryMethodsEnumerable(_factoryMethods, _factoryOfMethods, toConstruct);
            }

            private static bool IsRelevant(FactoryOfMethod factoryOfMethod, ITypeSymbol toConstruct)
            {
                return factoryOfMethod.FactoryOfType.OriginalDefinition.Equals(toConstruct.OriginalDefinition);
            }

            private bool CanConstructFromGenericFactoryMethod(ITypeSymbol toConstruct, FactoryMethod factoryMethod, out FactoryMethod constructedFactoryMethod, out bool constraintsDoNotMatch)
            {
                if (!CanConstructFromGenericMethodReturnType(_compilation, toConstruct, factoryMethod.FactoryOfType, factoryMethod.Method, out var constructedMethod, out constraintsDoNotMatch))
                {
                    constructedFactoryMethod = null!;
                    return false;
                }

                constructedFactoryMethod = factoryMethod with
                {
                    FactoryOfType = toConstruct,
                    Method = constructedMethod,
                    IsOpenGeneric = false
                };
                constraintsDoNotMatch = false;
                return true;
            }

            private readonly struct RelevantFactoryMethodsEnumerable
            {
                private readonly ImmutableArray<FactoryMethod> _factoryMethods;
                private readonly ImmutableArray<FactoryOfMethod> _factoryOfMethods;
                private readonly ITypeSymbol _toConstruct;

                public RelevantFactoryMethodsEnumerable(ImmutableArray<FactoryMethod> factoryMethods, ImmutableArray<FactoryOfMethod> factoryOfMethods, ITypeSymbol toConstruct)
                {
                    _factoryMethods = factoryMethods;
                    _factoryOfMethods = factoryOfMethods;
                    _toConstruct = toConstruct;
                }

                public RelevantFactoryMethodsEnumerator GetEnumerator()
                    => new RelevantFactoryMethodsEnumerator(_factoryMethods, _factoryOfMethods, _toConstruct);
            }

            private struct RelevantFactoryMethodsEnumerator
            {
                private readonly ITypeSymbol _toConstruct;

                [CodacyCannotFigureOutHowToHandleThisFieldUnlessAnAttributeIsApplied]
                private ImmutableArray<FactoryMethod>.Enumerator _factoryMethodsEnumerator;

                [CodacyCannotFigureOutHowToHandleThisFieldUnlessAnAttributeIsApplied]
                private ImmutableArray<FactoryOfMethod>.Enumerator _factoryOfMethodsEnumerator;

                private FactoryMethod _current;

                public RelevantFactoryMethodsEnumerator(ImmutableArray<FactoryMethod> factoryMethods, ImmutableArray<FactoryOfMethod> factoryOfMethods, ITypeSymbol toConstruct)
                {
                    _toConstruct = toConstruct;

                    _factoryMethodsEnumerator = factoryMethods.GetEnumerator();
                    _factoryOfMethodsEnumerator = factoryOfMethods.GetEnumerator();
                    _current = null!;
                }

                public FactoryMethod Current => _current;

                public bool MoveNext()
                {
                    if (_factoryMethodsEnumerator.MoveNext())
                    {
                        _current = _factoryMethodsEnumerator.Current;
                        return true;
                    }
                    else
                    {
                        while (_factoryOfMethodsEnumerator.MoveNext())
                        {
                            if (IsRelevant(_factoryOfMethodsEnumerator.Current, _toConstruct))
                            {
                                _current = _factoryOfMethodsEnumerator.Current.Underlying;
                                return true;
                            }
                        }

                        return false;
                    }
                }
            }

            /// <summary>
            /// This is a workaround to Codacy reporting invalid warnings for fields that cannot be made readonly.
            /// </summary>
            [AttributeUsage(AttributeTargets.Field, AllowMultiple = false, Inherited = false)]
            private sealed class CodacyCannotFigureOutHowToHandleThisFieldUnlessAnAttributeIsAppliedAttribute : Attribute
            {
            }
        }
    }
}
