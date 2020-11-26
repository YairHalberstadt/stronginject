using Microsoft.CodeAnalysis;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;

namespace StrongInject.Generator
{
    internal static class DependencyChecker
    {
        private const int MAX_DEPENDENCY_TREE_DEPTH = 200;
        public static bool HasCircularOrMissingDependencies(ITypeSymbol target, bool isAsync, InstanceSourcesScope containerScope, Action<Diagnostic> reportDiagnostic, Location location)
        {
            var currentlyVisiting = new HashSet<InstanceSource>();
            var visited = new HashSet<InstanceSource>();

            return Visit(
                GetInstanceSourceOrReport(target, containerScope, isOptional: false),
                containerScope,
                usedParams: null,
                isScopeAsync: isAsync,
                anyScopeAsync: isAsync);

            InstanceSource? GetInstanceSourceOrReport(ITypeSymbol type, InstanceSourcesScope instanceSourcesScope, bool isOptional)
            {
                if (!instanceSourcesScope.TryGetSource(type, out var instanceSource, out var ambiguous, out var sourcesNotMatchingConstraints))
                {
                    if (ambiguous)
                    {
                        reportDiagnostic(NoBestSourceForType(location, target, type));
                    }
                    else
                    {
                        if (isOptional)
                        {
                            reportDiagnostic(InfoNoSourceForOptionalParameter(location, target, type));
                        }
                        else
                        {
                            reportDiagnostic(NoSourceForType(location, target, type));
                        }

                        foreach (var sourceNotMatchingConstraints in sourcesNotMatchingConstraints)
                        {
                            reportDiagnostic(WarnFactoryMethodNotMatchingConstraint(location, target, type, sourceNotMatchingConstraints.Method));
                        }
                    }
                    return null;
                }
                return instanceSource;
            }

            // returns true if has any errors that make resolution impossible
            bool Visit(InstanceSource? instanceSource, InstanceSourcesScope instanceSourcesScope, HashSet<IParameterSymbol>? usedParams, bool isScopeAsync, bool anyScopeAsync)
            {
                if (instanceSource is null)
                    return true;

                var outerIsContainerScope = ReferenceEquals(instanceSourcesScope, containerScope);
                var innerIsContainerScope = outerIsContainerScope;
                if (outerIsContainerScope && visited.Contains(instanceSource))
                    return false;

                if (!currentlyVisiting.Add(instanceSource))
                {
                    reportDiagnostic(CircularDependency(location, target, instanceSource.OfType));
                    return true;
                }

                var result = false;

                instanceSourcesScope = instanceSourcesScope.Enter(instanceSource);
                innerIsContainerScope = ReferenceEquals(instanceSourcesScope, containerScope);
                if (innerIsContainerScope && visited.Contains(instanceSource))
                    return false;

                if (currentlyVisiting.Count > MAX_DEPENDENCY_TREE_DEPTH)
                {
                    reportDiagnostic(DependencyTreeTooDeep(location, target));
                    return true;
                }

                switch (instanceSource)
                {
                    case DelegateSource(var delegateType, var returnType, var delegateParameters, var isAsync) delegateSource:
                        {
#pragma warning disable RS1024 // Compare symbols correctly
                            foreach (var paramsWithType in delegateParameters.GroupBy(x => x.Type, (IEqualityComparer<ITypeSymbol>)SymbolEqualityComparer.Default))
#pragma warning restore RS1024 // Compare symbols correctly
                            {
                                if (paramsWithType.Count() > 1)
                                {
                                    reportDiagnostic(DelegateHasMultipleParametersOfTheSameType(location, target, instanceSource.OfType, paramsWithType.Key));
                                    result = true;
                                }
                            }

                            var returnTypeSource = GetInstanceSourceOrReport(returnType, instanceSourcesScope, isOptional: false);

                            if (returnTypeSource is DelegateParameter { Parameter: var param })
                            {
                                if (delegateParameters.Contains(param))
                                {
                                    reportDiagnostic(WarnDelegateReturnTypeProvidedBySameDelegate(location, target, instanceSource.OfType, returnType));
                                }
                                else
                                {
                                    reportDiagnostic(WarnDelegateReturnTypeProvidedByAnotherDelegate(location, target, instanceSource.OfType, returnType));
                                }
                            }
                            else if (returnTypeSource?.Scope is Scope.SingleInstance)
                            {
                                reportDiagnostic(WarnDelegateReturnTypeIsSingleInstance(location, target, instanceSource.OfType, returnType));
                            }

                            var usedByDelegateParams = usedParams ?? new(SymbolEqualityComparer.Default);

                            result |= Visit(
                                returnTypeSource,
                                instanceSourcesScope,
                                usedParams: usedByDelegateParams,
                                isScopeAsync: isAsync,
                                anyScopeAsync: isAsync | anyScopeAsync);

                            foreach (var delegateParam in delegateParameters)
                            {
                                if (delegateParam.RefKind != RefKind.None)
                                {
                                    reportDiagnostic(DelegateParameterIsPassedByRef(location, target, instanceSource.OfType, delegateParam));
                                    result = true;
                                }
                                if (!usedByDelegateParams.Contains(delegateParam))
                                {
                                    reportDiagnostic(WarnDelegateParameterNotUsed(location, target, instanceSource.OfType, delegateParam.Type, returnType));
                                }
                            }

                            break;
                        }
                    case Registration { Constructor: { Parameters: var parameters } }:
                        {
                            foreach (var param in parameters)
                            {
                                var isOptional = param.IsOptional;
                                var source = GetInstanceSourceOrReport(param.Type, instanceSourcesScope, isOptional);
                                if (!isOptional || source is not null)
                                {
                                    result |= Visit(
                                        source,
                                        instanceSourcesScope,
                                        usedParams,
                                        isScopeAsync,
                                        anyScopeAsync);
                                }
                            }

                            break;
                        }
                    case FactorySource { Underlying: var underlying }:
                        result = Visit(
                            underlying,
                            instanceSourcesScope,
                            usedParams,
                            isScopeAsync,
                            anyScopeAsync);
                        break;
                    case DelegateParameter { Parameter: var parameter }:
                        usedParams!.Add(parameter);
                        break;
                    case ArraySource { Items: var items, ElementType: var elementType }:
                        if (items.Count == 0)
                        {
                            reportDiagnostic(WarnNoRegistrationsForElementType(location, target, elementType));
                        }
                        foreach (var item in items)
                        {
                            result |= Visit(
                                item,
                                instanceSourcesScope,
                                usedParams,
                                isScopeAsync,
                                anyScopeAsync);
                        }
                        break;
                    case FactoryMethod {Method: { Parameters: var parameters } }:
                        {
                            foreach (var param in parameters)
                            {
                                var isOptional = param.IsOptional;
                                var source = GetInstanceSourceOrReport(param.Type, instanceSourcesScope, isOptional);
                                if (!isOptional || source is not null)
                                {
                                    result |= Visit(
                                        source,
                                        instanceSourcesScope,
                                        usedParams,
                                        isScopeAsync,
                                        anyScopeAsync);
                                }
                            }

                            break;
                        }
                    case WrappedDecoratorInstanceSource(var decoratorSource, var underlyingInstanceSource):
                        {
                            switch (decoratorSource)
                            {
                                case DecoratorRegistration { Constructor: { Parameters: var parameters } }:
                                    {
                                        foreach (var param in parameters)
                                        {
                                            var isOptional = param.IsOptional;
                                            var paramSource = param.Ordinal == decoratorSource.decoratedParameter
                                                ? underlyingInstanceSource
                                                : GetInstanceSourceOrReport(param.Type, instanceSourcesScope, isOptional);

                                            if (!isOptional || paramSource is not null)
                                            {
                                                result |= Visit(
                                                paramSource,
                                                instanceSourcesScope,
                                                usedParams,
                                                isScopeAsync,
                                                anyScopeAsync);
                                            }
                                        }

                                        break;
                                    }
                                case DecoratorFactoryMethod { Method: { Parameters: var parameters } }:
                                    {
                                        foreach (var param in parameters)
                                        {
                                            var isOptional = param.IsOptional;
                                            var paramSource = param.Ordinal == decoratorSource.decoratedParameter
                                                ? underlyingInstanceSource
                                                : GetInstanceSourceOrReport(param.Type, instanceSourcesScope, isOptional);

                                            if (!isOptional || paramSource is not null)
                                            {
                                                result |= Visit(
                                                paramSource,
                                                instanceSourcesScope,
                                                usedParams,
                                                isScopeAsync,
                                                anyScopeAsync);
                                            }
                                        }

                                        break;
                                    }
                            }
                            break;
                        }
                    case ForwardedInstanceSource { Underlying: var underlying }:
                        {
                            result |= Visit(underlying, instanceSourcesScope, usedParams, isScopeAsync, anyScopeAsync);
                            break;
                        }
                    case InstanceFieldOrProperty:
                        break;
                    default: throw new NotImplementedException(instanceSource.GetType().ToString());
                }

                if (instanceSource is { IsAsync: true, Scope: not Scope.SingleInstance } and not DelegateSource && !isScopeAsync
                    || instanceSource is { IsAsync: true, Scope: Scope.SingleInstance } && !anyScopeAsync)
                {
                    reportDiagnostic(RequiresAsyncResolution(location, target, instanceSource.OfType));
                    result = true;
                }

                currentlyVisiting.Remove(instanceSource);
                if (outerIsContainerScope || innerIsContainerScope)
                {
                    visited.Add(instanceSource);
                }
                return result;
            }
        }

