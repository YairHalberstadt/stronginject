using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;

namespace StrongInject.Generator
{
    internal class RegistrationCalculator
    {
        public RegistrationCalculator(
            Compilation compilation,
            Action<Diagnostic> reportDiagnostic,
            CancellationToken cancellationToken)
        {
            _compilation = compilation;
            _reportDiagnostic = reportDiagnostic;
            _cancellationToken = cancellationToken;
            _registrationAttributeType = compilation.GetTypeOrReport(typeof(RegistrationAttribute), reportDiagnostic)!;
            _factoryRegistrationAttributeType = compilation.GetTypeOrReport(typeof(FactoryRegistrationAttribute), reportDiagnostic)!;
            _moduleRegistrationAttributeType = compilation.GetTypeOrReport(typeof(ModuleRegistrationAttribute), _reportDiagnostic)!;
            _iFactoryType = compilation.GetTypeOrReport(typeof(IFactory<>), _reportDiagnostic)!;
            _iRequiresInitializationType = compilation.GetTypeOrReport(typeof(IRequiresInitialization), _reportDiagnostic)!;
            if (_registrationAttributeType is null 
                || _factoryRegistrationAttributeType is null 
                || _moduleRegistrationAttributeType is null 
                || _iFactoryType is null 
                || _iRequiresInitializationType is null)
            {
                _valid = false;
            }
            else
            {
                _valid = true;
            }
        }

        private readonly Dictionary<INamedTypeSymbol, Dictionary<ITypeSymbol, InstanceSource>> _registrations = new();
        private readonly INamedTypeSymbol _registrationAttributeType;
        private readonly INamedTypeSymbol _factoryRegistrationAttributeType;
        private readonly INamedTypeSymbol _moduleRegistrationAttributeType;
        private readonly INamedTypeSymbol _iFactoryType;
        private readonly INamedTypeSymbol _iRequiresInitializationType;
        private readonly Compilation _compilation;
        private readonly Action<Diagnostic> _reportDiagnostic;
        private readonly CancellationToken _cancellationToken;
        private readonly bool _valid;

        public IReadOnlyDictionary<ITypeSymbol, InstanceSource> GetRegistrations(INamedTypeSymbol module)
        {
            if (!_valid)
            {
                return ImmutableDictionary<ITypeSymbol, InstanceSource>.Empty;
            }

            return GetRegistrations(module, dependantModules: null);
        }

        private IReadOnlyDictionary<ITypeSymbol, InstanceSource> GetRegistrations(INamedTypeSymbol module, HashSet<INamedTypeSymbol>? dependantModules)
        {
            if (!_registrations.TryGetValue(module, out var registrations))
            {
                if (!(dependantModules ??= new()).Add(module))
                {
                    _reportDiagnostic(RecursiveModuleRegistration(module, _cancellationToken));
                    return ImmutableDictionary<ITypeSymbol, InstanceSource>.Empty;
                }

                registrations = CalculateRegistrations(module, dependantModules);
                _registrations[module] = registrations;
            }
            return registrations;
        }

        private Dictionary<ITypeSymbol, InstanceSource> CalculateRegistrations(INamedTypeSymbol module, HashSet<INamedTypeSymbol>? dependantModules)
        {
            _cancellationToken.ThrowIfCancellationRequested();

            var attributes = module.GetAttributes();

            var thisModuleRegistrations = CalculateThisModuleRegistrations(attributes);

            var importedModulesRegistrations = new List<(AttributeData moduleRegistrationAttribute, Dictionary<ITypeSymbol, InstanceSource> registrations)>();
            foreach (var moduleRegistrationAttribute in attributes.Where(x => x.AttributeClass?.Equals(_moduleRegistrationAttributeType, SymbolEqualityComparer.Default) ?? false))
            {
                _cancellationToken.ThrowIfCancellationRequested();
                var moduleConstant = moduleRegistrationAttribute.ConstructorArguments.FirstOrDefault();
                if (moduleConstant.Kind != TypedConstantKind.Type)
                {
                    // Invalid code, ignore;
                    continue;
                }
                var moduleType = (INamedTypeSymbol)moduleConstant.Value!;
                if (moduleType.IsOrReferencesErrorType())
                {
                    // Invalid code, ignore;
                    continue;
                }

                var exclusionListConstant = moduleRegistrationAttribute.ConstructorArguments.FirstOrDefault(x => x.Kind == TypedConstantKind.Array);
                if (exclusionListConstant.Kind is not TypedConstantKind.Array)
                {
                    // Invalid code, ignore;
                    continue;
                }
                var exclusionListConstants = exclusionListConstant.Values;
                var exclusionList = exclusionListConstants.IsDefault
                    ? new HashSet<INamedTypeSymbol>()
                    : exclusionListConstants.Select(x => x.Value).OfType<INamedTypeSymbol>().ToHashSet<INamedTypeSymbol>(SymbolEqualityComparer.Default);

                var registrations = GetRegistrations(moduleType, dependantModules);

                var importedModuleRegistrations = new Dictionary<ITypeSymbol, InstanceSource>();
                foreach (var (type, registration) in registrations)
                {
                    if (exclusionList.Contains(type))
                        continue;
                    if (thisModuleRegistrations.ContainsKey(type))
                        continue;
                    var use = true;
                    foreach (var (otherModuleRegistrationAttribute, otherImportedModuleRegistrations) in importedModulesRegistrations)
                    {
                        if (otherImportedModuleRegistrations.TryGetValue(type, out var otherRegistration))
                        {
                            use = false;
                            if (!registration.Equals(otherRegistration))
                            {
                                _reportDiagnostic(RegisteredByMultipleModules(moduleRegistrationAttribute, moduleType, otherModuleRegistrationAttribute, type, _cancellationToken));
                                _reportDiagnostic(RegisteredByMultipleModules(otherModuleRegistrationAttribute, moduleType, otherModuleRegistrationAttribute, type, _cancellationToken));
                            }
                            break;
                        }
                    }
                    if (!use)
                        continue;

                    importedModuleRegistrations.Add(type, registration);
                }
                importedModulesRegistrations.Add((moduleRegistrationAttribute, importedModuleRegistrations));
            }

            return thisModuleRegistrations.Concat(importedModulesRegistrations.SelectMany(x => x.registrations)).ToDictionary(x => x.Key, x => x.Value);
        }

