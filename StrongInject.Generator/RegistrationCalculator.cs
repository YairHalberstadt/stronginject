using Microsoft.CodeAnalysis;
using StrongInject.Runtime;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;

namespace StrongInject.Generator
{
    internal static class RegistrationCalculator
    {
        public static Dictionary<ITypeSymbol, Registration> CalculateRegistrations(
            INamedTypeSymbol containerOrModule,
            Compilation compilation,
            Action<Diagnostic> reportDiagnostic,
            CancellationToken cancellationToken)
        {
            var registrationAttributeType = compilation.GetType(typeof(RegistrationAttribute));
            var moduleRegistrationAttributeType = compilation.GetType(typeof(ModuleRegistrationAttribute));
            var iFactoryType = compilation.GetType(typeof(IFactory<>));
            var iRequiresInitializationType = compilation.GetType(typeof(IRequiresInitialization));
            if (registrationAttributeType is null || moduleRegistrationAttributeType is null || iFactoryType is null || iRequiresInitializationType is null)
                return new Dictionary<ITypeSymbol, Registration>(); //ToDo Report Diagnostic

            return new Dictionary<ITypeSymbol, Registration>(
                CalculateRegistrations(
                    containerOrModule,
                    compilation,
                    reportDiagnostic,
                    registrationAttributeType,
                    moduleRegistrationAttributeType,
                    iFactoryType,
                    iRequiresInitializationType,
                    cancellationToken));
        }

        private static IEnumerable<KeyValuePair<ITypeSymbol, Registration>> CalculateRegistrations(
            INamedTypeSymbol containerOrModule,
            Compilation compilation,
            Action<Diagnostic> reportDiagnostic,
            INamedTypeSymbol registrationAttributeType, 
            INamedTypeSymbol moduleRegistrationAttributeType,
            INamedTypeSymbol iFactoryType,
            INamedTypeSymbol iRequiresInitializationType,
            CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var attributes = containerOrModule.GetAttributes();

            var directRegistrations = CalculateDirectRegistrations(attributes, reportDiagnostic, registrationAttributeType, iFactoryType, iRequiresInitializationType, cancellationToken);

            var moduleRegistrations = new List<(AttributeData, Dictionary<ITypeSymbol, Registration> registrations)>();
            foreach (var moduleRegistrationAttribute in attributes.Where(x => x.AttributeClass?.Equals(moduleRegistrationAttributeType, SymbolEqualityComparer.Default) ?? false))
            {
                cancellationToken.ThrowIfCancellationRequested();
                var moduleConstant = moduleRegistrationAttribute.ConstructorArguments.FirstOrDefault();
                if (moduleConstant.Kind != TypedConstantKind.Type)
                {
                    // Invalid code, ignore;
                    continue;
                }
                var moduleType = (INamedTypeSymbol)moduleConstant.Value!;

                var exclusionListConstants = moduleRegistrationAttribute.ConstructorArguments.FirstOrDefault(x => x.Kind == TypedConstantKind.Array).Values;
                var exclusionList = exclusionListConstants.IsDefault 
                    ? new HashSet<INamedTypeSymbol>()
                    : exclusionListConstants.Select(x => x.Value).OfType<INamedTypeSymbol>().ToHashSet<INamedTypeSymbol>(SymbolEqualityComparer.Default); 

                var registrations = CalculateRegistrations(
                    moduleType,
                    compilation,
                    reportDiagnostic,
                    registrationAttributeType,
                    moduleRegistrationAttributeType,
                    iFactoryType,
                    iRequiresInitializationType,
                    cancellationToken);

                var thisModuleRegistrations = new Dictionary<ITypeSymbol, Registration>();
                foreach(var (type, registration) in registrations)
                {
                    if (exclusionList.Contains(type))
                        continue;
                    if (directRegistrations.ContainsKey(type))
                        continue;
                    var use = true;
                    foreach(var (otherModuleRegistrationAttribute, otherModuleRegistrations) in moduleRegistrations)
                    {
                        if (otherModuleRegistrations.ContainsKey(type))
                        {
                            use = false;
                            reportDiagnostic(RegisteredByMultipleModules(moduleRegistrationAttribute, moduleType, type, otherModuleRegistrationAttribute, cancellationToken));
                            reportDiagnostic(RegisteredByMultipleModules(otherModuleRegistrationAttribute, moduleType, type, otherModuleRegistrationAttribute, cancellationToken));
                            break;
                        }
                    }
                    if (!use)
                        continue;

                    thisModuleRegistrations.Add(type, registration);
                }
                moduleRegistrations.Add((moduleRegistrationAttribute, thisModuleRegistrations));
            }

            return directRegistrations.Concat(moduleRegistrations.SelectMany(x => x.registrations));
        }