        private static Diagnostic CircularDependency(Location location, ITypeSymbol target, ITypeSymbol type)
        {
            return Diagnostic.Create(
                new DiagnosticDescriptor(
                        "SI0101",
                        "Type contains circular dependency",
                        "Error while resolving dependencies for '{0}': '{1}' has a circular dependency",
                        "StrongInject",
                        DiagnosticSeverity.Error,
                        isEnabledByDefault: true),
                    location,
                    target,
                    type);
        }

        private static Diagnostic NoSourceForType(Location location, ITypeSymbol target, ITypeSymbol type)
        {
            return Diagnostic.Create(
                new DiagnosticDescriptor(
                        "SI0102",
                        "No source for instance of Type",
                        "Error while resolving dependencies for '{0}': We have no source for instance of type '{1}'",
                        "StrongInject",
                        DiagnosticSeverity.Error,
                        isEnabledByDefault: true),
                    location,
                    target,
                    type);
        }

        private static Diagnostic RequiresAsyncResolution(Location location, ITypeSymbol target, ITypeSymbol type)
        {
            return Diagnostic.Create(
                new DiagnosticDescriptor(
                        "SI0103",
                        "Type can only be resolved asynchronously",
                        "Error while resolving dependencies for '{0}': '{1}' can only be resolved asynchronously.",
                        "StrongInject",
                        DiagnosticSeverity.Error,
                        isEnabledByDefault: true),
                    location,
                    target,
                    type);
        }

