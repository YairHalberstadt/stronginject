using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using System.Threading;

namespace StrongInject.Generator
{
    internal static class RoslynExtensions
    {
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

        public static IEnumerable<INamedTypeSymbol> GetBaseTypes(this ITypeSymbol? namedType)
        {
            var current = namedType?.BaseType;
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
                INamedTypeSymbol named => !named.IsUnboundGenericType && named.TypeArguments.Any(IsOrReferencesErrorType),
                _ => false,
            };
        }

        public static bool IsAccessibleInternally(this ITypeSymbol type)
        {
            if (type is ITypeParameterSymbol)
                return true;
            if (!type.ContainingType?.IsAccessibleInternally() ?? false)
                return false;
            return type switch
            {
                IArrayTypeSymbol array => array.ElementType.IsAccessibleInternally(),
                IPointerTypeSymbol pointer => pointer.PointedAtType.IsAccessibleInternally(),
                INamedTypeSymbol named => named.DeclaredAccessibility is Accessibility.Public or Accessibility.ProtectedOrInternal or Accessibility.Internal 
                    && (named.IsUnboundGenericType || named.TypeArguments.All(IsAccessibleInternally)),
                _ => false,
            };
        }

        public static bool IsPublic(this ITypeSymbol type)
        {
            if (type is ITypeParameterSymbol)
                return true;
            if (!type.ContainingType?.IsPublic() ?? false)
                return false;
            return type switch
            {
                IArrayTypeSymbol array => array.ElementType.IsPublic(),
                IPointerTypeSymbol pointer => pointer.PointedAtType.IsPublic(),
                INamedTypeSymbol named => named.DeclaredAccessibility == Accessibility.Public && (named.IsUnboundGenericType || named.TypeArguments.All(IsPublic)),
                _ => false,
            };
        }

        public static bool IsInternal(this ITypeSymbol type)
        {
            return type.HasAtMostInternalVisibility() && type.IsAccessibleInternally();
        }

        public static bool HasAtMostInternalVisibility(this ITypeSymbol type)
        {
            if (type is ITypeParameterSymbol)
                return false;
            if (type.ContainingType?.HasAtMostInternalVisibility() ?? false)
                return true;
            return type switch
            {
                IArrayTypeSymbol array => array.ElementType.HasAtMostInternalVisibility(),
                IPointerTypeSymbol pointer => pointer.PointedAtType.HasAtMostInternalVisibility(),
                INamedTypeSymbol named => named.DeclaredAccessibility is Accessibility.Internal or Accessibility.Private or Accessibility.ProtectedAndInternal
                    || named.TypeArguments.Any(HasAtMostInternalVisibility),
                _ => false,
            };
        }

        public static bool IsProtected(this ISymbol member)
        {
            return member.DeclaredAccessibility is Accessibility.Protected or Accessibility.ProtectedOrInternal;
        }

        public static string FullName(this ITypeSymbol type)
        {
            return type.ToDisplayString(new SymbolDisplayFormat(
                globalNamespaceStyle: SymbolDisplayGlobalNamespaceStyle.Included,
                typeQualificationStyle: SymbolDisplayTypeQualificationStyle.NameAndContainingTypesAndNamespaces,
                genericsOptions: SymbolDisplayGenericsOptions.IncludeTypeParameters,
                parameterOptions: SymbolDisplayParameterOptions.IncludeType | SymbolDisplayParameterOptions.IncludeParamsRefOut,
                memberOptions: SymbolDisplayMemberOptions.IncludeRef));
        }

        public static string FullName(this INamespaceSymbol @namespace)
        {
            return @namespace.ToDisplayString(new SymbolDisplayFormat(typeQualificationStyle: SymbolDisplayTypeQualificationStyle.NameAndContainingTypesAndNamespaces));
        }