        private Dictionary<ITypeSymbol, InstanceSource> CalculateThisModuleRegistrations(ImmutableArray<AttributeData> moduleAttributes)
        {
            var registrations = new Dictionary<ITypeSymbol, InstanceSource>();
            AppendSimpleRegistrations(registrations, moduleAttributes);
            AppendFactoryRegistrations(registrations, moduleAttributes);
            return registrations;
        }

        private void AppendSimpleRegistrations(Dictionary<ITypeSymbol, InstanceSource> registrations, ImmutableArray<AttributeData> moduleAttributes)
        {
            foreach (var registrationAttribute in moduleAttributes.Where(x => x.AttributeClass?.Equals(_registrationAttributeType, SymbolEqualityComparer.Default) ?? false))
            {
                _cancellationToken.ThrowIfCancellationRequested();
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
                if (!CheckValidType(registrationAttribute, typeConstant, out var type))
                {
                    continue;
                }
                if (type.IsAbstract)
                {
                    _reportDiagnostic(TypeIsAbstract(
                        type!,
                        registrationAttribute.GetLocation(_cancellationToken)));
                    continue;
                }

                if (!TryGetConstructor(registrationAttribute, type, out var constructor))
                {
                    continue;
                }

                var scope = countConstructorArguments is 3 && registrationAttribute.ConstructorArguments[1] is { Kind: TypedConstantKind.Enum, Value: int scopeInt }
                    ? (Scope)scopeInt
                    : Scope.InstancePerResolution;

                if (scope == Scope.SingleInstance && type.TypeKind == TypeKind.Struct)
                {
                    _reportDiagnostic(StructWithSingleInstanceScope(registrationAttribute, type, _cancellationToken));
                    continue;
                }

                var registeredAsConstant = registrationAttribute.ConstructorArguments[countConstructorArguments - 1];
                if (registeredAsConstant.Kind is not TypedConstantKind.Array)
                {
                    // error case.
                    continue;
                }
                var registeredAsConstants = registeredAsConstant.Values;
                var registeredAs = registeredAsConstants.IsDefaultOrEmpty
                    ? new[] { typeConstant }
                    : registeredAsConstants.Where(x => x.Kind == TypedConstantKind.Type).ToArray();

                var requiresInitialization = type.AllInterfaces.Contains(_iRequiresInitializationType);

                foreach (var registeredAsTypeConstant in registeredAs)
                {
                    if (!CheckValidType(registrationAttribute, registeredAsTypeConstant, out var directTarget))
                    {
                        continue;
                    }

                    if (_compilation.ClassifyCommonConversion(type, directTarget) is not { IsImplicit: true, IsNumeric: false, IsUserDefined: false })
                    {
                        _reportDiagnostic(DoesNotHaveSuitableConversion(registrationAttribute, type, directTarget, _cancellationToken));
                        continue;
                    }

                    if (registrations.ContainsKey(directTarget))
                    {
                        _reportDiagnostic(DuplicateRegistration(registrationAttribute, directTarget, _cancellationToken));
                        continue;
                    }

                    registrations.Add(directTarget, new Registration(type, directTarget, scope, requiresInitialization, constructor));
                }
            }
        }

