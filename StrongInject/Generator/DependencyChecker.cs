using Microsoft.CodeAnalysis;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;

namespace StrongInject.Generator
{
    internal static class DependencyChecker
    {
        public static bool HasCircularOrMissingDependencies(ITypeSymbol target, bool isAsync, InstanceSourcesScope containerScope, Action<Diagnostic> reportDiagnostic, Location location)
        {
            var currentlyVisiting = new HashSet<ITypeSymbol>();
            var visited = new HashSet<ITypeSymbol>();
            return Visit(
                target,
                containerScope,
                usedParams: null,
                isScopeAsync: isAsync);

            // returns true if it has errors
            bool Visit(ITypeSymbol node, InstanceSourcesScope instanceSourcesScope, HashSet<IParameterSymbol>? usedParams, bool isScopeAsync)
            {
                var outerIsContainerScope = ReferenceEquals(instanceSourcesScope, containerScope);
                var innerIsContainerScope = outerIsContainerScope;
                if (outerIsContainerScope && visited.Contains(node))
                    return false;

                if (!currentlyVisiting.Add(node))
                {
                    reportDiagnostic(CircularDependency(location, target, node));
                    return true;
                }

                var result = false;
                if (!instanceSourcesScope.TryGetSource(node, out var instanceSources))
                {
                    reportDiagnostic(NoSourceForType(location, target, node));
                    result = true;
                }
                else if (instanceSources.Best is null)
                {
                    reportDiagnostic(NoBestSourceForType(location, target, node));
                    result = true;
                }
                else
                {
                    var instanceSource = instanceSources.Best;
                    instanceSourcesScope = instanceSourcesScope.Enter(instanceSource);
                    innerIsContainerScope = ReferenceEquals(instanceSourcesScope, containerScope);
                    if (innerIsContainerScope && visited.Contains(node))
                        return false;

                    switch (instanceSource)
                    {
                        case DelegateSource(var delegateType, var returnType, var delegateParameters, var isAsync) delegateSource:
                            {
                                foreach (var paramsWithType in delegateParameters.GroupBy(x => x.Type))
                                {
                                    if (paramsWithType.Count() > 1)
                                    {
                                        reportDiagnostic(DelegateHasMultipleParametersOfTheSameType(location, target, node, paramsWithType.Key));
                                        result = true;
                                    }
                                }

                                if (instanceSourcesScope.TryGetSource(returnType, out var returnTypeSource))
                                {
                                    if (returnTypeSource.Best is DelegateParameter { parameter: var param })
                                    {
                                        if (delegateParameters.Contains(param))
                                        {
                                            reportDiagnostic(WarnDelegateReturnTypeProvidedBySameDelegate(location, target, node, returnType));
                                        }
                                        else
                                        {
                                            reportDiagnostic(WarnDelegateReturnTypeProvidedByAnotherDelegate(location, target, node, returnType));
                                        }
                                    }
                                    else if (returnTypeSource.Best?.scope is Scope.SingleInstance)
                                    {
                                        reportDiagnostic(WarnDelegateReturnTypeIsSingleInstance(location, target, node, returnType));
                                    }
                                }

                                var usedByDelegateParams = usedParams ?? new();

                                result |= Visit(
                                    returnType,
                                    instanceSourcesScope,
                                    usedParams: usedByDelegateParams,
                                    isScopeAsync: isAsync);

                                foreach (var delegateParam in delegateParameters)
                                {
                                    if (delegateParam.RefKind != RefKind.None)
                                    {
                                        reportDiagnostic(DelegateParameterIsPassedByRef(location, target, node, delegateParam));
                                        result = true;
                                    }
                                    if (!usedByDelegateParams.Contains(delegateParam))
                                    {
                                        reportDiagnostic(WarnDelegateParameterNotUsed(location, target, node, delegateParam.Type, returnType));
                                    }
                                }

                                break;
                            }
                        case Registration { constructor: { Parameters: var parameters } }:
                            {
                                foreach (var param in parameters)
                                {
                                    result |= Visit(
                                        param.Type,
                                        instanceSourcesScope,
                                        usedParams,
                                        isScopeAsync);
                                }

                                break;
                            }

                        case FactoryRegistration { factoryType: var factoryType }:
                            result = Visit(
                                factoryType,
                                instanceSourcesScope,
                                usedParams,
                                isScopeAsync);
                            break;
                        case DelegateParameter { parameter: var parameter }:
                            usedParams!.Add(parameter);
                            break;
                    }

                    if (instanceSource is not DelegateSource && instanceSource.isAsync && !isScopeAsync)
                    {
                        reportDiagnostic(RequiresAsyncResolution(location, target, node));
                        result = true;
                    }
                }

                currentlyVisiting.Remove(node);
                if (outerIsContainerScope || innerIsContainerScope)
                {
                    visited.Add(node);
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

        /// <summary>
        /// Only call if <see cref="HasCircularOrMissingDependencies"/> returns true,
        /// as this does no validation, and could throw or stack overflow otherwise.
        /// </summary>
        internal static bool RequiresAsync(InstanceSource source, InstanceSourcesScope containerScope)
        {
            var visited = new HashSet<InstanceSource>();
            return Visit(source);

            bool Visit(InstanceSource source)
            {
                if (visited.Contains(source))
                    return false;

                if (source is DelegateSource)
                {
                    return false;
                }

                if (source.isAsync)
                {
                    return true;
                }

                if (source is Registration { constructor: { Parameters: var parameters } })
                {
                    foreach (var param in parameters)
                    {
                        var paramSource = containerScope[param.Type];
                        if (Visit(paramSource.Best!))
                            return true;
                    }
                }
                else if (source is FactoryRegistration { factoryType: var factoryType })
                {
                    var factorySource = containerScope[factoryType];
                    if (Visit(factorySource.Best!))
                        return true;
                }

                visited.Add(source);

                return false;
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

            void Visit(InstanceSource source, InstanceSourcesScope instanceSourcesScope, ref List<InstanceSource>? results)
            {
                if (source.scope == Scope.SingleInstance)
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
                    return;

                var innerScope = instanceSourcesScope.Enter(source);

                if (source is Registration { constructor: { Parameters: var parameters } })
                {
                    foreach (var param in parameters)
                    {
                        var paramSource = innerScope[param.Type];
                        Visit(paramSource.Best!, innerScope, ref results);
                    }
                }
                else if (source is FactoryRegistration { factoryType: var factoryType })
                {
                    var factorySource = innerScope[factoryType];
                    Visit(factorySource.Best!, innerScope, ref results);
                }
                else if (source is DelegateSource { returnType: var returnType })
                {
                    var returnTypeSource = innerScope[returnType];
                    Visit(returnTypeSource.Best!, innerScope, ref results);
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
                return RuntimeHelpers.GetHashCode(obj);
            }
        }
    }
}
