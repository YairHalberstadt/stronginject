using Microsoft.CodeAnalysis;
using System;
using System.Collections.Generic;

namespace StrongInject.Generator
{
    internal static class DependencySorter
    {
        public static IEnumerable<ITypeSymbol>? SortDependencies(ITypeSymbol target, IReadOnlyDictionary<ITypeSymbol, InstanceSource> registrations, Action<Diagnostic> reportDiagnostic, Location location)
        {
            var creationOrder = new List<ITypeSymbol>();
            var visited = new HashSet<ITypeSymbol>();
            var currentlyVisiting = new HashSet<ITypeSymbol>();
            if (!Visit(target))
                return null;
            return creationOrder;

            // returns false on error
            bool Visit(ITypeSymbol node)
            {
                if (visited.Contains(node))
                    return true;
                if (currentlyVisiting.Contains(node))
                {
                    reportDiagnostic(CircularDependency(location, target, node));
                    return false;
                }
                currentlyVisiting.Add(node);
                if (!registrations.TryGetValue(node, out var instanceSource))
                {
                    reportDiagnostic(NoSourceForType(location, target, node));
                    return false;
                }
                if (instanceSource is Registration { constructor: { Parameters: var parameters } })
                {
                    foreach (var param in parameters)
                    {
                        if(!Visit(param.Type))
                            return false;
                    }
                }
                currentlyVisiting.Remove(node);
                visited.Add(node);
                creationOrder.Add(node);
                return true;
            }
        }

        private static Diagnostic CircularDependency(Location location, ITypeSymbol target, ITypeSymbol type)
        {
            return Diagnostic.Create(
                new DiagnosticDescriptor(
                        "SI0101",
                        "Type contains circular dependency",
                        "Error whilst resolving dependencies for '{0}': '{1}' has a circular dependency",
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
                        "Error whilst resolving dependencies for '{0}': We have no source for instance of type '{1}'",
                        "StrongInject",
                        DiagnosticSeverity.Error,
                        isEnabledByDefault: true),
                    location,
                    target,
                    type);
        }
    }
}
