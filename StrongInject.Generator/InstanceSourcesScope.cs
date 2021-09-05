using Microsoft.CodeAnalysis;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;

namespace StrongInject.Generator
{
    internal class InstanceSourcesScope
    {
        private readonly IReadOnlyDictionary<ITypeSymbol, InstanceSources> _instanceSources;
        private readonly GenericRegistrationsResolver _genericRegistrationsResolver;
        private readonly IReadOnlyDictionary<ITypeSymbol, ImmutableArray<DecoratorSource>> _decoratorSources;
        private readonly GenericDecoratorsResolver _genericDecoratorsResolver;
        private readonly WellKnownTypes _wellKnownTypes;
        private readonly InstanceSourcesScope _containerScope;
        private readonly Dictionary<ITypeSymbol, DelegateParameter>? _delegateParameters;

        public int Depth { get; }

        public InstanceSourcesScope(
            IReadOnlyDictionary<ITypeSymbol, InstanceSources> instanceSources,
            GenericRegistrationsResolver genericRegistrationsResolver,
            IReadOnlyDictionary<ITypeSymbol, ImmutableArray<DecoratorSource>> decoratorSources,
            GenericDecoratorsResolver genericDecoratorsResolver,
            WellKnownTypes wellKnownTypes)
        {
            _instanceSources = instanceSources;
            _genericRegistrationsResolver = genericRegistrationsResolver;
            _decoratorSources = decoratorSources;
            _genericDecoratorsResolver = genericDecoratorsResolver;
            _wellKnownTypes = wellKnownTypes;
            _containerScope = this;
            Depth = 0;
        }

        private InstanceSourcesScope(InstanceSourcesScope containerScope, Dictionary<ITypeSymbol, DelegateParameter> delegateParameters, int depth)
        {
            _instanceSources = containerScope._instanceSources;
            _genericRegistrationsResolver = containerScope._genericRegistrationsResolver;
            _decoratorSources = containerScope._decoratorSources;
            _genericDecoratorsResolver = containerScope._genericDecoratorsResolver;
            _wellKnownTypes = containerScope._wellKnownTypes;
            _containerScope = containerScope;
            _delegateParameters = delegateParameters;
            Depth = depth;
        }

        public bool TryGetSource(ITypeSymbol target, out InstanceSource instanceSource, out bool isAmbiguous, out IEnumerable<InstanceSource> sourcesNotMatchingConstraints)
        {
            if (!TryGetUnderlyingSource(target, out instanceSource, out isAmbiguous, out sourcesNotMatchingConstraints))
                return false;

            instanceSource = Decorate(instanceSource);

            return true;
        }

        private bool TryGetUnderlyingSource(ITypeSymbol target, out InstanceSource instanceSource, out bool isAmbiguous, out IEnumerable<InstanceSource> sourcesNotMatchingConstraints)
        {
            isAmbiguous = false;
            sourcesNotMatchingConstraints = Array.Empty<FactoryMethod>();
            if (_delegateParameters is not null && _delegateParameters.TryGetValue(target, out var delegateParameter))
            {
                instanceSource = delegateParameter;
                return true;
            }

            if (_instanceSources.TryGetValue(target, out var instanceSources))
            {
                instanceSource = instanceSources.Best!;
                isAmbiguous = instanceSource is null;
                return !isAmbiguous;
            }

            if (_genericRegistrationsResolver.TryResolve(target, out var constructedFactoryMethod, out isAmbiguous, out sourcesNotMatchingConstraints))
            {
                instanceSource = constructedFactoryMethod;
                return true;
            }
            else if (isAmbiguous)
            {
                instanceSource = null!;
                return false;
            }

            if (target is INamedTypeSymbol 
            { 
                DelegateInvokeMethod: 
                { 
                    ReturnType: { SpecialType: not SpecialType.System_Void } returnType,
                    Parameters: var parameters 
                }
            } delegateType)
            {
                if (returnType.IsWellKnownTaskType(_wellKnownTypes, out var taskOfType))
                {
                    instanceSource = new DelegateSource(delegateType, taskOfType, parameters, IsAsync: true);
                }
                else
                {
                    instanceSource = new DelegateSource(delegateType, returnType, parameters, IsAsync: false);
                }
                return true;
            }

            if (target.IsWellKnownOwnedType(_wellKnownTypes, out var isAsync, out var valueType))
            {
                instanceSource = new OwnedSource(target, valueType, isAsync);
                return true;
            }

            if (target is IArrayTypeSymbol { Rank: 1, ElementType: var elementType } arrayTypeSymbol )
            {
                var elementSources = Enumerable.Empty<InstanceSource>();
                if (_instanceSources.TryGetValue(elementType, out var nonGenericElementSources))
                {
                    elementSources = elementSources.Concat(nonGenericElementSources);
                }

                elementSources = elementSources.Concat(_genericRegistrationsResolver.ResolveAll(elementType));

                var decoratedSources = elementSources.Select(x => Decorate(x)).ToList();

                instanceSource = new ArraySource(
                    arrayTypeSymbol,
                    elementType,
                    decoratedSources);
                return true;
            }

            instanceSource = null!;
            return false;
        }

