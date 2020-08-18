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

        public int Depth { get; }

        public InstanceSourcesScope(IReadOnlyDictionary<ITypeSymbol, InstanceSources> instanceSources, WellKnownTypes wellKnownTypes)
        {
            _instanceSources = instanceSources;
            _containerScope = this;
            _wellKnownTypes = wellKnownTypes;
            Depth = 0;
        }

        private InstanceSourcesScope(InstanceSourcesScope containerScope, Dictionary<ITypeSymbol, DelegateParameter> delegateParameters, int depth)
        {
            _instanceSources = containerScope._instanceSources;
            _delegateParameters = delegateParameters;
            _containerScope = containerScope;
            _wellKnownTypes = containerScope._wellKnownTypes;
            Depth = depth;
        }

        public bool TryGetSource(ITypeSymbol target, out InstanceSource instanceSource, out bool ambiguous)
        {
            ambiguous = false;
            if (_delegateParameters is not null && _delegateParameters.TryGetValue(target, out var delegateParameter))
            {
                instanceSource = delegateParameter;
                return true;
            }
            if (_instanceSources.TryGetValue(target, out var instanceSources))
            {
                instanceSource = instanceSources.Best!;
                ambiguous = instanceSource is null;
                return !ambiguous;
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
                instanceSource = new ArraySource(
                    arrayTypeSymbol,
                    elementType,
                    _instanceSources.TryGetValue(elementType, out var elementSources) ? elementSources : (IReadOnlyCollection<InstanceSource>)Array.Empty<InstanceSource>());
                return true;
            }
            instanceSource = null!;
            return false;
        }

        public InstanceSource this[ITypeSymbol typeSymbol]
        {
            get
            {
                if (TryGetSource(typeSymbol, out var instanceSource, out var ambiguous))
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