        private static Diagnostic DelegateHasMultipleParametersOfTheSameType(Location location, ITypeSymbol target, ITypeSymbol delegateType, ITypeSymbol parameterType)
        {
            return Diagnostic.Create(
                new DiagnosticDescriptor(
                        "SI0104",
                        "Delegate has multiple parameters of same type",
                        "Error while resolving dependencies for '{0}': delegate '{1}' has multiple parameters of type '{2}'.",
                        "StrongInject",
                        DiagnosticSeverity.Error,
                        isEnabledByDefault: true),
                    location,
                    target,
                    delegateType,
                    parameterType);
        }

        private static Diagnostic DelegateParameterIsPassedByRef(Location location, ITypeSymbol target, ITypeSymbol delegateType, IParameterSymbol parameter)
        {
            return Diagnostic.Create(
                new DiagnosticDescriptor(
                        "SI0105",
                        "Parameter of delegate is passed as ref",
                        "Error while resolving dependencies for '{0}': parameter '{1}' of delegate '{2}' is passed as '{3}'.",
                        "StrongInject",
                        DiagnosticSeverity.Error,
                        isEnabledByDefault: true),
                    location,
                    target,
                    parameter,
                    delegateType,
                    parameter.RefKind);
        }

        private static Diagnostic NoBestSourceForType(Location location, ITypeSymbol target, ITypeSymbol type)
        {
            return Diagnostic.Create(
                new DiagnosticDescriptor(
                        "SI0106",
                        "Type contains circular dependency",
                        "Error while resolving dependencies for '{0}': We have multiple sources for instance of type '{1}' and no best source." +
                        " Try adding a single registration for '{1}' directly to the container, and moving any existing registrations for '{1}' on the container to an imported module.",
                        "StrongInject",
                        DiagnosticSeverity.Error,
                        isEnabledByDefault: true),
                    location,
                    target,
                    type);
        }

