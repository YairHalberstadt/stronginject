using Microsoft.CodeAnalysis;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace StrongInject.Generator
{
    internal class InstanceSourcesScope
    {
        private readonly IReadOnlyDictionary<ITypeSymbol, InstanceSource> _instanceSource;
        private readonly Dictionary<ITypeSymbol, DelegateParameter>? _delegateParameters;
        private readonly InstanceSourcesScope _containerScope;
        private readonly INamedTypeSymbol? _task1Type;
        private readonly INamedTypeSymbol? _valueTask1Type;

        public int Depth { get; }

        public InstanceSourcesScope(IReadOnlyDictionary<ITypeSymbol, InstanceSource> instanceSource, Compilation compilation)
        {
            _instanceSource = instanceSource;
            _containerScope = this;
            _task1Type = compilation.GetType(typeof(Task<>));
            _valueTask1Type = compilation.GetType(typeof(ValueTask<>));
            Depth = 0;
        }

        private InstanceSourcesScope(InstanceSourcesScope containerScope, Dictionary<ITypeSymbol, DelegateParameter> delegateParameters, int depth)
        {
            _instanceSource = containerScope._instanceSource;
            _delegateParameters = delegateParameters;
            _containerScope = containerScope;
            _task1Type = containerScope._task1Type;
            _valueTask1Type = containerScope._valueTask1Type;
            Depth = depth;
        }

        public bool TryGetSource(ITypeSymbol target, out InstanceSource instanceSource)
        {
            if (_delegateParameters is not null && _delegateParameters.TryGetValue(target, out var delegateParameter))
            {
                instanceSource = delegateParameter;
                return true;
            }
            if (_instanceSource.TryGetValue(target, out instanceSource))
            {
                return true;
            }
            if (target is INamedTypeSymbol { DelegateInvokeMethod: { ReturnType: { } returnType, Parameters: var parameters } } delegateType)
            {
                if (returnType.OriginalDefinition.Equals(_task1Type, SymbolEqualityComparer.Default)
                    || returnType.OriginalDefinition.Equals(_valueTask1Type, SymbolEqualityComparer.Default))
                {
                    instanceSource = new DelegateSource(delegateType, ((INamedTypeSymbol)returnType).TypeArguments[0], parameters, isAsync: true);
                }
                else
                {
                    instanceSource = new DelegateSource(delegateType, returnType, parameters, isAsync: false);
                }
                return true;
            }
            return false;
        }

        public InstanceSource this[ITypeSymbol typeSymbol] => TryGetSource(typeSymbol, out var instanceSource)
            ? instanceSource
            : throw new KeyNotFoundException($"No source is available for type {typeSymbol}");

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