        private static Diagnostic RegisteredByMultipleModules(AttributeData attributeForLocation, INamedTypeSymbol moduleType, ITypeSymbol type, AttributeData otherModuleRegistrationAttribute, CancellationToken cancellationToken)
        {
            return Diagnostic.Create(
                new DiagnosticDescriptor(
                    "SI0002",
                    "Type registered by multiple modules",
                    "'{0}' is registered by both modules '{1}' and '{2}'.",
                    "StrongInject",
                    DiagnosticSeverity.Error,
                    isEnabledByDefault: true),
                attributeForLocation.ApplicationSyntaxReference?.GetSyntax(cancellationToken).GetLocation() ?? Location.None,
                type,
                otherModuleRegistrationAttribute.ConstructorArguments[0].Value,
                moduleType);
        }

        public static Dictionary<ITypeSymbol, Registration> CalculateDirectRegistrations(
            ImmutableArray<AttributeData> attributes,
            Action<Diagnostic> reportDiagnostic,
            INamedTypeSymbol registrationAttributeType,
            INamedTypeSymbol iFactoryType,
            INamedTypeSymbol iRequiresInitializationType,
            CancellationToken cancellationToken)
        {
            var directRegistrations = new Dictionary<ITypeSymbol, Registration>();
            foreach (var registrationAttribute in attributes.Where(x => x.AttributeClass?.Equals(registrationAttributeType, SymbolEqualityComparer.Default) ?? false))
            {
                cancellationToken.ThrowIfCancellationRequested();
                var countConstructorArguments = registrationAttribute.ConstructorArguments.Length;
                if (countConstructorArguments is not (2 or 3))
                {
                    // Invalid code, ignore;
                    continue;
                }

                var typeConstant = registrationAttribute.ConstructorArguments[0];
                if (typeConstant.Kind != TypedConstantKind.Type)
                {
                    // Invalid code, ignore;
                    continue;
                }
                if (typeConstant.Value is not INamedTypeSymbol type || type.ReferencesTypeParametersOrErrorTypes())
                {
                    reportDiagnostic(InvalidType(
                        (ITypeSymbol)typeConstant.Value!,
                        registrationAttribute.ApplicationSyntaxReference?.GetSyntax(cancellationToken).GetLocation() ?? Location.None));
                    continue;
                }
                else if (!type.IsPublic())
                {
                    reportDiagnostic(TypeNotPublic(
                        (ITypeSymbol)typeConstant.Value!,
                        registrationAttribute.ApplicationSyntaxReference?.GetSyntax(cancellationToken).GetLocation() ?? Location.None));
                    continue;
                }

                IMethodSymbol constructor;
                var applicableConstructors = type.Constructors.Where(x => x.DeclaredAccessibility == Accessibility.Public).ToList();
                if (applicableConstructors.Count == 0)
                {
                    reportDiagnostic(NoConstructor(registrationAttribute, type, cancellationToken));
                    continue;
                }
                else if (applicableConstructors.Count == 1)
                {
                    constructor = applicableConstructors[0];
                }
                else
                {
                    var nonDefaultConstructors = applicableConstructors.Where(x => x.Parameters.Length != 0).ToList();
                    if (nonDefaultConstructors.Count == 0)
                    {
                        // We should only be able to get here in an error case. Take the first constructor.
                        constructor = applicableConstructors[0];
                    }
                    else if (nonDefaultConstructors.Count == 1)
                    {
                        constructor = nonDefaultConstructors[0];
                    }
                    else
                    {
                        reportDiagnostic(MultipleConstructors(registrationAttribute, type, cancellationToken));
                        continue;
                    }
                }

                if(constructor.Parameters.Any(x => x.Type is not INamedTypeSymbol))
                {
                    reportDiagnostic(ConstructorParameterNonNamedTypeSymbol(registrationAttribute, type, constructor, cancellationToken));
                    continue;
                }

                var lifeTime = countConstructorArguments == 3 && registrationAttribute.ConstructorArguments[1] is { Kind: TypedConstantKind.Enum, Value: int value }
                    ? (Lifetime)value
                    : Lifetime.InstancePerDependency;

                var registeredAsConstants = registrationAttribute.ConstructorArguments[countConstructorArguments - 1].Values;
                var registeredAs = registeredAsConstants.IsDefaultOrEmpty ? new[] { type } : registeredAsConstants.Select(x => x.Value).OfType<INamedTypeSymbol>().ToArray();

                var interfacesAndBaseTypes = type.GetBaseTypesAndThis().Concat(type.AllInterfaces).ToHashSet(SymbolEqualityComparer.Default);
                foreach (var directTarget in registeredAs)
                {
                    if (directTarget.ReferencesTypeParametersOrErrorTypes())
                    {
                        reportDiagnostic(InvalidType(
                            directTarget,
                            registrationAttribute.ApplicationSyntaxReference?.GetSyntax(cancellationToken).GetLocation() ?? Location.None));
                        continue;
                    }
                    else if (!directTarget.IsPublic())
                    {
                        reportDiagnostic(TypeNotPublic(
                            (ITypeSymbol)typeConstant.Value!,
                            registrationAttribute.ApplicationSyntaxReference?.GetSyntax(cancellationToken).GetLocation() ?? Location.None));
                        continue;
                    }

                    if (!interfacesAndBaseTypes.Contains(directTarget))
                    {
                        reportDiagnostic(DoesNotImplement(registrationAttribute, type, directTarget, cancellationToken));
                        continue;
                    }

                    var requiresInitialization = interfacesAndBaseTypes.Contains(iRequiresInitializationType);
                    if (directRegistrations.ContainsKey(directTarget))
                    {
                        reportDiagnostic(DuplicateRegistration(registrationAttribute, directTarget, cancellationToken));
                        continue;
                    }

                    directRegistrations.Add(directTarget, new Registration(type, directTarget, lifeTime, directTarget, isFactory: false, requiresInitialization, constructor));

                    if (directTarget.OriginalDefinition.Equals(iFactoryType, SymbolEqualityComparer.Default))
                    {
                        var factoryTarget = directTarget.TypeArguments.First();

                        if (directRegistrations.ContainsKey(factoryTarget))
                        {
                            reportDiagnostic(DuplicateRegistration(registrationAttribute, factoryTarget, cancellationToken));
                            continue;
                        }

                        directRegistrations.Add(factoryTarget, new Registration(type, factoryTarget, lifeTime, directTarget, isFactory: true, requiresInitialization, constructor));
                    }
                }
            }
            return directRegistrations;
        }