        private static Diagnostic DependencyTreeTooDeep(Location location, ITypeSymbol target)
        {
            return Diagnostic.Create(
                new DiagnosticDescriptor(
                        "SI0107",
                        "The Dependency tree is deeper than the Maximum Depth",
                        "Error while resolving dependencies for '{0}': The Dependency tree is deeper than the maximum depth of {1}.",
                        "StrongInject",
                        DiagnosticSeverity.Error,
                        isEnabledByDefault: true),
                    location,
                    target,
                    MAX_DEPENDENCY_TREE_DEPTH);
        }

        private static Diagnostic WarnDelegateParameterNotUsed(Location location, ITypeSymbol target, ITypeSymbol delegateType, ITypeSymbol parameterType, ITypeSymbol delegateReturnType)
        {
            return Diagnostic.Create(
                new DiagnosticDescriptor(
                        "SI1101",
                        "Parameter of delegate is not used in resolution",
                        "Warning while resolving dependencies for '{0}': Parameter '{1}' of delegate '{2}' is not used in resolution of '{3}'.",
                        "StrongInject",
                        DiagnosticSeverity.Warning,
                        isEnabledByDefault: true),
                    location,
                    target,
                    parameterType,
                    delegateType,
                    delegateReturnType);
        }

        private static Diagnostic WarnDelegateReturnTypeProvidedByAnotherDelegate(Location location, ITypeSymbol target, ITypeSymbol delegateType, ITypeSymbol delegateReturnType)
        {
            return Diagnostic.Create(
                new DiagnosticDescriptor(
                        "SI1102",
                        "Return type of delegate is provided as a parameter to another delegate and so will always have the same value",
                        "Warning while resolving dependencies for '{0}': Return type '{1}' of delegate '{2}' is provided as a parameter to another delegate and so will always have the same value.",
                        "StrongInject",
                        DiagnosticSeverity.Warning,
                        isEnabledByDefault: true),
                    location,
                    target,
                    delegateReturnType,
                    delegateType);
        }

        private static Diagnostic WarnDelegateReturnTypeIsSingleInstance(Location location, ITypeSymbol target, ITypeSymbol delegateType, ITypeSymbol delegateReturnType)
        {
            return Diagnostic.Create(
                new DiagnosticDescriptor(
                        "SI1103",
                        "Return type of delegate has a single instance scope and so will always have the same value",
                        "Warning while resolving dependencies for '{0}': Return type '{1}' of delegate '{2}' has a single instance scope and so will always have the same value.",
                        "StrongInject",
                        DiagnosticSeverity.Warning,
                        isEnabledByDefault: true),
                    location,
                    target,
                    delegateReturnType,
                    delegateType);
        }

        private static Diagnostic WarnDelegateReturnTypeProvidedBySameDelegate(Location location, ITypeSymbol target, ITypeSymbol delegateType, ITypeSymbol delegateReturnType)
        {
            return Diagnostic.Create(
                new DiagnosticDescriptor(
                        "SI1104",
                        "Return type of delegate is provided as a parameter to the delegate and so will be returned unchanged",
                        "Warning while resolving dependencies for '{0}': Return type '{1}' of delegate '{2}' is provided as a parameter to the delegate and so will be returned unchanged.",
                        "StrongInject",
                        DiagnosticSeverity.Warning,
                        isEnabledByDefault: true),
                    location,
                    target,
                    delegateReturnType,
                    delegateType);
        }

        private static Diagnostic WarnNoRegistrationsForElementType(Location location, ITypeSymbol target, ITypeSymbol elementType)
        {
            return Diagnostic.Create(
                new DiagnosticDescriptor(
                        "SI1105",
                        "Resolving all registrations of Type, but there are no such registrations",
                        "Warning while resolving dependencies for '{0}': Resolving all registration of type '{1}', but there are no such registrations.",
                        "StrongInject",
                        DiagnosticSeverity.Warning,
                        isEnabledByDefault: true),
                    location,
                    target,
                    elementType);
        }

