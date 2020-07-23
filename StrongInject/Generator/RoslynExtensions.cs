using Microsoft.CodeAnalysis;
using System;
using System.Collections.Generic;
using System.Linq;

namespace StrongInject.Generator
{
    internal static class RoslynExtensions
    {
        public static INamedTypeSymbol? GetType(this Compilation compilation, Type type) => compilation.GetTypeByMetadataName(type.FullName);
        public static INamedTypeSymbol? GetTypeOrReport(this Compilation compilation, Type type, Action<Diagnostic> reportDiagnostic)
        {
            var typeSymbol = compilation.GetType(type);
            if (typeSymbol is null)
            {
                reportDiagnostic(Diagnostic.Create(
                    MissingTypeDescriptor,
                    Location.None,
                    type));
            }
            return typeSymbol;
        }

        public static INamedTypeSymbol? GetTypeOrReport(this Compilation compilation, string metadataName, Action<Diagnostic> reportDiagnostic)
        {
            var typeSymbol = compilation.GetTypeByMetadataName(metadataName);
            if (typeSymbol is null)
            {
                reportDiagnostic(Diagnostic.Create(
                    MissingTypeDescriptor,
                    Location.None,
                    metadataName));
            }
            return typeSymbol;
        }

        private static DiagnosticDescriptor MissingTypeDescriptor => new DiagnosticDescriptor(
            "SI0201",
            "Missing Type",
            "Missing Type '{0}'. Are you missing an assembly reference?",
            "StrongInject",
            DiagnosticSeverity.Error,
            isEnabledByDefault: true);

        public static IEnumerable<INamedTypeSymbol> GetBaseTypesAndThis(this INamedTypeSymbol? namedType)
        {
            var current = namedType;
            while (current != null)
            {
                yield return current;
                current = current.BaseType;
            }
        }

        public static IEnumerable<INamedTypeSymbol> GetContainingTypesAndThis(this INamedTypeSymbol? namedType)
        {
            var current = namedType;
            while (current != null)
            {
                yield return current;
                current = current.ContainingType;
            }
        }

        public static bool IsOrReferencesErrorType(this ITypeSymbol type)
        {
            if (!type.ContainingType?.IsOrReferencesErrorType() ?? false)
                return false;
            return type switch
            {
                IErrorTypeSymbol => true,
                IArrayTypeSymbol array => array.ElementType.IsOrReferencesErrorType(),
                IPointerTypeSymbol pointer => pointer.PointedAtType.IsOrReferencesErrorType(),
                INamedTypeSymbol named => named.IsUnboundGenericType ? false : named.TypeArguments.Any(IsOrReferencesErrorType),
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

        public static string FullName(this ITypeSymbol type)
        {
            return type.ToDisplayString(new SymbolDisplayFormat(
                globalNamespaceStyle: SymbolDisplayGlobalNamespaceStyle.Included,
                typeQualificationStyle: SymbolDisplayTypeQualificationStyle.NameAndContainingTypesAndNamespaces,
                genericsOptions: SymbolDisplayGenericsOptions.IncludeTypeParameters));
        }

        public static string FullName(this INamespaceSymbol @namespace)
        {
            return @namespace.ToDisplayString(new SymbolDisplayFormat(typeQualificationStyle: SymbolDisplayTypeQualificationStyle.NameAndContainingTypesAndNamespaces));
        }

        public static string NameWithGenerics(this ITypeSymbol type)
        {
            return type.ToDisplayString(new SymbolDisplayFormat(genericsOptions: SymbolDisplayGenericsOptions.IncludeTypeParameters));
        }

        public static IEnumerable<INamedTypeSymbol> AllInterfacesAndSelf(this ITypeSymbol type)
        {
            if (type.TypeKind != TypeKind.Interface)
                return type.AllInterfaces;
            return type.AllInterfaces.Prepend((INamedTypeSymbol)type);
        }
    }
}