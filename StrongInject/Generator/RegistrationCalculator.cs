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
            _moduleRegistrationAttributeType = compilation.GetTypeOrReport(typeof(ModuleRegistrationAttribute), _reportDiagnostic)!;
            _iFactoryType = compilation.GetTypeOrReport(typeof(IFactory<>), _reportDiagnostic)!;
            _iRequiresInitializationType = compilation.GetTypeOrReport(typeof(IRequiresInitialization), _reportDiagnostic)!;
            if (_registrationAttributeType is null || _moduleRegistrationAttributeType is null || _iFactoryType is null || _iRequiresInitializationType is null)
            {
                _valid = false;
            }
            else
            {
                _valid = true;
            }
        }

        private Dictionary<INamedTypeSymbol, Dictionary<ITypeSymbol, InstanceSource>> _registrations = new();
        private INamedTypeSymbol _registrationAttributeType;
        private INamedTypeSymbol _moduleRegistrationAttributeType;
        private INamedTypeSymbol _iFactoryType;
        private INamedTypeSymbol _iRequiresInitializationType;
        private readonly Compilation _compilation;
        private readonly Action<Diagnostic> _reportDiagnostic;
        private readonly CancellationToken _cancellationToken;
        private bool _valid;

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

            var directRegistrations = CalculateDirectRegistrations(attributes);

            var moduleRegistrations = new List<(AttributeData, Dictionary<ITypeSymbol, InstanceSource> registrations)>();
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

                var exclusionListConstants = moduleRegistrationAttribute.ConstructorArguments.FirstOrDefault(x => x.Kind == TypedConstantKind.Array).Values;
                var exclusionList = exclusionListConstants.IsDefault
                    ? new HashSet<INamedTypeSymbol>()
                    : exclusionListConstants.Select(x => x.Value).OfType<INamedTypeSymbol>().ToHashSet<INamedTypeSymbol>(SymbolEqualityComparer.Default);

                var registrations = GetRegistrations(moduleType, dependantModules);

                var thisModuleRegistrations = new Dictionary<ITypeSymbol, InstanceSource>();
                foreach (var (type, registration) in registrations)
                {
                    if (exclusionList.Contains(type))
                        continue;
                    if (directRegistrations.ContainsKey(type))
                        continue;
                    var use = true;
                    foreach (var (otherModuleRegistrationAttribute, otherModuleRegistrations) in moduleRegistrations)
                    {
                        if (otherModuleRegistrations.TryGetValue(type, out var otherRegistration))
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

                    thisModuleRegistrations.Add(type, registration);
                }
                moduleRegistrations.Add((moduleRegistrationAttribute, thisModuleRegistrations));
            }

            return directRegistrations.Concat(moduleRegistrations.SelectMany(x => x.registrations)).ToDictionary(x => x.Key, x => x.Value);
        }

        private Dictionary<ITypeSymbol, InstanceSource> CalculateDirectRegistrations(ImmutableArray<AttributeData> attributes)
        {
            var directRegistrations = new Dictionary<ITypeSymbol, InstanceSource>();
            foreach (var registrationAttribute in attributes.Where(x => x.AttributeClass?.Equals(_registrationAttributeType, SymbolEqualityComparer.Default) ?? false))
            {
                _cancellationToken.ThrowIfCancellationRequested();
                var countConstructorArguments = registrationAttribute.ConstructorArguments.Length;
                if (countConstructorArguments is not (2 or 3 or 4))
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
                    _reportDiagnostic(InvalidType(
                        (ITypeSymbol)typeConstant.Value!,
                        registrationAttribute.ApplicationSyntaxReference?.GetSyntax(_cancellationToken).GetLocation() ?? Location.None));
                    continue;
                }
                if (!type.IsPublic())
                {
                    _reportDiagnostic(TypeNotPublic(
                        type!,
                        registrationAttribute.ApplicationSyntaxReference?.GetSyntax(_cancellationToken).GetLocation() ?? Location.None));
                    continue;
                }
                if (type.IsAbstract)
                {
                    _reportDiagnostic(TypeIsAbstract(
                        type!,
                        registrationAttribute.ApplicationSyntaxReference?.GetSyntax(_cancellationToken).GetLocation() ?? Location.None));
                    continue;
                }

                IMethodSymbol constructor;
                var applicableConstructors = type.Constructors.Where(x => x.DeclaredAccessibility == Accessibility.Public).ToList();
                if (applicableConstructors.Count == 0)
                {
                    _reportDiagnostic(NoConstructor(registrationAttribute, type, _cancellationToken));
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
                        _reportDiagnostic(MultipleConstructors(registrationAttribute, type, _cancellationToken));
                        continue;
                    }
                }

                var scope = countConstructorArguments is 3 or 4 && registrationAttribute.ConstructorArguments[1] is { Kind: TypedConstantKind.Enum, Value: int scopeInt }
                    ? (Scope)scopeInt
                    : Scope.InstancePerResolution;

                var factoryTargetScope = countConstructorArguments is 4 && registrationAttribute.ConstructorArguments[2] is { Kind: TypedConstantKind.Enum, Value: int factoryTargetScopeInt }
                    ? (Scope)factoryTargetScopeInt
                    : Scope.InstancePerResolution;

                var registeredAsConstants = registrationAttribute.ConstructorArguments[countConstructorArguments - 1].Values;
                var registeredAs = registeredAsConstants.IsDefaultOrEmpty ? new[] { type } : registeredAsConstants.Select(x => x.Value).OfType<INamedTypeSymbol>().ToArray();

                var requiresInitialization = type.AllInterfaces.Contains(_iRequiresInitializationType);
                foreach (var directTarget in registeredAs)
                {
                    if (directTarget.ReferencesTypeParametersOrErrorTypes())
                    {
                        _reportDiagnostic(InvalidType(
                            directTarget,
                            registrationAttribute.ApplicationSyntaxReference?.GetSyntax(_cancellationToken).GetLocation() ?? Location.None));
                        continue;
                    }
                    else if (!directTarget.IsPublic())
                    {
                        _reportDiagnostic(TypeNotPublic(
                            (ITypeSymbol)typeConstant.Value!,
                            registrationAttribute.ApplicationSyntaxReference?.GetSyntax(_cancellationToken).GetLocation() ?? Location.None));
                        continue;
                    }

                    if (_compilation.ClassifyCommonConversion(type, directTarget) is not { IsImplicit: true, IsNumeric: false, IsUserDefined: false })
                    {
                        _reportDiagnostic(DoesNotHaveSuitableConversion(registrationAttribute, type, directTarget, _cancellationToken));
                        continue;
                    }

                    if (directRegistrations.ContainsKey(directTarget))
                    {
                        _reportDiagnostic(DuplicateRegistration(registrationAttribute, directTarget, _cancellationToken));
                        continue;
                    }

                    if (scope == Scope.SingleInstance && type.TypeKind == TypeKind.Struct)
                    {
                        _reportDiagnostic(StructWithSingleInstanceScope(registrationAttribute, type, _cancellationToken));
                        continue;
                    }

                    directRegistrations.Add(directTarget, new Registration(type, directTarget, scope, requiresInitialization, constructor));

                    if (directTarget.OriginalDefinition.Equals(_iFactoryType, SymbolEqualityComparer.Default))
                    {
                        var factoryOf = directTarget.TypeArguments.First();

                        if (directRegistrations.ContainsKey(factoryOf))
                        {
                            _reportDiagnostic(DuplicateRegistration(registrationAttribute, factoryOf, _cancellationToken));
                            continue;
                        }

                        if (factoryTargetScope == Scope.SingleInstance && factoryOf.TypeKind == TypeKind.Struct)
                        {
                            _reportDiagnostic(StructWithSingleInstanceScope(registrationAttribute, factoryOf, _cancellationToken));
                            continue;
                        }

                        directRegistrations.Add(factoryOf, new FactoryRegistration(directTarget, factoryOf, factoryTargetScope));
                    }
                }
            }
            return directRegistrations;
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
    }
}