        private void AppendFactoryRegistrations(Dictionary<ITypeSymbol, InstanceSource> registrations, ImmutableArray<AttributeData> moduleAttributes)
        {
            foreach (var factoryRegistrationAttribute in moduleAttributes.Where(x => x.AttributeClass?.Equals(_factoryRegistrationAttributeType, SymbolEqualityComparer.Default) ?? false))
            {
                _cancellationToken.ThrowIfCancellationRequested();
                var countConstructorArguments = factoryRegistrationAttribute.ConstructorArguments.Length;
                if (countConstructorArguments != 3)
                {
                    // Invalid code, ignore;
                    continue;
                }

                var typeConstant = factoryRegistrationAttribute.ConstructorArguments[0];
                if (typeConstant.Kind != TypedConstantKind.Type)
                {
                    // Invalid code, ignore;
                    continue;
                }
                if (!CheckValidType(factoryRegistrationAttribute, typeConstant, out var type))
                {
                    continue;
                }
                if (type.IsAbstract)
                {
                    _reportDiagnostic(TypeIsAbstract(
                        type!,
                        factoryRegistrationAttribute.GetLocation(_cancellationToken)));
                    continue;
                }

                if (!TryGetConstructor(factoryRegistrationAttribute, type, out var constructor))
                {
                    continue;
                }

                var factoryScope = factoryRegistrationAttribute.ConstructorArguments[1] is { Kind: TypedConstantKind.Enum, Value: int factoryScopeInt }
                    ? (Scope)factoryScopeInt
                    : Scope.InstancePerResolution;

                var factoryTargetScope = factoryRegistrationAttribute.ConstructorArguments[2] is { Kind: TypedConstantKind.Enum, Value: int factoryTargetScopeInt }
                    ? (Scope)factoryTargetScopeInt
                    : Scope.InstancePerResolution;

                if (factoryScope == Scope.SingleInstance && type.TypeKind == TypeKind.Struct)
                {
                    _reportDiagnostic(StructWithSingleInstanceScope(factoryRegistrationAttribute, type, _cancellationToken));
                    continue;
                }

                var requiresInitialization = type.AllInterfaces.Contains(_iRequiresInitializationType);

                bool any = false;
                foreach (var factoryType in type.AllInterfaces.Where(x => x.OriginalDefinition.Equals(_iFactoryType, SymbolEqualityComparer.Default)))
                {
                    any = true;
                    registrations.Add(factoryType, new Registration(type, factoryType, factoryScope, requiresInitialization, constructor));

                    if (factoryType.OriginalDefinition.Equals(_iFactoryType, SymbolEqualityComparer.Default))
                    {
                        var factoryOf = factoryType.TypeArguments.First();

                        if (registrations.ContainsKey(factoryOf))
                        {
                            _reportDiagnostic(DuplicateRegistration(factoryRegistrationAttribute, factoryOf, _cancellationToken));
                            continue;
                        }

                        if (factoryTargetScope == Scope.SingleInstance && factoryOf.TypeKind == TypeKind.Struct)
                        {
                            _reportDiagnostic(StructWithSingleInstanceScope(factoryRegistrationAttribute, factoryOf, _cancellationToken));
                            continue;
                        }

                        registrations.Add(factoryOf, new FactoryRegistration(factoryType, factoryOf, factoryTargetScope));
                    }
                }
                if (!any)
                {
                    _reportDiagnostic(FactoryRegistrationNotAFactory(
                        type,
                        factoryRegistrationAttribute.GetLocation(_cancellationToken)));
                }
            }
        }

        private bool CheckValidType(AttributeData registrationAttribute, TypedConstant typedConstant, out INamedTypeSymbol type)
        {
            type = (typedConstant.Value as INamedTypeSymbol)!;
            if (typedConstant.Value is null)
            {
                _reportDiagnostic(InvalidType(
                    (ITypeSymbol)typedConstant.Value!,
                    registrationAttribute.GetLocation(_cancellationToken)));
                return false;
            }
            if (type.IsOrReferencesErrorType())
            {
                // we will report an error for this case anyway.
                return false;
            }
            if (type.IsUnboundGenericType)
            {
                _reportDiagnostic(UnboundGenericType(
                    (ITypeSymbol)typedConstant.Value!,
                    registrationAttribute.GetLocation(_cancellationToken)));
                return false;
            }
            if (!type.IsPublic())
            {
                _reportDiagnostic(TypeNotPublic(
                    type!,
                    registrationAttribute.GetLocation(_cancellationToken)));
                return false;
            }
            return true;
        }