        private InstanceSource Decorate(InstanceSource instanceSource)
        {
            var target = instanceSource.OfType;
            if (instanceSource.CanDecorate)
            {
                if (instanceSource is FactorySource factorySource)
                {
                    instanceSource = factorySource with { Underlying = Decorate(factorySource.Underlying) };
                }
                else if (instanceSource is ForwardedInstanceSource forwardedInstanceSource)
                {
                    instanceSource = forwardedInstanceSource with { Underlying = Decorate(forwardedInstanceSource.Underlying) };
                }

                foreach (var decorator in _decoratorSources.GetValueOrDefault(target, ImmutableArray<DecoratorSource>.Empty))
                {
                    instanceSource = new WrappedDecoratorInstanceSource(decorator, instanceSource);
                }

                foreach (var decorator in _genericDecoratorsResolver.ResolveDecorators(target))
                {
                    instanceSource = new WrappedDecoratorInstanceSource(decorator, instanceSource);
                }
            }

            return instanceSource;
        }

        public InstanceSource this[ITypeSymbol typeSymbol]
        {
            get
            {
                if (TryGetSource(typeSymbol, out var instanceSource, out var ambiguous, out _))
                    return instanceSource;
                if (ambiguous)
                    throw new InvalidOperationException($"No best source for type {typeSymbol}");
                throw new KeyNotFoundException($"No source is available for type {typeSymbol}");
            }
        }

        public InstanceSource? GetParameterSource(IParameterSymbol parameterSymbol)
        {
            if (parameterSymbol.IsOptional)
            {
                if (TryGetSource(parameterSymbol.Type, out var instanceSource, out var ambiguous, out _))
                    return instanceSource;
                if (ambiguous)
                    throw new InvalidOperationException($"No best source for type {parameterSymbol.Type}");
                return null;
            }
            return this[parameterSymbol.Type];
        }

        public InstanceSourcesScope Enter(InstanceSource instanceSource)
        {
            switch (instanceSource)
            {
                case DelegateSource { Parameters: var parameters }:
                    var delegateParameters = _delegateParameters is null
                        ? new Dictionary<ITypeSymbol, DelegateParameter>(SymbolEqualityComparer.Default)
                        : new Dictionary<ITypeSymbol, DelegateParameter>(_delegateParameters);
                    foreach (var param in parameters)
                    {
                        delegateParameters[param.Type] = new DelegateParameter(param, "param" + Depth + "_" + param.Ordinal);
                    }
                    return new InstanceSourcesScope(_containerScope, delegateParameters, Depth + 1);
                case { Scope: Scope.SingleInstance }:
                    return _containerScope;
                default:
                    return this;
            }
        }
    }
}

