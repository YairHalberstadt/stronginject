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
            WellKnownTypes wellKnownTypes,
            Action<Diagnostic> reportDiagnostic,
            CancellationToken cancellationToken)
        {
            _compilation = compilation;
            _wellKnownTypes = wellKnownTypes;
            _reportDiagnostic = reportDiagnostic;
            _cancellationToken = cancellationToken;
        }

        private readonly Dictionary<INamedTypeSymbol, Dictionary<ITypeSymbol, InstanceSource>> _registrations = new();
        private readonly Compilation _compilation;
        private readonly WellKnownTypes _wellKnownTypes;
        private readonly Action<Diagnostic> _reportDiagnostic;
        private readonly CancellationToken _cancellationToken;

        public IReadOnlyDictionary<ITypeSymbol, InstanceSource> GetModuleRegistrations(INamedTypeSymbol module)
        {
            var registrations = GetRegistrations(module, dependantModules: null);
            WarnOnNonStaticPublicMethodFactories(module);
            return registrations;
        }

        public IReadOnlyDictionary<ITypeSymbol, InstanceSource> GetContainerRegistrations(INamedTypeSymbol module)
        {
            var registrations = GetRegistrations(module, dependantModules: null).ToDictionary(x => x.Key, x => x.Value);
            AddContainerSpecificSources(module, registrations);
            return registrations;
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

            var thisModuleRegistrations = CalculateThisModuleRegistrations(module, attributes);

            var importedModulesRegistrations = new List<(AttributeData moduleRegistrationAttribute, Dictionary<ITypeSymbol, InstanceSource> registrations)>();
            foreach (var moduleRegistrationAttribute in attributes.Where(x => x.AttributeClass?.Equals(_wellKnownTypes.moduleRegistrationAttribute, SymbolEqualityComparer.Default) ?? false))
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

        private Dictionary<ITypeSymbol, InstanceSource> CalculateThisModuleRegistrations(INamedTypeSymbol module, ImmutableArray<AttributeData> moduleAttributes)
        {
            var registrations = new Dictionary<ITypeSymbol, InstanceSource>();
            AppendSimpleRegistrations(registrations, moduleAttributes);
            AppendFactoryRegistrations(registrations, moduleAttributes);
            AppendMethodFactories(registrations, module);
            return registrations;
        }

        private void AppendSimpleRegistrations(Dictionary<ITypeSymbol, InstanceSource> registrations, ImmutableArray<AttributeData> moduleAttributes)
        {
            foreach (var registrationAttribute in moduleAttributes.Where(x => x.AttributeClass?.Equals(_wellKnownTypes.registrationAttribute, SymbolEqualityComparer.Default) ?? false))
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

                var requiresInitialization = type.AllInterfaces.Contains(_wellKnownTypes.iRequiresInitialization);
                var requiresAsyncInitialization = type.AllInterfaces.Contains(_wellKnownTypes.iRequiresAsyncInitialization);

                if (requiresInitialization && requiresAsyncInitialization)
                {
                    _reportDiagnostic(TypeImplementsSyncAndAsyncRequiresInitialization(type, registrationAttribute.GetLocation(_cancellationToken)));
                }

                ReportSuspiciousSimpleRegistrations(type, registrationAttribute);

                foreach (var registeredAsTypeConstant in registeredAs)
                {
                    if (!CheckValidType(registrationAttribute, registeredAsTypeConstant, out var target))
                    {
                        continue;
                    }

                    if (_compilation.ClassifyCommonConversion(type, target) is not { IsImplicit: true, IsNumeric: false, IsUserDefined: false })
                    {
                        _reportDiagnostic(DoesNotHaveSuitableConversion(registrationAttribute, type, target, _cancellationToken));
                        continue;
                    }

                    if (registrations.ContainsKey(target))
                    {
                        _reportDiagnostic(DuplicateRegistration(registrationAttribute, target, _cancellationToken));
                        continue;
                    }

                    registrations.Add(target, new Registration(
                        type,
                        target,
                        scope,
                        requiresInitialization || requiresAsyncInitialization,
                        constructor,
                        isAsync: requiresAsyncInitialization));

                }
            }
        }

        private void ReportSuspiciousSimpleRegistrations(INamedTypeSymbol type, AttributeData registrationAttribute)
        {
            if (type.AllInterfaces.FirstOrDefault(x 
                => x.OriginalDefinition.Equals(_wellKnownTypes.iFactory, SymbolEqualityComparer.Default)
                || x.OriginalDefinition.Equals(_wellKnownTypes.iAsyncFactory, SymbolEqualityComparer.Default)) is { } factoryType)
            {
                _reportDiagnostic(WarnSimpleRegistrationImplementingFactory(type, factoryType, registrationAttribute.GetLocation(_cancellationToken)));
            }
        }

        private void AppendFactoryRegistrations(Dictionary<ITypeSymbol, InstanceSource> registrations, ImmutableArray<AttributeData> moduleAttributes)
        {
            foreach (var factoryRegistrationAttribute in moduleAttributes.Where(x => x.AttributeClass?.Equals(_wellKnownTypes.factoryRegistrationAttribute, SymbolEqualityComparer.Default) ?? false))
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

                var requiresInitialization = type.AllInterfaces.Contains(_wellKnownTypes.iRequiresInitialization);
                var requiresAsyncInitialization = type.AllInterfaces.Contains(_wellKnownTypes.iRequiresAsyncInitialization);

                if (requiresInitialization && requiresAsyncInitialization)
                {
                    _reportDiagnostic(TypeImplementsSyncAndAsyncRequiresInitialization(type, factoryRegistrationAttribute.GetLocation(_cancellationToken)));
                }

                bool any = false;
                foreach (var factoryType in type.AllInterfaces.Where(x
                    => x.OriginalDefinition.Equals(_wellKnownTypes.iFactory, SymbolEqualityComparer.Default)
                    || x.OriginalDefinition.Equals(_wellKnownTypes.iAsyncFactory, SymbolEqualityComparer.Default)))
                {
                    any = true;

                    if (registrations.ContainsKey(factoryType))
                    {
                        _reportDiagnostic(DuplicateRegistration(factoryRegistrationAttribute, factoryType, _cancellationToken));
                        continue;
                    }

                    registrations.Add(factoryType, new Registration(
                        type,
                        factoryType,
                        factoryScope,
                        requiresInitialization || requiresAsyncInitialization,
                        constructor,
                        isAsync: requiresAsyncInitialization));

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

                    bool isAsync = factoryType.OriginalDefinition.Equals(_wellKnownTypes.iAsyncFactory, SymbolEqualityComparer.Default);

                    registrations.Add(factoryOf, new FactoryRegistration(factoryType, factoryOf, factoryTargetScope, isAsync));
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

            foreach (var param in constructor.Parameters)
            {
                if (param.RefKind != RefKind.None)
                {
                    _reportDiagnostic(ConstructorParameterIsPassedByRef(
                        constructor,
                        param,
                        registrationAttribute.ApplicationSyntaxReference?.GetSyntax(_cancellationToken).GetLocation() ?? Location.None));
                    return false;
                }
            }
            return true;
        }

        private void AppendMethodFactories(Dictionary<ITypeSymbol, InstanceSource> registrations, INamedTypeSymbol module)
        {
            foreach (var method in module.GetMembers().OfType<IMethodSymbol>().Where(x => x.IsStatic && x.IsPublic()))
            {
                var instanceSource = CreateInstanceSourceIfFactoryMethod(method, out var attribute);
                if (instanceSource is not null)
                {
                    if (registrations.ContainsKey(instanceSource.returnType))
                    {
                        _reportDiagnostic(DuplicateRegistration(attribute, instanceSource.returnType, _cancellationToken));
                        continue;
                    }
                    registrations.Add(instanceSource.returnType, instanceSource);
                }
            }
        }

        private FactoryMethod? CreateInstanceSourceIfFactoryMethod(IMethodSymbol method, out AttributeData attribute)
        {
            attribute = method.GetAttributes().FirstOrDefault(x
                => x.AttributeClass is { } attribute
                && attribute.Equals(_wellKnownTypes.factoryAttribute, SymbolEqualityComparer.Default))!;
            if (attribute is not null)
            {
                var countConstructorArguments = attribute.ConstructorArguments.Length;
                if (countConstructorArguments != 1)
                {
                    // Invalid code, ignore;
                    return null;
                }

                var scope = attribute.ConstructorArguments[0] is { Kind: TypedConstantKind.Enum, Value: int scopeInt }
                    ? (Scope)scopeInt
                    : Scope.InstancePerResolution;

                if (method.ReturnType is { SpecialType: not SpecialType.System_Void } returnType)
                {
                    if (method.TypeParameters.Length > 0)
                    {
                        _reportDiagnostic(FactoryMethodIsGeneric(
                            method,
                            attribute.ApplicationSyntaxReference?.GetSyntax(_cancellationToken).GetLocation() ?? Location.None));
                        return null;
                    }

                    foreach (var param in method.Parameters)
                    {
                        if (param.RefKind != RefKind.None)
                        {
                            _reportDiagnostic(FactoryMethodParameterIsPassedByRef(
                                method,
                                param,
                                param.DeclaringSyntaxReferences.FirstOrDefault()?.GetSyntax(_cancellationToken).GetLocation() ?? Location.None));
                            return null;
                        }
                    }

                    if (returnType.IsWellKnownTaskType(_wellKnownTypes, out var taskOfType))
                    {
                        return new FactoryMethod(method, taskOfType, scope, isAsync: true);
                    }
                    else
                    {
                        return new FactoryMethod(method, returnType, scope, isAsync: false);
                    }
                }
                else
                {
                    _reportDiagnostic(FactoryMethodReturnsVoid(
                        method,
                        attribute.ApplicationSyntaxReference?.GetSyntax(_cancellationToken).GetLocation() ?? Location.None));
                }
            }

            return null;
        }

        private void WarnOnNonStaticPublicMethodFactories(INamedTypeSymbol module)
        {
            foreach (var method in module.GetMembers().OfType<IMethodSymbol>().Where(x => !(x.IsStatic && x.IsPublic())))
            {
                var attribute = method.GetAttributes().FirstOrDefault(x
                    => x.AttributeClass is { } attribute
                    && attribute.Equals(_wellKnownTypes.factoryAttribute, SymbolEqualityComparer.Default));
                if (attribute is not null)
                {
                    var location = attribute.ApplicationSyntaxReference?.GetSyntax(_cancellationToken).GetLocation() ?? Location.None;
                    if (!method.IsStatic)
                    {
                        _reportDiagnostic(WarnFactoryMethodNotStatic(module, method, location));
                    }
                    else
                    {
                        _reportDiagnostic(WarnFactoryMethodNotPubliclyAccessible(module, method, location));
                    }
                }
            }
        }

        private void AddContainerSpecificSources(INamedTypeSymbol module, Dictionary<ITypeSymbol, InstanceSource> registrations)
        {
            var containerSpecificRegistrations = new Dictionary<ITypeSymbol, (ISymbol symbol, Location location)>();

            AddMethodFactories();

            AddInstanceProviderFields();

            void AddMethodFactories()
            {
                foreach (var method in module.GetMembers().OfType<IMethodSymbol>().Where(x => !(x.IsStatic && x.IsPublic())))
                {
                    var instanceSource = CreateInstanceSourceIfFactoryMethod(method, out var attribute);
                    if (instanceSource is not null)
                    {
                        var location = attribute.ApplicationSyntaxReference?.GetSyntax(_cancellationToken).GetLocation() ?? Location.None;
                        if (containerSpecificRegistrations.TryGetValue(instanceSource.returnType, out var existing))
                        {
                            _reportDiagnostic(DuplicateMethodFactoriesForType(
                                method,
                                (IMethodSymbol)existing.symbol,
                                instanceSource.returnType,
                                existing.location));
                            _reportDiagnostic(DuplicateMethodFactoriesForType(
                                method,
                                (IMethodSymbol)existing.symbol,
                                instanceSource.returnType,
                                location));
                        }
                        containerSpecificRegistrations[instanceSource.returnType] = (method, location);
                        registrations[instanceSource.returnType] = instanceSource;
                    }
                }
            }

            void AddInstanceProviderFields()
            {
                foreach (var field in module.GetMembers().OfType<IFieldSymbol>())
                {
                    var location = field.DeclaringSyntaxReferences.FirstOrDefault()?.GetSyntax(_cancellationToken).GetLocation() ?? Location.None;

                    foreach (var constructedInstanceProviderInterface in field.Type.AllInterfacesAndSelf().Where(x
                        => x.OriginalDefinition.Equals(_wellKnownTypes.iInstanceProvider, SymbolEqualityComparer.Default)
                        || x.OriginalDefinition.Equals(_wellKnownTypes.iAsyncInstanceProvider, SymbolEqualityComparer.Default)))
                    {
                        var providedType = constructedInstanceProviderInterface.TypeArguments[0];
                        if (containerSpecificRegistrations.TryGetValue(providedType, out var existing))
                        {
                            if (existing.symbol is IMethodSymbol existingMethod)
                            {
                                _reportDiagnostic(DuplicateFactoryMethodAndInstanceProviderForType(
                                    existingMethod,
                                    field,
                                    providedType,
                                    existing.location));
                                _reportDiagnostic(DuplicateFactoryMethodAndInstanceProviderForType(
                                    existingMethod,
                                    field,
                                    providedType,
                                    location));
                            }
                            else
                            {
                                _reportDiagnostic(DuplicateInstanceProviders(
                                    (IFieldSymbol)existing.symbol,
                                    field,
                                    providedType,
                                    existing.location));
                                _reportDiagnostic(DuplicateInstanceProviders(
                                    (IFieldSymbol)existing.symbol,
                                    field,
                                    providedType,
                                    location));
                            }
                        }
                        var isAsync = constructedInstanceProviderInterface.OriginalDefinition.Equals(_wellKnownTypes.iAsyncInstanceProvider, SymbolEqualityComparer.Default);
                        var instanceProvider = new InstanceProvider(providedType, field, constructedInstanceProviderInterface, isAsync);
                        containerSpecificRegistrations[providedType] = (field, location);
                        registrations[providedType] = instanceProvider;
                    }
                }
            }
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
                    "Type is registered as a factory but does not implement StrongInject.IAsyncFactory<T>",
                    "'{0}' is registered as a factory but does not implement StrongInject.IAsyncFactory<T>",
                    "StrongInject",
                    DiagnosticSeverity.Error,
                    isEnabledByDefault: true),
                location,
                typeSymbol);
        }

        private static Diagnostic TypeImplementsSyncAndAsyncRequiresInitialization(ITypeSymbol typeSymbol, Location location)
        {
            return Diagnostic.Create(
                new DiagnosticDescriptor(
                    "SI0013",
                    "Type implements both IRequiresInitialization and IRequiresAsyncInitialization",
                    "'{0}' implements both IRequiresInitialization and IRequiresAsyncInitialization",
                    "StrongInject",
                    DiagnosticSeverity.Error,
                    isEnabledByDefault: true),
                location,
                typeSymbol);
        }

        private static Diagnostic FactoryMethodReturnsVoid(IMethodSymbol methodSymbol, Location location)
        {
            return Diagnostic.Create(
                new DiagnosticDescriptor(
                    "SI0014",
                    "Factory Method returns void",
                    "Factory method '{0}' returns void.",
                    "StrongInject",
                    DiagnosticSeverity.Error,
                    isEnabledByDefault: true),
                location,
                methodSymbol);
        }

        private static Diagnostic DuplicateInstanceProviders(
            IFieldSymbol firstField,
            IFieldSymbol secondField,
            ITypeSymbol providedType,
            Location location)
        {
            return Diagnostic.Create(
                new DiagnosticDescriptor(
                    "SI0015",
                    "Duplicate instance providers for type",
                    "Both fields '{0}' and '{1}' are instance providers for '{2}'",
                    "StrongInject",
                    DiagnosticSeverity.Error,
                    isEnabledByDefault: true),
                location,
                firstField,
                secondField,
                providedType);
        }

        private static Diagnostic DuplicateMethodFactoriesForType(
            IMethodSymbol firstMethod,
            IMethodSymbol secondMethod,
            ITypeSymbol providedType,
            Location location)
        {
            return Diagnostic.Create(
                new DiagnosticDescriptor(
                    "SI0016",
                    "Duplicate instance providers for type",
                    "Both methods '{0}' and '{1}' are factories for '{2}'",
                    "StrongInject",
                    DiagnosticSeverity.Error,
                    isEnabledByDefault: true),
                location,
                firstMethod,
                secondMethod,
                providedType);
        }

        private static Diagnostic DuplicateFactoryMethodAndInstanceProviderForType(
            IMethodSymbol methodSymbol,
            IFieldSymbol fieldSymbol,
            ITypeSymbol providedType,
            Location location)
        {
            return Diagnostic.Create(
                new DiagnosticDescriptor(
                    "SI0017",
                    "Duplicate instance providers for type",
                    "Both factory method '{0}' and instance provider field '{1}' are sources for '{2}'",
                    "StrongInject",
                    DiagnosticSeverity.Error,
                    isEnabledByDefault: true),
                location,
                methodSymbol,
                fieldSymbol,
                providedType);
        }

        private static Diagnostic FactoryMethodParameterIsPassedByRef(IMethodSymbol method, IParameterSymbol parameter, Location location)
        {
            return Diagnostic.Create(
                new DiagnosticDescriptor(
                        "SI0018",
                        "parameter of factory method is passed as ref",
                        "parameter '{0}' of factory method '{1}' is passed as '{2}'.",
                        "StrongInject",
                        DiagnosticSeverity.Error,
                        isEnabledByDefault: true),
                    location,
                    parameter,
                    method,
                    parameter.RefKind);
        }

        private static Diagnostic ConstructorParameterIsPassedByRef(IMethodSymbol constructor, IParameterSymbol parameter, Location location)
        {
            return Diagnostic.Create(
                new DiagnosticDescriptor(
                        "SI0019",
                        "parameter of factory method is passed as ref",
                        "parameter '{0}' of constructor '{1}' is passed as '{2}'.",
                        "StrongInject",
                        DiagnosticSeverity.Error,
                        isEnabledByDefault: true),
                    location,
                    parameter,
                    constructor,
                    parameter.RefKind);
        }

        private static Diagnostic FactoryMethodIsGeneric(IMethodSymbol methodSymbol, Location location)
        {
            return Diagnostic.Create(
                new DiagnosticDescriptor(
                    "SI0014",
                    "Factory Method is generic",
                    "Factory method '{0}' is generic.",
                    "StrongInject",
                    DiagnosticSeverity.Error,
                    isEnabledByDefault: true),
                location,
                methodSymbol);
        }

        private static Diagnostic WarnSimpleRegistrationImplementingFactory(ITypeSymbol type, ITypeSymbol factoryType, Location location)
        {
            return Diagnostic.Create(
                new DiagnosticDescriptor(
                    "SI1001",
                    "Type implements FactoryType. Did you mean to use FactoryRegistration instead?",
                    "'{0}' implements '{1}'. Did you mean to use FactoryRegistration instead?",
                    "StrongInject",
                    DiagnosticSeverity.Warning,
                    isEnabledByDefault: true),
                location,
                type,
                factoryType);
        }

        private static Diagnostic WarnFactoryMethodNotStatic(ITypeSymbol module, IMethodSymbol method, Location location)
        {
            return Diagnostic.Create(
                new DiagnosticDescriptor(
                    "SI1002",
                    "Factory Method is not static, and containing module is not a container, so will be ignored",
                    "Factory method '{0}' is not static, and containing module '{1}' is not a container, so will be ignored.",
                    "StrongInject",
                    DiagnosticSeverity.Warning,
                    isEnabledByDefault: true),
                location,
                method,
                module);
        }

        private static Diagnostic WarnFactoryMethodNotPubliclyAccessible(ITypeSymbol module, IMethodSymbol method, Location location)
        {
            return Diagnostic.Create(
                new DiagnosticDescriptor(
                    "SI1003",
                    "Factory Method is not publicly accessible, and containing module is not a container, so it will be ignored",
                    "Factory method '{0}' is not publicly accessible, and containing module '{1}' is not a container, so it will be ignored.",
                    "StrongInject",
                    DiagnosticSeverity.Warning,
                    isEnabledByDefault: true),
                location,
                method,
                module);
        }
    }
}