        private bool TryGetConstructor(AttributeData registrationAttribute, INamedTypeSymbol type, out IMethodSymbol constructor)
        {
            constructor = default!;
            var applicableConstructors = type.Constructors.Where(x => x.DeclaredAccessibility == Accessibility.Public).ToList();
            if (applicableConstructors.Count == 0)
            {
                _reportDiagnostic(NoConstructor(registrationAttribute, type, _cancellationToken));
                return false;
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
                    _reportDiagnostic(MultipleConstructors(registrationAttribute, type, _cancellationToken));
                    return false;
                }
            }
            return true;
        }

        private static Diagnostic DoesNotHaveSuitableConversion(AttributeData registrationAttribute, INamedTypeSymbol registeredType, INamedTypeSymbol registeredAsType, CancellationToken cancellationToken)
        {
            return Diagnostic.Create(
                new DiagnosticDescriptor(
                    "SI0001",
                    "Registered type does not have an identity, implicit reference, boxing or nullable conversion to registered as type",
                    "'{0}' does not have an identity, implicit reference, boxing or nullable conversion to '{1}'.",
                    "StrongInject",
                    DiagnosticSeverity.Error,
                    isEnabledByDefault: true),
                registrationAttribute.ApplicationSyntaxReference?.GetSyntax(cancellationToken).GetLocation() ?? Location.None,
                registeredType,
                registeredAsType);
        }

        private static Diagnostic RegisteredByMultipleModules(AttributeData attributeForLocation, INamedTypeSymbol moduleType, AttributeData otherModuleRegistrationAttribute, ITypeSymbol type, CancellationToken cancellationToken)
        {
            return Diagnostic.Create(
                new DiagnosticDescriptor(
                    "SI0002",
                    "Modules provide differing registrations for Type",
                    "Modules '{0}' and '{1}' provide differing registrations for '{2}'.",
                    "StrongInject",
                    DiagnosticSeverity.Error,
                    isEnabledByDefault: true),
                attributeForLocation.ApplicationSyntaxReference?.GetSyntax(cancellationToken).GetLocation() ?? Location.None,
                otherModuleRegistrationAttribute.ConstructorArguments[0].Value,
                moduleType,
                type);
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

        private static Diagnostic NoConstructor(AttributeData registrationAttribute, INamedTypeSymbol type, CancellationToken cancellationToken)
        {
            return Diagnostic.Create(
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

        private static Diagnostic StructWithSingleInstanceScope(AttributeData registrationAttribute, ITypeSymbol type, CancellationToken cancellationToken)
        {
            return Diagnostic.Create(
                new DiagnosticDescriptor(
                    "SI0008",
                    "Struct cannot have Single Instance scope",
                    "'{0}' is a struct and cannot have a Single Instance scope.",
                    "StrongInject",
                    DiagnosticSeverity.Error,
                    isEnabledByDefault: true),
                registrationAttribute.ApplicationSyntaxReference?.GetSyntax(cancellationToken).GetLocation() ?? Location.None,
                type);
        }

        private static Diagnostic RecursiveModuleRegistration(INamedTypeSymbol module, CancellationToken cancellationToken)
        {
            return Diagnostic.Create(
                new DiagnosticDescriptor(
                    "SI0009",
                    "Registration for Module is recursive",
                    "Registration for '{0}' is recursive.",
                    "StrongInject",
                    DiagnosticSeverity.Error,
                    isEnabledByDefault: true),
                (module.DeclaringSyntaxReferences.FirstOrDefault()?.GetSyntax(cancellationToken) as ClassDeclarationSyntax)?.Identifier.GetLocation() ?? Location.None,
                module);
        }

        private static Diagnostic TypeIsAbstract(ITypeSymbol typeSymbol, Location location)
        {
            return Diagnostic.Create(
                new DiagnosticDescriptor(
                    "SI0010",
                    "Cannot register Type as it is abstract",
                    "Cannot register '{0}' as it is abstract.",
                    "StrongInject",
                    DiagnosticSeverity.Error,
                    isEnabledByDefault: true),
                location,
                typeSymbol);
        }

        private static Diagnostic UnboundGenericType(ITypeSymbol typeSymbol, Location location)
        {
            return Diagnostic.Create(
                new DiagnosticDescriptor(
                    "SI0011",
                    "Unbound Generic Type is invalid in a registration",
                    "Unbound Generic Type '{0}' is invalid in a registration.",
                    "StrongInject",
                    DiagnosticSeverity.Error,
                    isEnabledByDefault: true),
                location,
                typeSymbol);
        }

        private static Diagnostic FactoryRegistrationNotAFactory(ITypeSymbol typeSymbol, Location location)
        {
            return Diagnostic.Create(
                new DiagnosticDescriptor(
                    "SI0012",
                    "Type is registered as a factory but does not implement StrongInject.IFactory<T>",
                    "'{0}' is registered as a factory but does not implement StrongInject.IFactory<T>",
                    "StrongInject",
                    DiagnosticSeverity.Error,
                    isEnabledByDefault: true),
                location,
                typeSymbol);
        }
    }
}