        private static Diagnostic WarnFactoryMethodNotMatchingConstraint(Location location, ITypeSymbol target, ITypeSymbol type, IMethodSymbol factoryMethodNotMatchingConstraints)
        {
            return Diagnostic.Create(
                new DiagnosticDescriptor(
                        "SI1106",
                        "Factory Method cannot be used to resolve instance of Type as the required type arguments do not satisfy the generic constraints",
                        "Warning while resolving dependencies for '{0}': factory method '{1}' cannot be used to resolve instance of type '{2}' as the required type arguments do not satisfy the generic constraints.",
                        "StrongInject",
                        DiagnosticSeverity.Warning,
                        isEnabledByDefault: true),
                    location,
                    target,
                    factoryMethodNotMatchingConstraints,
                    type);
        }

        private static Diagnostic InfoNoSourceForOptionalParameter(Location location, ITypeSymbol target, ITypeSymbol type)
        {
            return Diagnostic.Create(
                new DiagnosticDescriptor(
                        "SI2100",
                        "No source for instance of Type used in optional parameter",
                        "Info about resolving dependencies for '{0}': We have no source for instance of type '{1}' used in an optional parameter. Using The default value instead.",
                        "StrongInject",
                        DiagnosticSeverity.Info,
                        isEnabledByDefault: true),
                    location,
                    target,
                    type);
        }

        /// <summary>
        /// Only call if <see cref="HasCircularOrMissingDependencies"/> returns true,
        /// as this does no validation, and could throw or stack overflow otherwise.
        /// </summary>
        internal static bool RequiresAsync(InstanceSource source, InstanceSourcesScope containerScope)
        {
            var visited = new HashSet<InstanceSource>();
            return Visit(source, containerScope);

            bool Visit(InstanceSource? source, InstanceSourcesScope instanceSourcesScope)
            {
                if (source is null)
                    return false;

                if (instanceSourcesScope == containerScope && visited.Contains(source))
                    return false;

                if (source is DelegateSource { IsAsync: true })
                    return false;

                if (source.IsAsync)
                    return true;

                var innerScope = instanceSourcesScope.Enter(source);

                if (innerScope == containerScope && visited.Contains(source))
                    return false;

                switch (source)
                {
                    case Registration { Constructor: { Parameters: var parameters } }:
                        {
                            foreach (var param in parameters)
                            {
                                var paramSource = containerScope.GetParameterSource(param);
                                if (Visit(paramSource, innerScope))
                                    return true;
                            }

                            break;
                        }

                    case FactorySource { Underlying: var underlying }:
                        {
                            if (Visit(underlying, innerScope))
                                return true;
                            break;
                        }

                    case DelegateSource { ReturnType: var returnType }:
                        {
                            var returnTypeSource = innerScope[returnType];
                            if (Visit(returnTypeSource, innerScope))
                                return true;
                            break;
                        }

                    case ArraySource { Items: var items, }:
                        {
                            foreach (var item in items)
                            {
                                if (Visit(item, innerScope))
                                    return true;
                                    
                            }

                            break;
                        }
                    case FactoryMethod { Method: { Parameters: var parameters } }:
                        {
                            foreach (var param in parameters)
                            {
                                var paramSource = containerScope.GetParameterSource(param);
                                if (Visit(paramSource, innerScope))
                                    return true;
                            }
                        }
                        break;
                    case WrappedDecoratorInstanceSource(var decoratorSource, var underlyingInstanceSource):
                        {
                            switch (decoratorSource)
                            {
                                case DecoratorRegistration { Constructor: { Parameters: var parameters } }:
                                    {
                                        foreach (var param in parameters)
                                        {
                                            var paramSource = param.Ordinal == decoratorSource.decoratedParameter
                                                ? underlyingInstanceSource
                                                : containerScope.GetParameterSource(param);

                                            if (Visit(paramSource, innerScope))
                                                return true;
                                        }

                                        break;
                                    }
                                case DecoratorFactoryMethod { Method: { Parameters: var parameters } }:
                                    {
                                        foreach (var param in parameters)
                                        {
                                            var paramSource = param.Ordinal == decoratorSource.decoratedParameter
                                                ? underlyingInstanceSource
                                                : containerScope.GetParameterSource(param);

                                            if (Visit(paramSource, innerScope))
                                                return true;
                                        }

                                        break;
                                    }
                            }
                            break;
                        }
                    case ForwardedInstanceSource { Underlying: var underlying }:
                        {
                            if (Visit(underlying, innerScope))
                                return true;
                            break;
                        }
                    case InstanceFieldOrProperty:
                    case DelegateParameter:
                        break;
                    default: throw new NotImplementedException(source.GetType().ToString());
                }

                if (ReferenceEquals(instanceSourcesScope, containerScope) || ReferenceEquals(innerScope, containerScope))
                    visited.Add(source);

                return false;
            }
        }