        private static Diagnostic ConstructorParameterNonNamedTypeSymbol(AttributeData registrationAttribute, INamedTypeSymbol type, IMethodSymbol constructor, CancellationToken cancellationToken)
        {
            return
                Diagnostic.Create(
                    new DiagnosticDescriptor(
                        "SI0008",
                        "Constructor has parameter not of named type symbol",
                        "'{0}' does not have any public constructors.",
                        "StrongInject",
                        DiagnosticSeverity.Error,
                        isEnabledByDefault: true),
                    registrationAttribute.ApplicationSyntaxReference?.GetSyntax(cancellationToken).GetLocation() ?? Location.None,
                    type);
        }

        private static Diagnostic NoConstructor(AttributeData registrationAttribute, INamedTypeSymbol type, CancellationToken cancellationToken)
        {
            return
                Diagnostic.Create(
                    new DiagnosticDescriptor(
                        "SI0005",
                        "Registered type does not have any public constructors",
                        "'{0}' does not have any public constructors.",
                        "StrongInject",
                        DiagnosticSeverity.Error,
                        isEnabledByDefault: true),
                    registrationAttribute.ApplicationSyntaxReference?.GetSyntax(cancellationToken).GetLocation() ?? Location.None,
                    type);
        }

        private static Diagnostic MultipleConstructors(AttributeData registrationAttribute, INamedTypeSymbol type, CancellationToken cancellationToken)
        {
            return Diagnostic.Create(
                new DiagnosticDescriptor(
                    "SI0006",
                    "Registered type has multiple non-default public constructors",
                    "'{0}' has multiple non-default public constructors.",
                    "StrongInject",
                    DiagnosticSeverity.Error,
                    isEnabledByDefault: true),
                registrationAttribute.ApplicationSyntaxReference?.GetSyntax(cancellationToken).GetLocation() ?? Location.None,
                type);
        }

        private static Diagnostic DoesNotImplement(AttributeData registrationAttribute, INamedTypeSymbol registeredType, INamedTypeSymbol registeredAsType, CancellationToken cancellationToken)
        {
            return Diagnostic.Create(
                new DiagnosticDescriptor(
                    "SI0001",
                    "Registered type does not implement registered as type",
                    "'{0}' does not implement '{1}'.",
                    "StrongInject",
                    DiagnosticSeverity.Error,
                    isEnabledByDefault: true),
                registrationAttribute.ApplicationSyntaxReference?.GetSyntax(cancellationToken).GetLocation() ?? Location.None,
                registeredType,
                registeredAsType);
        }

        private static Diagnostic DuplicateRegistration(AttributeData registrationAttribute, ITypeSymbol registeredAsType, CancellationToken cancellationToken)
        {
            return Diagnostic.Create(
                new DiagnosticDescriptor(
                    "SI0004",
                    "Module already contains registration for type",
                    "Module already contains registration for '{0}'.",
                    "StrongInject",
                    DiagnosticSeverity.Error,
                    isEnabledByDefault: true),
                registrationAttribute.ApplicationSyntaxReference?.GetSyntax(cancellationToken).GetLocation() ?? Location.None,
                registeredAsType);
        }

        private static Diagnostic InvalidType(ITypeSymbol typeSymbol, Location location)
        {
            return Diagnostic.Create(
                new DiagnosticDescriptor(
                    "SI0003",
                    "Type is invalid in a registration",
                    "'{0}' is invalid in a registration.",
                    "StrongInject",
                    DiagnosticSeverity.Error,
                    isEnabledByDefault: true),
                location,
                typeSymbol);
        }

        private static Diagnostic TypeNotPublic(ITypeSymbol typeSymbol, Location location)
        {
            return Diagnostic.Create(
                new DiagnosticDescriptor(
                    "SI0007",
                    "Type is not public",
                    "'{0}' is not public.",
                    "StrongInject",
                    DiagnosticSeverity.Error,
                    isEnabledByDefault: true),
                location,
                typeSymbol);
        }
    }
}
