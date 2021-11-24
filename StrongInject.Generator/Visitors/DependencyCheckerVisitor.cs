using Microsoft.CodeAnalysis;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace StrongInject.Generator.Visitors
{
    internal class DependencyCheckerVisitor : BaseVisitor<DependencyCheckerVisitor.State>
    {
        private readonly HashSet<InstanceSource> _visited = new();
        private readonly ITypeSymbol _target;
        private readonly InstanceSourcesScope _containerScope;
        private readonly Action<Diagnostic> _reportDiagnostic;
        private readonly Location _location;
        private readonly List<InstanceSource> _resolutionPath = new();
        private bool _anyErrors;

        private DependencyCheckerVisitor(ITypeSymbol target, InstanceSourcesScope containerScope, Action<Diagnostic> reportDiagnostic, Location location)
        {
            _target = target;
            _containerScope = containerScope;
            _reportDiagnostic = reportDiagnostic;
            _location = location;
        }

        public static bool HasCircularOrMissingDependencies(ITypeSymbol target, bool isAsync, InstanceSourcesScope containerScope, Action<Diagnostic> reportDiagnostic, Location location)
        {
            var visitor = new DependencyCheckerVisitor(target, containerScope, reportDiagnostic, location);
            var state = new State(containerScope, isAsync);
            visitor.VisitCore(visitor.GetInstanceSource(target, state, parameterSymbol: null), state);
            return visitor._anyErrors;
        }

        protected override InstanceSource? GetInstanceSource(ITypeSymbol type, State state, IParameterSymbol? parameterSymbol)
        {
            if (!state.InstanceSourcesScope.TryGetSource(type, out var instanceSource, out var ambiguous, out var sourcesNotMatchingConstraints))
            {
                if (ambiguous)
                {
                    _reportDiagnostic(NoBestSourceForType(_location, _target, type));
                    _anyErrors = true;
                }
                else
                {
                    if (parameterSymbol?.IsOptional ?? false)
                    {
                        _reportDiagnostic(InfoNoSourceForOptionalParameter(_location, _target, type));
                    }
                    else
                    {
                        _reportDiagnostic(NoSourceForType(_location, _target, type));
                        _anyErrors = true;
                    }

                    foreach (var sourceNotMatchingConstraints in sourcesNotMatchingConstraints)
                    {
                        switch (sourceNotMatchingConstraints)
                        {
                            case FactoryMethod {Method: var method}:
                                _reportDiagnostic(WarnFactoryMethodNotMatchingConstraint(_location, _target, type, method));
                                break;
                            case ForwardedInstanceSource { Underlying: Registration { Type: var registeredType } }:
                                _reportDiagnostic(WarnRegistrationNotMatchingConstraint(_location, _target, type, registeredType));
                                break;
                            default: throw new NotImplementedException(sourceNotMatchingConstraints.ToString());
                        }
                    }
                }
                return null;
            }
            return instanceSource;
        }

        protected override bool ShouldVisitBeforeUpdateState(InstanceSource? source, State state)
        {
            if (source is null)
                return false;
            if (_resolutionPath.Count == MAX_DEPENDENCY_TREE_DEPTH)
            {
                _reportDiagnostic(DependencyTreeTooDeep(_location, _target));
                _anyErrors = true;
                return false;
            }
            
            _resolutionPath.Add(source);
            return true;
        }

        protected override void UpdateState(InstanceSource source, ref State state)
        {
            if (source.Scope == Scope.SingleInstance && state.InstanceSourcesScope.Depth != 0)
            {
                // We don't care about circular dependencies between different scopes,
                // since we assume the delegate is instantiated lazily.
                // However we still need to track what we are currently visiting so we can warn if we
                // can't generate correct code for the delegate.
                // When the circular dependency crosses the boundary of a singleton this is always possible,
                // so there's no need to track what we were visiting before the singleton.
                state.ResetCurrentlyVisiting();
            }
            base.UpdateState(source, ref state);
        }

        protected override bool ShouldVisitAfterUpdateState(InstanceSource source, State state)
        {
            if (!state.AddCurrentlyVisiting(source, state.InstanceSourcesScope, out var existingScope))
            {
                if (source is DelegateSource delegateSource)
                {
                    var interveningDelegateParameters = state.InstanceSourcesScope.GetInterveningDelegateParameters(existingScope).ToList();
                    if (interveningDelegateParameters.Count > 0)
                    {
                        _reportDiagnostic(WarnCircularDependencyWithInterveningDelegateParameter(_location, _target, delegateSource.DelegateType, interveningDelegateParameters));
                    }
                    return false;
                }

                if (state.InstanceSourcesScope.Depth == existingScope.Depth)
                {
                    _reportDiagnostic(CircularDependency(_location, _target, source.OfType));
                    _anyErrors = true;
                    return false;
                }
            }
            
            if ((ReferenceEquals(state.InstanceSourcesScope, _containerScope)
                 || ReferenceEquals(state.PreviousScope, _containerScope))
                && !_visited.Add(source))
            {
                _resolutionPath.RemoveAt(_resolutionPath.Count - 1);
                state.RemoveCurrentlyVisiting(source);
                return false;
            }

            return true;
        }
        
        protected override void AfterVisit(InstanceSource source, State state)
        {
            if (source is { IsAsync: true, Scope: not Scope.SingleInstance } and not (DelegateSource or OwnedSource) && !state.IsScopeAsync
                || source is { IsAsync: true, Scope: Scope.SingleInstance } && !state.IsCurrentOrAnyParentScopeAsync)
            {
                _reportDiagnostic(RequiresAsyncResolution(_location, _target, source.OfType));
                _anyErrors = true;
            }
            _resolutionPath.RemoveAt(_resolutionPath.Count - 1);
            state.RemoveCurrentlyVisiting(source);
        }
        
        public override void Visit(DelegateSource delegateSource, State state)
        {
            var (delegateType, returnType, delegateParameters, isAsync) = delegateSource;
#pragma warning disable RS1024 // Compare symbols correctly
            foreach (var paramsWithType in delegateParameters.GroupBy(x => x.Type, (IEqualityComparer<ITypeSymbol>)SymbolEqualityComparer.Default))
#pragma warning restore RS1024 // Compare symbols correctly
            {
                if (paramsWithType.Count() > 1)
                {
                    _reportDiagnostic(DelegateHasMultipleParametersOfTheSameType(_location, _target, delegateType, paramsWithType.Key));
                    _anyErrors = true;
                }
            }

            var returnTypeSource = GetInstanceSource(returnType, state, parameterSymbol: null);

            if (returnTypeSource is DelegateParameter { Parameter: var param })
            {
                if (delegateParameters.Contains(param))
                {
                    _reportDiagnostic(WarnDelegateReturnTypeProvidedBySameDelegate(_location, _target, delegateType, returnType));
                }
                else
                {
                    _reportDiagnostic(WarnDelegateReturnTypeProvidedByAnotherDelegate(_location, _target, delegateType, returnType));
                }
            }
            else if (returnTypeSource?.Scope is Scope.SingleInstance)
            {
                _reportDiagnostic(WarnDelegateReturnTypeIsSingleInstance(_location, _target, delegateType, returnType));
            }

            var usedByDelegateParams = state.UsedParams ??= new(SymbolEqualityComparer.Default);
            state.IsScopeAsync = isAsync;
            VisitCore(returnTypeSource, state);

            foreach (var delegateParam in delegateParameters)
            {
                if (delegateParam.RefKind != RefKind.None)
                {
                    _reportDiagnostic(DelegateParameterIsPassedByRef(_location, _target, delegateType, delegateParam));
                    _anyErrors = true;
                }
                if (!usedByDelegateParams.Contains(delegateParam))
                {
                    _reportDiagnostic(WarnDelegateParameterNotUsed(_location, _target, delegateType, delegateParam.Type, returnType));
                }
            }
        }

        public override void Visit(DelegateParameter delegateParameter, State state)
        {
            state.UsedParams!.Add(delegateParameter.Parameter);
            base.Visit(delegateParameter, state);
        }

        public override void Visit(ArraySource arraySource, State state)
        {
            if (arraySource.Items.Count == 0)
            {
                _reportDiagnostic(WarnNoRegistrationsForElementType(_location, _target, arraySource.ElementType));
            }
            base.Visit(arraySource, state);
        }

        public struct State : IState
        {
            public State(InstanceSourcesScope scope, bool isAsync)
            {
                _instanceSourcesScope = scope;
                _isAsync = isAsync;
                _currentlyVisiting = new();
                PreviousScope = null;
                IsCurrentOrAnyParentScopeAsync = isAsync;
                UsedParams = null;
            }
            
            private InstanceSourcesScope _instanceSourcesScope;
            private bool _isAsync;
            private Dictionary<InstanceSource, InstanceSourcesScope> _currentlyVisiting;

            public InstanceSourcesScope? PreviousScope { get; private set; }
            public InstanceSourcesScope InstanceSourcesScope
            {
                get => _instanceSourcesScope;
                set
                {
                    PreviousScope = _instanceSourcesScope;
                    _instanceSourcesScope = value;
                }
            }
            public bool IsCurrentOrAnyParentScopeAsync { get; private set; }
            public bool IsScopeAsync
            {
                get => _isAsync;
                set
                {
                    _isAsync = value;
                    IsCurrentOrAnyParentScopeAsync |= _isAsync;
                }
            }
            public HashSet<IParameterSymbol>? UsedParams { get; set; }


            public bool AddCurrentlyVisiting(InstanceSource source, InstanceSourcesScope scope, out InstanceSourcesScope existingScope)
            {
                _currentlyVisiting.AddOrUpdate(source, scope, out existingScope);
                return existingScope is null;
            }

            public bool RemoveCurrentlyVisiting(InstanceSource source) => _currentlyVisiting.Remove(source);

            public void ResetCurrentlyVisiting() => _currentlyVisiting = new();
        }

        private string? PrintResolutionPath()
        {
            if (_resolutionPath.Count == 0)
                return null;
            var result = new StringBuilder("Resolution Path:\n");
            for (var i = 0; i < _resolutionPath.Count; i++)
            {
                if (i != 0)
                {
                    result.Append("⟶ ");
                }

                var source = _resolutionPath[i];
                switch (source)
                {
                    case Registration { Type: var type, Constructor: var constructor }:
                        result.Append("Resolving Type '");
                        result.Append(type);
                        result.Append("' through Constructor '");
                        result.Append(constructor);
                        result.AppendLine("'");
                        //equivalent to: `result.AppendLine($"Resolving Type '{ type }' through Constructor '{ constructor }'");`
                        break;
                    case FactorySource { FactoryOf: var factoryOf, Underlying: { OfType: var factoryType } }:
                        result.Append("Resolving Type '");
                        result.Append(factoryOf);
                        result.Append("' through Factory '");
                        result.Append(factoryType);
                        result.AppendLine("'");
                        //equivalent to: `result.AppendLine($"Resolving Type '{ factoryOf }' through Factory '{ factoryType }'");`
                        break;
                    case DelegateSource { DelegateType: var delegateType }:
                        result.Append("Resolving Delegate '");
                        result.Append(delegateType);
                        result.AppendLine("'");
                        //equivalent to: `result.AppendLine($"Resolving Delegate '{ delegateType }'");`
                        break;
                    case DelegateParameter { Parameter: { Type: var type} }:
                        result.Append("Resolving Type '");
                        result.Append(type);
                        result.AppendLine("' as a Delegate Parameter");
                        //equivalent to: `result.AppendLine($"Resolving Type '{ type }' as a Delegate Parameter");`
                        break;
                    case FactoryMethod { FactoryOfType: var type, Method: var method }:
                        result.Append("Resolving Type '");
                        result.Append(type);
                        result.Append("' through Factory Method '");
                        Format(method);
                        result.AppendLine("'");
                        //equivalent to: `result.AppendLine($"Resolving Type '{ type }' through Factory Method '{ Format(method) }'");`
                        break;
                    case InstanceFieldOrProperty { Type: var type, FieldOrPropertySymbol: { Kind: var kind } fieldOrPropertySymbol }:
                        result.Append("Resolving Type '");
                        result.Append(type);
                        result.Append("' through ");
                        result.Append(kind is SymbolKind.Field ? "Field" : "Property");
                        result.Append(" '");
                        result.Append(fieldOrPropertySymbol);
                        result.AppendLine("'");
                        //equivalent to: `result.AppendLine($"Resolving Type '{ type }' through { (kind is SymbolKind.Field  ? "Field" : "Property" ) } '{ fieldOrPropertySymbol }'");`
                        break;
                    case ArraySource { ArrayType: var array }:
                        result.Append("Resolving Array '");
                        result.Append(array);
                        result.AppendLine("'");
                        //equivalent to: `result.AppendLine($"Resolving Array '{ array }'");`
                        break;
                    case WrappedDecoratorInstanceSource { Decorator: var decorator }:
                        switch (decorator)
                        {
                            case DecoratorFactoryMethod { DecoratedType: var decoratedType, Method: var method }:
                                result.Append("Decorating Type '");
                                result.Append(decoratedType);
                                result.Append("' with Decorator Method '");
                                Format(method);
                                result.AppendLine("'");
                                //equivalent to: `result.AppendLine($"Decorating Type '{ decoratedType }' with Decorator Method '{ Format(method) }'");`
                                break;
                            case DecoratorRegistration { DecoratedType: var decoratedType, Type: var type, Constructor: var constructor }:
                                result.Append("Decorating Type '");
                                result.Append(decoratedType);
                                result.Append("' with Decorator '");
                                result.Append(type);
                                result.Append("' through Constructor '");
                                result.Append(constructor);
                                result.AppendLine("'");
                                //equivalent to: `result.AppendLine($"Decorating Type { decoratedType } with Decorator '{ type }' through Constructor '{ constructor }'");`
                                break;
                            default: throw new NotImplementedException(decorator.GetType().ToString());
                        }
                        break;
                    case ForwardedInstanceSource { AsType: var forwardedType, Underlying: { OfType: var type } }:
                        result.Append("Casting instance of Type '");
                        result.Append(type);
                        result.Append("' to '");
                        result.Append(forwardedType);
                        result.AppendLine("'");
                        //equivalent to: `result.AppendLine($"Casting instance of Type '{ type }' to '{ forwardedType }'");`
                        break;
                    case OwnedSource { OwnedType: var ownedType }:
                        result.Append("Resolving '");
                        result.Append(ownedType);
                        result.AppendLine("'");
                        //equivalent to: `result.AppendLine($"Resolving '{ ownedType }'");`
                        break;
                    default: throw new NotImplementedException(source.GetType().ToString());

                }
            }
            return result.ToString();

            void Format(IMethodSymbol method)
            {
                result.Append(method.ReturnType);
                result.Append(" ");
                result.Append(method);
            }
        }

        private Diagnostic CircularDependency(Location location, ITypeSymbol target, ITypeSymbol type)
        {
            return Diagnostic.Create(
                new DiagnosticDescriptor(
                        "SI0101",
                        "Type contains circular dependency",
                        "Error while resolving dependencies for '{0}': '{1}' has a circular dependency",
                        "StrongInject",
                        DiagnosticSeverity.Error,
                        isEnabledByDefault: true,
                        PrintResolutionPath()),
                    location,
                    target,
                    type);
        }

        private Diagnostic NoSourceForType(Location location, ITypeSymbol target, ITypeSymbol type)
        {
            return Diagnostic.Create(
                new DiagnosticDescriptor(
                        "SI0102",
                        "No source for instance of Type",
                        "Error while resolving dependencies for '{0}': We have no source for instance of type '{1}'",
                        "StrongInject",
                        DiagnosticSeverity.Error,
                        isEnabledByDefault: true,
                        PrintResolutionPath()),
                    location,
                    target,
                    type);
        }

        private Diagnostic RequiresAsyncResolution(Location location, ITypeSymbol target, ITypeSymbol type)
        {
            return Diagnostic.Create(
                new DiagnosticDescriptor(
                        "SI0103",
                        "Type can only be resolved asynchronously",
                        "Error while resolving dependencies for '{0}': '{1}' can only be resolved asynchronously.",
                        "StrongInject",
                        DiagnosticSeverity.Error,
                        isEnabledByDefault: true,
                        PrintResolutionPath()),
                    location,
                    target,
                    type);
        }

        private Diagnostic DelegateHasMultipleParametersOfTheSameType(Location location, ITypeSymbol target, ITypeSymbol delegateType, ITypeSymbol parameterType)
        {
            return Diagnostic.Create(
                new DiagnosticDescriptor(
                        "SI0104",
                        "Delegate has multiple parameters of same type",
                        "Error while resolving dependencies for '{0}': delegate '{1}' has multiple parameters of type '{2}'.",
                        "StrongInject",
                        DiagnosticSeverity.Error,
                        isEnabledByDefault: true,
                        PrintResolutionPath()),
                    location,
                    target,
                    delegateType,
                    parameterType);
        }

        private Diagnostic DelegateParameterIsPassedByRef(Location location, ITypeSymbol target, ITypeSymbol delegateType, IParameterSymbol parameter)
        {
            return Diagnostic.Create(
                new DiagnosticDescriptor(
                        "SI0105",
                        "Parameter of delegate is passed as ref",
                        "Error while resolving dependencies for '{0}': parameter '{1}' of delegate '{2}' is passed as '{3}'.",
                        "StrongInject",
                        DiagnosticSeverity.Error,
                        isEnabledByDefault: true,
                        PrintResolutionPath()),
                    location,
                    target,
                    parameter,
                    delegateType,
                    parameter.RefKind);
        }

        private Diagnostic NoBestSourceForType(Location location, ITypeSymbol target, ITypeSymbol type)
        {
            return Diagnostic.Create(
                new DiagnosticDescriptor(
                        "SI0106",
                        "Type contains circular dependency",
                        "Error while resolving dependencies for '{0}': We have multiple sources for instance of type '{1}' and no best source." +
                        " Try adding a single registration for '{1}' directly to the container, and moving any existing registrations for '{1}' on the container to an imported module.",
                        "StrongInject",
                        DiagnosticSeverity.Error,
                        isEnabledByDefault: true,
                        PrintResolutionPath()),
                    location,
                    target,
                    type);
        }

        private const int MAX_DEPENDENCY_TREE_DEPTH = 200;

        private Diagnostic DependencyTreeTooDeep(Location location, ITypeSymbol target)
        {
            return Diagnostic.Create(
                new DiagnosticDescriptor(
                        "SI0107",
                        "The Dependency tree is deeper than the Maximum Depth",
                        "Error while resolving dependencies for '{0}': The Dependency tree is deeper than the maximum depth of {1}.",
                        "StrongInject",
                        DiagnosticSeverity.Error,
                        isEnabledByDefault: true,
                        PrintResolutionPath()),
                    location,
                    target,
                    MAX_DEPENDENCY_TREE_DEPTH);
        }

        private Diagnostic WarnDelegateParameterNotUsed(Location location, ITypeSymbol target, ITypeSymbol delegateType, ITypeSymbol parameterType, ITypeSymbol delegateReturnType)
        {
            return Diagnostic.Create(
                new DiagnosticDescriptor(
                        "SI1101",
                        "Parameter of delegate is not used in resolution",
                        "Warning while resolving dependencies for '{0}': Parameter '{1}' of delegate '{2}' is not used in resolution of '{3}'.",
                        "StrongInject",
                        DiagnosticSeverity.Warning,
                        isEnabledByDefault: true,
                        PrintResolutionPath()),
                    location,
                    target,
                    parameterType,
                    delegateType,
                    delegateReturnType);
        }

        private Diagnostic WarnDelegateReturnTypeProvidedByAnotherDelegate(Location location, ITypeSymbol target, ITypeSymbol delegateType, ITypeSymbol delegateReturnType)
        {
            return Diagnostic.Create(
                new DiagnosticDescriptor(
                        "SI1102",
                        "Return type of delegate is provided as a parameter to another delegate and so will always have the same value",
                        "Warning while resolving dependencies for '{0}': Return type '{1}' of delegate '{2}' is provided as a parameter to another delegate and so will always have the same value.",
                        "StrongInject",
                        DiagnosticSeverity.Warning,
                        isEnabledByDefault: true,
                        PrintResolutionPath()),
                    location,
                    target,
                    delegateReturnType,
                    delegateType);
        }

        private Diagnostic WarnDelegateReturnTypeIsSingleInstance(Location location, ITypeSymbol target, ITypeSymbol delegateType, ITypeSymbol delegateReturnType)
        {
            return Diagnostic.Create(
                new DiagnosticDescriptor(
                        "SI1103",
                        "Return type of delegate has a single instance scope and so will always have the same value",
                        "Warning while resolving dependencies for '{0}': Return type '{1}' of delegate '{2}' has a single instance scope and so will always have the same value.",
                        "StrongInject",
                        DiagnosticSeverity.Warning,
                        isEnabledByDefault: true,
                        PrintResolutionPath()),
                    location,
                    target,
                    delegateReturnType,
                    delegateType);
        }

        private Diagnostic WarnDelegateReturnTypeProvidedBySameDelegate(Location location, ITypeSymbol target, ITypeSymbol delegateType, ITypeSymbol delegateReturnType)
        {
            return Diagnostic.Create(
                new DiagnosticDescriptor(
                        "SI1104",
                        "Return type of delegate is provided as a parameter to the delegate and so will be returned unchanged",
                        "Warning while resolving dependencies for '{0}': Return type '{1}' of delegate '{2}' is provided as a parameter to the delegate and so will be returned unchanged.",
                        "StrongInject",
                        DiagnosticSeverity.Warning,
                        isEnabledByDefault: true,
                        PrintResolutionPath()),
                    location,
                    target,
                    delegateReturnType,
                    delegateType);
        }

        private Diagnostic WarnNoRegistrationsForElementType(Location location, ITypeSymbol target, ITypeSymbol elementType)
        {
            return Diagnostic.Create(
                new DiagnosticDescriptor(
                        "SI1105",
                        "Resolving all registrations of Type, but there are no such registrations",
                        "Warning while resolving dependencies for '{0}': Resolving all registration of type '{1}', but there are no such registrations.",
                        "StrongInject",
                        DiagnosticSeverity.Warning,
                        isEnabledByDefault: true,
                        PrintResolutionPath()),
                    location,
                    target,
                    elementType);
        }

        private Diagnostic WarnFactoryMethodNotMatchingConstraint(Location location, ITypeSymbol target, ITypeSymbol type, IMethodSymbol factoryMethodNotMatchingConstraints)
        {
            return Diagnostic.Create(
                new DiagnosticDescriptor(
                        "SI1106",
                        "Factory Method cannot be used to resolve instance of Type as the required type arguments do not satisfy the generic constraints",
                        "Warning while resolving dependencies for '{0}': factory method '{1}' cannot be used to resolve instance of type '{2}' as the required type arguments do not satisfy the generic constraints.",
                        "StrongInject",
                        DiagnosticSeverity.Warning,
                        isEnabledByDefault: true,
                        PrintResolutionPath()),
                    location,
                    target,
                    factoryMethodNotMatchingConstraints,
                    type);
        }

        private Diagnostic WarnRegistrationNotMatchingConstraint(Location location, ITypeSymbol target, ITypeSymbol type, ITypeSymbol registeredType)
        {
            return Diagnostic.Create(
                new DiagnosticDescriptor(
                    "SI1106",
                    "Registration cannot be used to resolve instance of Type as the required type arguments do not satisfy the generic constraints",
                    "Warning while resolving dependencies for '{0}': Registered type '{1}' cannot be used to resolve instance of type '{2}' as the required type arguments do not satisfy the generic constraints.",
                    "StrongInject",
                    DiagnosticSeverity.Warning,
                    isEnabledByDefault: true,
                    PrintResolutionPath()),
                location,
                target,
                registeredType,
                type);
        }

        private Diagnostic WarnCircularDependencyWithInterveningDelegateParameter(Location location, ITypeSymbol target, ITypeSymbol delegateType, List<DelegateParameter> interveningDelegateParameters)
        {
            return Diagnostic.Create(
                new DiagnosticDescriptor(
                    "SI1107",
                    "Registration cannot be used to resolve instance of Type as the required type arguments do not satisfy the generic constraints",
                    "Warning while resolving dependencies for '{0}': Delegate '{1}' has a circular dependency on itself, which means it will not see the updated values for types '{2}' passed as parameters to intervening delegates '{3}'.",
                    "StrongInject",
                    DiagnosticSeverity.Warning,
                    isEnabledByDefault: true,
                    PrintResolutionPath()),
                location,
                target,
                delegateType,
                string.Join(", ", interveningDelegateParameters.Select(x => x.Parameter.Type)),
#pragma warning disable RS1024
                string.Join(", ", interveningDelegateParameters.Select(x => x.Parameter.ContainingType).Distinct(SymbolEqualityComparer.Default)));
#pragma warning restore RS1024
        }

        private Diagnostic InfoNoSourceForOptionalParameter(Location location, ITypeSymbol target, ITypeSymbol type)
        {
            return Diagnostic.Create(
                new DiagnosticDescriptor(
                        "SI2100",
                        "No source for instance of Type used in optional parameter",
                        "Info about resolving dependencies for '{0}': We have no source for instance of type '{1}' used in an optional parameter. Using The default value instead.",
                        "StrongInject",
                        DiagnosticSeverity.Info,
                        isEnabledByDefault: true,
                        PrintResolutionPath()),
                    location,
                    target,
                    type);
        }
    }
}