        /// <summary>
        /// Only call if <see cref="HasCircularOrMissingDependencies"/> returns true,
        /// as this does no validation, and could throw or stack overflow otherwise.
        /// </summary>
        internal static List<InstanceSource> SingleInstanceVariablesToCreateEarly(DelegateSource source, InstanceSourcesScope scope)
        {
            var singleInstanceVariablesToCreateEarly = new List<InstanceSource>();
            Visit(source, scope);
            return singleInstanceVariablesToCreateEarly;

            void Visit(InstanceSource? source, InstanceSourcesScope instanceSourcesScope)
            {
                if (source is null)
                    return;

                if (source is DelegateSource { IsAsync: true })
                    return;

                if (source.Scope == Scope.SingleInstance)
                {
                    if (RequiresAsync(source, instanceSourcesScope.Enter(source)))
                    {
                        singleInstanceVariablesToCreateEarly.Add(source);
                    }
                    return;
                }

                var innerScope = instanceSourcesScope.Enter(source);

                switch (source)
                {
                    case Registration { Constructor: { Parameters: var parameters } }:
                        {
                            foreach (var param in parameters)
                            {
                                var paramSource = innerScope.GetParameterSource(param);
                                Visit(paramSource, innerScope);
                            }

                            break;
                        }

                    case FactorySource { Underlying: var underlying }:
                        {
                            Visit(underlying, innerScope);
                            break;
                        }
                    case DelegateSource { ReturnType: var returnType }:
                        {
                            var returnTypeSource = innerScope[returnType];
                            Visit(returnTypeSource, innerScope);
                            break;
                        }
                    case ArraySource { Items: var items, }:
                        {
                            foreach (var item in items)
                            {
                                Visit(item, innerScope);

                            }

                            break;
                        }
                    case FactoryMethod { Method: { Parameters: var parameters } }:
                        {
                            foreach (var param in parameters)
                            {
                                var paramSource = innerScope.GetParameterSource(param);
                                Visit(paramSource, innerScope);
                            }
                        }
                        break;
                    case WrappedDecoratorInstanceSource(var decoratorSource, var underlyingInstanceSource):
                        {
                            switch (decoratorSource)
                            {
                                case DecoratorRegistration { Constructor: { Parameters: var parameters } }:
                                    {
                                        foreach (var param in parameters)
                                        {
                                            var paramSource = param.Ordinal == decoratorSource.decoratedParameter
                                                ? underlyingInstanceSource
                                                : innerScope.GetParameterSource(param);

                                            Visit(paramSource, innerScope);
                                        }

                                        break;
                                    }
                                case DecoratorFactoryMethod { Method: { Parameters: var parameters } }:
                                    {
                                        foreach (var param in parameters)
                                        {
                                            var paramSource = param.Ordinal == decoratorSource.decoratedParameter
                                                ? underlyingInstanceSource
                                                : innerScope.GetParameterSource(param);

                                            Visit(paramSource, innerScope);
                                        }

                                        break;
                                    }
                            }
                            break;
                        }
                    case ForwardedInstanceSource { Underlying: var underlying }:
                        {
                            Visit(underlying, innerScope);
                            break;
                        }
                    case InstanceFieldOrProperty:
                    case DelegateParameter:
                        break;
                    default: throw new NotImplementedException(source.GetType().ToString());
                }
            }
        }

