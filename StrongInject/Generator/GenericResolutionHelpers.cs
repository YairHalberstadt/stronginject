using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using System.Collections.Immutable;

namespace StrongInject.Generator
{
    internal static class GenericResolutionHelpers
    {
        public static bool CanConstructFromGenericMethodReturnType(Compilation compilation, ITypeSymbol toConstruct, ITypeSymbol toConstructFrom, IMethodSymbol method, out IMethodSymbol constructedMethod, out bool constraintsDoNotMatch)
        {
            if (CanConstructFromReturnType(toConstruct, toConstructFrom, method, out var typeArguments)
                && SatisfiesConstraints(method, typeArguments, compilation))
            {
                constructedMethod = method.Construct(typeArguments);
                constraintsDoNotMatch = false;
                return true;
            }
            constructedMethod = null!;
            constraintsDoNotMatch = false;
            return false;
        }

        private static bool CanConstructFromReturnType(ITypeSymbol toConstruct, ITypeSymbol toConstructFrom, IMethodSymbol method, out ITypeSymbol[] typeArguments)
        {
            typeArguments = null!;
            return CanConstructFrom(toConstruct, toConstructFrom, method, ref typeArguments);
            static bool CanConstructFrom(ITypeSymbol toConstruct, ITypeSymbol toConstructFrom, IMethodSymbol method, ref ITypeSymbol[] typeArguments)
            {
                switch (toConstructFrom)
                {
                    case ITypeParameterSymbol typeParameterSymbol:

                        if (!SymbolEqualityComparer.Default.Equals(typeParameterSymbol.DeclaringMethod, method))
                        {
                            return SymbolEqualityComparer.Default.Equals(toConstruct, toConstructFrom);
                        }

                        var currentTypeArgumentForOrdinal = typeArguments?[typeParameterSymbol.Ordinal] ?? null;
                        if (currentTypeArgumentForOrdinal is null)
                        {
                            (typeArguments ??= new ITypeSymbol[method.TypeParameters.Length])[typeParameterSymbol.Ordinal] = toConstruct;
                            return true;
                        }
                        return SymbolEqualityComparer.Default.Equals(toConstruct, currentTypeArgumentForOrdinal);

                    case IArrayTypeSymbol { Rank: var rank, ElementType: var elementType }:

                        if (toConstruct is IArrayTypeSymbol { Rank: var toConstructRank, ElementType: var elementTypeToConstruct })
                        {
                            return rank == toConstructRank && CanConstructFrom(elementTypeToConstruct, elementType, method, ref typeArguments);
                        }
                        return false;

                    case INamedTypeSymbol { OriginalDefinition: var originalDefinition, TypeArguments: var fromTypeArguments }:

                        if (!SymbolEqualityComparer.Default.Equals(originalDefinition, toConstruct.OriginalDefinition))
                        {
                            return false;
                        }

                        var typeArgumentsToConstruct = ((INamedTypeSymbol)toConstruct).TypeArguments;

                        for (var i = 0; i < fromTypeArguments.Length; i++)
                        {
                            var typeArgument = fromTypeArguments[i];
                            var typeArgumentToConstruct = typeArgumentsToConstruct[i];

                            if (!CanConstructFrom(typeArgumentToConstruct, typeArgument, method, ref typeArguments))
                            {
                                return false;
                            }
                        }

                        return true;
                }

                return false;
            }
        }

        private static bool SatisfiesConstraints(IMethodSymbol method, ITypeSymbol[] typeArguments, Compilation compilation)
        {
            var typeParameters = method.TypeParameters;
            for (int i = 0; i < typeParameters.Length; i++)
            {
                var typeParameter = typeParameters[i];
                var typeArgument = typeArguments[i];

                if (typeArgument.IsPointerOrFunctionPointer() || typeArgument.IsRefLikeType)
                {
                    return false;
                }

                if (typeParameter.HasReferenceTypeConstraint && !typeArgument.IsReferenceType
                    || typeParameter.HasValueTypeConstraint && !typeArgument.IsNonNullableValueType()
                    || typeParameter.HasUnmanagedTypeConstraint && !(typeArgument.IsUnmanagedType && typeArgument.IsNonNullableValueType())
                    || typeParameter.HasConstructorConstraint && !SatisfiesConstructorConstraint(typeArgument))
                {
                    return false;
                }

                foreach (var typeConstraint in typeParameter.ConstraintTypes)
                {
                    var substitutedConstraintType = SubstituteType(compilation, typeConstraint, method, typeArguments);
                    var conversion = compilation.ClassifyConversion(typeArgument, substitutedConstraintType);
                    if (typeArgument.IsNullableType() || conversion is not ({ IsIdentity: true } or { IsImplicit: true, IsReference: true } or { IsBoxing: true }))
                    {
                        return false;
                    }
                }
            }
            return true;
        }

        private static bool SatisfiesConstructorConstraint(ITypeSymbol typeArgument)
        {
            switch (typeArgument.TypeKind)
            {
                case TypeKind.Struct:
                case TypeKind.Enum:
                case TypeKind.Dynamic:
                    return true;

                case TypeKind.Class:
                    return HasPublicParameterlessConstructor((INamedTypeSymbol)typeArgument) && !typeArgument.IsAbstract;

                case TypeKind.TypeParameter:
                    {
                        var typeParameter = (ITypeParameterSymbol)typeArgument;
                        return typeParameter.HasConstructorConstraint || typeParameter.IsValueType;
                    }

                default:
                    return false;
            }
        }

        private static bool HasPublicParameterlessConstructor(INamedTypeSymbol type)
        {
            foreach (var constructor in type.InstanceConstructors)
            {
                if (constructor.Parameters.Length == 0)
                {
                    return constructor.DeclaredAccessibility == Accessibility.Public;
                }
            }
            return false;
        }

        private static ITypeSymbol SubstituteType(Compilation compilation, ITypeSymbol type, IMethodSymbol method, ITypeSymbol[] typeArguments)
        {
            return Visit(type);

            ITypeSymbol Visit(ITypeSymbol type)
            {
                switch (type)
                {
                    case ITypeParameterSymbol typeParameterSymbol:
                        return SymbolEqualityComparer.Default.Equals(typeParameterSymbol.DeclaringMethod, method)
                            ? typeArguments[typeParameterSymbol.Ordinal]
                            : type;
                    case IArrayTypeSymbol { ElementType: var elementType, Rank: var rank } arrayTypeSymbol:
                        var visitedElementType = Visit(elementType);
                        return ReferenceEquals(elementType, visitedElementType)
                            ? arrayTypeSymbol
                            : compilation.CreateArrayTypeSymbol(visitedElementType, rank);
                    case INamedTypeSymbol { OriginalDefinition: var originalDefinition, TypeArguments: var typeArguments } namedTypeSymbol:
                        var visitedTypeArguments = new ITypeSymbol[typeArguments.Length];
                        var anyChanged = false;
                        for (var i = 0; i < typeArguments.Length; i++)
                        {
                            var typeArgument = typeArguments[i];
                            var visited = Visit(typeArgument);
                            if (!ReferenceEquals(visited, typeArgument))
                                anyChanged = true;
                            visitedTypeArguments[i] = visited;
                        }
                        return anyChanged ? originalDefinition.Construct(visitedTypeArguments) : namedTypeSymbol;
                    default: return type;
                }
            }
        }
    }
}