        public static string NameWithTypeParameters(this ITypeSymbol type)
        {
            return type.ToDisplayString(new SymbolDisplayFormat(
                globalNamespaceStyle: SymbolDisplayGlobalNamespaceStyle.Omitted,
                typeQualificationStyle: SymbolDisplayTypeQualificationStyle.NameOnly,
                genericsOptions: SymbolDisplayGenericsOptions.IncludeTypeParameters));
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

        public static Location GetLocation(this AttributeData attributeData, CancellationToken cancellationToken)
            => attributeData.ApplicationSyntaxReference?.GetSyntax(cancellationToken).GetLocation() ?? Location.None;

        public static bool IsWellKnownTaskType(this ITypeSymbol type, WellKnownTypes wellKnownTypes, out ITypeSymbol taskOfType)
        {
            if (type.OriginalDefinition.Equals(wellKnownTypes.Task1, SymbolEqualityComparer.Default)
                || type.OriginalDefinition.Equals(wellKnownTypes.ValueTask1, SymbolEqualityComparer.Default))
            {
                taskOfType = ((INamedTypeSymbol)type).TypeArguments[0];
                return true;
            }
            taskOfType = null!;
            return false;
        }

        public static bool IsWellKnownOwnedType(this ITypeSymbol type, WellKnownTypes wellKnownTypes, out bool isAsync, out ITypeSymbol valueType)
        {
            isAsync = type.OriginalDefinition.Equals(wellKnownTypes.AsyncOwned, SymbolEqualityComparer.Default);
            if (isAsync || type.OriginalDefinition.Equals(wellKnownTypes.Owned, SymbolEqualityComparer.Default))
            {
                valueType = ((INamedTypeSymbol)type).TypeArguments[0];
                return true;
            }

            valueType = null!;
            return false;
        }

        public static IEnumerable<AttributeData> Sort(this IEnumerable<AttributeData> attributes)
        {
            return attributes.OrderBy(x => x, AttributeComparer.Instance);
        }

        private class AttributeComparer : IComparer<AttributeData>
        {
            private AttributeComparer() { }

            public static readonly AttributeComparer Instance = new();
            public int Compare(AttributeData x, AttributeData y)
            {
                Debug.Assert(
                    x.AttributeClass is { ContainingNamespace: { Name: "StrongInject" } }
                    && y.AttributeClass is { ContainingNamespace: { Name: "StrongInject" } });

                var compareClass = string.CompareOrdinal(x.AttributeClass?.Name, y.AttributeClass?.Name);
                if (compareClass != 0)
                    return compareClass;

                return CompareArrays(x.ConstructorArguments, y.ConstructorArguments);
            }

            private static int CompareArrays(ImmutableArray<TypedConstant> arrayX, ImmutableArray<TypedConstant> arrayY)
            {
                var compareLength = arrayX.Length.CompareTo(arrayY.Length);
                if (compareLength != 0)
                    return compareLength;

                for (var i = 0; i < arrayX.Length; i++)
                {
                    var argX = arrayX[i];
                    var argY = arrayY[i];

                    var compareKinds = argX.Kind.CompareTo(argY.Kind);
                    if (compareKinds != 0)
                        return compareKinds;

                    if (argX.Kind != TypedConstantKind.Enum)
                    {
                        var compareTypes = string.CompareOrdinal(argX.Type?.Name, argY.Type?.Name);
                        if (compareTypes != 0)
                            return compareTypes;
                    }
                    else
                    {
                        var compareTypes = CompareTypes(argX.Type!, argY.Type!);
                        if (compareTypes != 0)
                            return compareTypes;
                    }

                    var compareIsNull = argX.IsNull.CompareTo(argY.IsNull);
                    if (compareIsNull != 0)
                        return compareIsNull;

                    var compareValues = argX.Kind switch
                    {
                        TypedConstantKind.Primitive or TypedConstantKind.Enum => argX.Value is string
                            ? StringComparer.Ordinal.Compare(argX.Value, argY.Value)
                            : Comparer.Default.Compare(argX.Value, argY.Value),
                        TypedConstantKind.Error => 0,
                        TypedConstantKind.Type => CompareTypes((ITypeSymbol)argX.Value!, (ITypeSymbol)argY.Value!),
                        TypedConstantKind.Array => CompareArrays(argX.Values, argY.Values),
                        _ => throw new InvalidOperationException("This location is thought to be impossible"),
                    };
                    if (compareValues != 0)
                        return compareValues;
                }

                return 0;
            }

            private static int CompareTypes(ITypeSymbol typeX, ITypeSymbol typeY)
            {
                var compareTypeNames = string.CompareOrdinal(typeX.Name, typeY.Name);
                if (compareTypeNames != 0)
                    return compareTypeNames;

                var namespaceX = typeX.ContainingNamespace;
                var namespaceY = typeY.ContainingNamespace;

                while (!namespaceX.IsGlobalNamespace && !namespaceY.IsGlobalNamespace)
                {
                    var compareNamespaces = string.CompareOrdinal(namespaceX.Name, namespaceY.Name);
                    if (compareNamespaces != 0)
                        return compareNamespaces;

                    namespaceX = namespaceX.ContainingNamespace;
                    namespaceY = namespaceY.ContainingNamespace;
                }

                return namespaceX.IsGlobalNamespace.CompareTo(namespaceY.IsGlobalNamespace);
            }
        }

        public static bool IsNonNullableValueType(this ITypeSymbol typeArgument)
        {
            if (!typeArgument.IsValueType)
            {
                return false;
            }

            return !IsNullableTypeOrTypeParameter(typeArgument);
        }

        public static bool IsNullableTypeOrTypeParameter(this ITypeSymbol? type)
        {
            if (type is null)
            {
                return false;
            }

            if (type.TypeKind == TypeKind.TypeParameter)
            {
                var constraintTypes = ((ITypeParameterSymbol)type).ConstraintTypes;
                foreach (var constraintType in constraintTypes)
                {
                    if (constraintType.IsNullableTypeOrTypeParameter())
                    {
                        return true;
                    }
                }
                return false;
            }

            return type.IsNullableType();
        }

        /// <summary>
        /// Is this System.Nullable`1 type, or its substitution.
        ///
        /// To check whether a type is System.Nullable`1 or is a type parameter constrained to System.Nullable`1
        /// use <see cref="IsNullableTypeOrTypeParameter" /> instead.
        /// </summary>
        public static bool IsNullableType(this ITypeSymbol type)
        {
            return type.OriginalDefinition.SpecialType == SpecialType.System_Nullable_T;
        }

        public static bool IsPointerOrFunctionPointer(this ITypeSymbol type)
        {
            switch (type.TypeKind)
            {
                case TypeKind.Pointer:
                case TypeKind.FunctionPointer:
                    return true;

                default:
                    return false;
            }
        }

        public static string ToIdentifier(this ITypeSymbol type, string defaultIfInvalidIdentifier)
        {
            var name = type.Name;
            return SyntaxFacts.IsValidIdentifier(name) ? name : defaultIfInvalidIdentifier;
        }

        public static string ToLowerCaseIdentifier(this ITypeSymbol type, string defaultIfInvalidIdentifier)
        {
            var name = type.Name;
            if (SyntaxFacts.IsValidIdentifier(name))
            {
                var array = name.ToCharArray();
                array[0] = char.ToLowerInvariant(array[0]);
                return new string(array);
            }
            else
            {
                return defaultIfInvalidIdentifier;
            }
        }

        public static ImmutableArray<ITypeParameterSymbol> TypeParameters(this ISymbol symbol)
        {
            return symbol switch
            {
                IMethodSymbol {TypeParameters: var tps} => tps,
                INamedTypeSymbol {TypeParameters: var tps} => tps,
                _ => throw new InvalidOperationException($"Symbol of kind '{symbol.Kind}' cannot have type parameters"),
            };
        }
        
        public static ISymbol? DeclaringSymbol(this ITypeParameterSymbol symbol)
        {
            return (ISymbol?)symbol.DeclaringMethod ?? symbol.DeclaringType;
        }
    }
}