using Microsoft.CodeAnalysis;
using System;
using System.Collections.Generic;

namespace StrongInject.Generator
{
    internal static class DependencyChecker
    {
        public static bool HasCircularOrMissingDependencies(ITypeSymbol target, IReadOnlyDictionary<ITypeSymbol, InstanceSource> registrations, Action<Diagnostic> reportDiagnostic, Location location)
        {
            var visited = new HashSet<ITypeSymbol>();
            var currentlyVisiting = new HashSet<ITypeSymbol>();
            return Visit(target);

            // returns true if it has errors
            bool Visit(ITypeSymbol node)
            {
                if (visited.Contains(node))
                    return false;
                if (currentlyVisiting.Contains(node))
                {
                    reportDiagnostic(CircularDependency(location, target, node));
                    return true;
                }
                currentlyVisiting.Add(node);
                if (!registrations.TryGetValue(node, out var instanceSource))
                {
                    reportDiagnostic(NoSourceForType(location, target, node));
                    return true;
                }
                if (instanceSource is Registration { constructor: { Parameters: var parameters } })
                {
                    foreach (var param in parameters)
                    {
                        if (Visit(param.Type))
                            return true;
                    }
                }
                else if (instanceSource is FactoryRegistration { factoryType: var factoryType })
                {
                    if (Visit(factoryType))
                        return true;
                }
                currentlyVisiting.Remove(node);
                visited.Add(node);
                return false;
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
                        "Type contains circular dependency",
                        "Error while resolving dependencies for '{0}': We have no source for instance of type '{1}'",
                        "StrongInject",
                        DiagnosticSeverity.Error,
                        isEnabledByDefault: true),
                    location,
                    target,
                    type);
        }
    }
}
