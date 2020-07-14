using Microsoft.CodeAnalysis;
using System;
using System.Collections.Generic;
using System.Linq;

namespace StrongInject.Generator
{
    public static class RoslynExtensions
    {
        public static INamedTypeSymbol? GetType(this Compilation compilation, Type type) => compilation.GetTypeByMetadataName(type.FullName);

        public static IEnumerable<INamedTypeSymbol> GetBaseTypesAndThis(this INamedTypeSymbol? namedType)
        {
            var current = namedType;
            while (current != null)
            {
                yield return current;
                current = current.BaseType;
            }
        }

        public static bool ReferencesTypeParametersOrErrorTypes(this ITypeSymbol type)
        {
            if (type.ContainingType?.ReferencesTypeParametersOrErrorTypes() ?? false)
                return true;
            return type switch
            {
                ITypeParameterSymbol or IErrorTypeSymbol => true,
                IArrayTypeSymbol array => array.ElementType.ReferencesTypeParametersOrErrorTypes(),
                IPointerTypeSymbol pointer => pointer.PointedAtType.ReferencesTypeParametersOrErrorTypes(),
                INamedTypeSymbol named => named.TypeArguments.Any(ReferencesTypeParametersOrErrorTypes),
                _ => false,
            };
        }

        public static bool IsPublic(this ITypeSymbol type)
        {
            if (!type.ContainingType?.IsPublic() ?? false)
                return false;
            return type switch
            {
                IArrayTypeSymbol array => array.ElementType.IsPublic(),
                IPointerTypeSymbol pointer => pointer.PointedAtType.IsPublic(),
                INamedTypeSymbol named => named.DeclaredAccessibility == Accessibility.Public && named.TypeArguments.All(IsPublic),
                _ => false,
            };
        }

        public static string FullName(this INamespaceOrTypeSymbol namespaceOrType)
        {
            return namespaceOrType.ToDisplayString(new SymbolDisplayFormat(
                globalNamespaceStyle: SymbolDisplayGlobalNamespaceStyle.Included,
                typeQualificationStyle: SymbolDisplayTypeQualificationStyle.NameAndContainingTypesAndNamespaces,
                genericsOptions: SymbolDisplayGenericsOptions.IncludeTypeParameters));
        }
    }
}