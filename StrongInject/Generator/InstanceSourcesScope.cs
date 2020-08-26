using Microsoft.CodeAnalysis;
using System;
using System.Collections.Generic;

namespace StrongInject.Generator
{
    internal class InstanceSourcesScope
    {
        private readonly IReadOnlyDictionary<ITypeSymbol, InstanceSources> _instanceSources;
        private readonly Dictionary<ITypeSymbol, DelegateParameter>? _delegateParameters;
        private readonly InstanceSourcesScope _containerScope;
        private readonly WellKnownTypes _wellKnownTypes;
        private readonly GenericRegistrationsResolver _genericRegistrationsResolver;

        public int Depth { get; }

        public InstanceSourcesScope(IReadOnlyDictionary<ITypeSymbol, InstanceSources> instanceSources, WellKnownTypes wellKnownTypes, GenericRegistrationsResolver genericRegistrationsResolver)
        {
            _instanceSources = instanceSources;
            _containerScope = this;
            _wellKnownTypes = wellKnownTypes;
            _genericRegistrationsResolver = genericRegistrationsResolver;
            Depth = 0;
        }

        private InstanceSourcesScope(InstanceSourcesScope containerScope, Dictionary<ITypeSymbol, DelegateParameter> delegateParameters, int depth)
        {
            _instanceSources = containerScope._instanceSources;
            _delegateParameters = delegateParameters;
            _containerScope = containerScope;
            _wellKnownTypes = containerScope._wellKnownTypes;
            _genericRegistrationsResolver = containerScope._genericRegistrationsResolver;
            Depth = depth;
        }

        public bool TryGetSource(ITypeSymbol target, out InstanceSource instanceSource, out bool isAmbiguous, out IEnumerable<FactoryMethod> sourcesNotMatchingConstraints)
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
                    instanceSource = new DelegateSource(delegateType, taskOfType, parameters, isAsync: true);
                }
                else
                {
                    instanceSource = new DelegateSource(delegateType, returnType, parameters, isAsync: false);
                }
                return true;
            }

            if (target is IArrayTypeSymbol { Rank: 1, ElementType: var elementType } arrayTypeSymbol )
            {
                var elementSources = new List<InstanceSource>();
                if (_instanceSources.TryGetValue(elementType, out var nonGenericElementSources))
                {
                    elementSources.AddRange(nonGenericElementSources);
                };

                elementSources.AddRange(_genericRegistrationsResolver.ResolveAll(elementType));

                instanceSource = new ArraySource(
                    arrayTypeSymbol,
                    elementType,
                    elementSources);
                return true;
            }

            instanceSource = null!;
            return false;
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

        public InstanceSourcesScope Enter(InstanceSource instanceSource)
        {
            switch (instanceSource)
            {
                case DelegateSource { parameters: var parameters }:
                    var delegateParameters = _delegateParameters is null
                        ? new Dictionary<ITypeSymbol, DelegateParameter>()
                        : new Dictionary<ITypeSymbol, DelegateParameter>(_delegateParameters);
                    foreach (var param in parameters)
                    {
                        delegateParameters[param.Type] = new DelegateParameter(param, "param" + Depth + "_" + param.Ordinal);
                    }
                    return new InstanceSourcesScope(_containerScope, delegateParameters, Depth + 1);
                case { scope: Scope.SingleInstance }:
                    return _containerScope;
                default:
                    return this;
            }
        }
    }
}