        /// <summary>
        /// Only call if <see cref="HasCircularOrMissingDependencies"/> returns true,
        /// as this does no validation, and could throw or stack overflow otherwise.
        /// </summary>
        internal static IEnumerable<InstanceSource> GetPartialOrderingOfSingleInstanceDependencies(InstanceSourcesScope containerScope, HashSet<InstanceSource> used)
        {
            var visited = new HashSet<InstanceSource>();
            var results = Enumerable.Empty<InstanceSource>();
            var added = new HashSet<InstanceSource>();

            foreach (var source in used)
            {
                List<InstanceSource>? thisResults = null;
                Visit(source, containerScope, ref thisResults);
                results = thisResults?.Concat(results) ?? results;
            }

            return results;

            void Visit(InstanceSource? source, InstanceSourcesScope instanceSourcesScope, ref List<InstanceSource>? results)
            {
                if (source is null)
                {
                    return;
                }

                if (source.Scope == Scope.SingleInstance && source is not (InstanceFieldOrProperty or ForwardedInstanceSource))
                {
                    if (added.Add(source))
                    {
                        (results ??= new()).Add(source);
                    }
                    else
                    {
                        return;
                    }
                }

                if (visited.Contains(source))
                {
                    return;
                }

                var innerScope = instanceSourcesScope.Enter(source);

                switch (source)
                {
                    case Registration { Constructor: { Parameters: var parameters } }:
                        {
                            foreach (var param in parameters)
                            {
                                var paramSource = innerScope.GetParameterSource(param);
                                Visit(paramSource, innerScope, ref results);
                            }

                            break;
                        }
                    case FactorySource { Underlying: var underlying }:
                        {
                            Visit(underlying, innerScope, ref results);
                            break;
                        }
                    case DelegateSource { ReturnType: var returnType }:
                        {
                            var returnTypeSource = innerScope[returnType];
                            Visit(returnTypeSource, innerScope, ref results);
                            break;
                        }
                    case ArraySource { Items: var items, }:
                        {
                            foreach (var item in items)
                            {
                                Visit(item, innerScope, ref results);
                            }

                            break;
                        }
                    case FactoryMethod { Method: { Parameters: var parameters } }:
                        {
                            foreach (var param in parameters)
                            {
                                var paramSource = innerScope.GetParameterSource(param);
                                Visit(paramSource, innerScope, ref results);
                            }

                            break;
                        }
                    case WrappedDecoratorInstanceSource(var decoratorSource, var underlyingInstanceSource):
                        {
                            switch (decoratorSource)
                            {
                                case DecoratorRegistration { Constructor: { Parameters: var parameters } }:
                                    {
                                        foreach (var param in parameters)
                                        {
                                            var paramSource = param.Ordinal == decoratorSource.decoratedParameter
                                                ? underlyingInstanceSource
                                                : innerScope.GetParameterSource(param);

                                            Visit(paramSource, innerScope, ref results);
                                        }

                                        break;
                                    }
                                case DecoratorFactoryMethod { Method: { Parameters: var parameters } }:
                                    {
                                        foreach (var param in parameters)
                                        {
                                            var paramSource = param.Ordinal == decoratorSource.decoratedParameter
                                                ? underlyingInstanceSource
                                                : innerScope.GetParameterSource(param);

                                            Visit(paramSource, innerScope, ref results);
                                        }

                                        break;
                                    }
                            }
                            break;
                        }
                    case ForwardedInstanceSource { Underlying: var underlying }:
                        {
                            Visit(underlying, innerScope, ref results);
                            break;
                        }
                    case InstanceFieldOrProperty:
                    case DelegateParameter:
                        break;
                    default: throw new NotImplementedException(source.GetType().ToString());
                }

                if (ReferenceEquals(instanceSourcesScope, containerScope) || ReferenceEquals(innerScope, containerScope))
                    visited.Add(source);
            }
        }

        private class ReferenceEqualityComparer : IEqualityComparer<InstanceSource>
        {
            private ReferenceEqualityComparer() { }

            public static ReferenceEqualityComparer Instance = new ReferenceEqualityComparer();

            public bool Equals(InstanceSource x, InstanceSource y)
            {
                return ReferenceEquals(x, y);
            }

            public int GetHashCode(InstanceSource obj)
            {
#pragma warning disable RS1024 // Compare symbols correctly
                return RuntimeHelpers.GetHashCode(obj);
#pragma warning restore RS1024 // Compare symbols correctly
            }
        }
    }
}
