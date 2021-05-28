using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using StrongInject.Plugins;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using static StrongInject.Plugins.ModuleContext;

namespace StrongInject.Generator
{
    internal class RegistrationCalculator
    {
        private record RegistrationData(
            IReadOnlyDictionary<ITypeSymbol, InstanceSources> NonGenericRegistrations,
            GenericRegistrationsResolver.Builder GenericRegistrations,
            ImmutableArray<DecoratorSource> NonGenericDecorators,
            ImmutableArray<DecoratorFactoryMethod> GenericDecorators);

        public RegistrationCalculator(
            Compilation compilation,
            WellKnownTypes wellKnownTypes,
            IReadOnlyList<IRegistrationProvider> registrationProviders,
            Action<Diagnostic> reportDiagnostic,
            CancellationToken cancellationToken)
        {
            _compilation = compilation;
            _wellKnownTypes = wellKnownTypes;
            _registrationProviders = registrationProviders;
            _reportDiagnostic = reportDiagnostic;
            _cancellationToken = cancellationToken;
        }

        private readonly Dictionary<INamedTypeSymbol, RegistrationData> _registrations = new(SymbolEqualityComparer.Default);
        private readonly Compilation _compilation;
        private readonly WellKnownTypes _wellKnownTypes;
        private readonly IReadOnlyList<IRegistrationProvider> _registrationProviders;
        private readonly Action<Diagnostic> _reportDiagnostic;
        private readonly CancellationToken _cancellationToken;

        public InstanceSourcesScope GetContainerRegistrations(INamedTypeSymbol container)
        {
            var registrations = CalculateRegistrations(container, dependantModules: null, RegistrationsToInclude.All);
            return new(
                registrations.NonGenericRegistrations,
                registrations.GenericRegistrations.Build(_compilation),
                registrations.NonGenericDecorators.GroupBy(x => x.OfType).ToDictionary(x => x.Key, x => x.Distinct().ToImmutableArray()),
                new GenericDecoratorsResolver(_compilation, registrations.GenericDecorators.Distinct().ToImmutableArray()),
                _wellKnownTypes);
        }

        private RegistrationData GetBaseClassRegistrations(INamedTypeSymbol baseClass, HashSet<INamedTypeSymbol>? dependantModules)
        {
            return CalculateRegistrations(baseClass, dependantModules, RegistrationsToInclude.ExportedAndInherited);
        }

        public void ValidateModuleRegistrations(INamedTypeSymbol module)
        {
            _ = GetModuleRegistrations(module);
        }

        internal IReadOnlyDictionary<ITypeSymbol, InstanceSources> GetModuleRegistrations(INamedTypeSymbol module)
        {
            var registrations = GetRegistrations(module, dependantModules: null);
            WarnOnNonStaticPublicOrProtectedFactoryMethods(module);
            WarnOnNonStaticPublicOrProtectedInstanceFieldsOrProperties(module);
            WarnOnNonStaticPublicDecoratorFactoryMethods(module);
            return registrations.NonGenericRegistrations;
        }

        private RegistrationData GetRegistrations(INamedTypeSymbol module, HashSet<INamedTypeSymbol>? dependantModules)
        {
            if (!_registrations.TryGetValue(module, out var registrations))
            {
                if (!(dependantModules ??= new(SymbolEqualityComparer.Default)).Add(module))
                {
                    _reportDiagnostic(RecursiveModuleRegistration(module, _cancellationToken));
                    return new RegistrationData(
                        ImmutableDictionary<ITypeSymbol, InstanceSources>.Empty,
                        new(),
                        ImmutableArray<DecoratorSource>.Empty,
                        ImmutableArray<DecoratorFactoryMethod>.Empty);
                }

                registrations = CalculateRegistrations(module, dependantModules, RegistrationsToInclude.ExportedOnly);
                _registrations[module] = registrations;
            }
            return registrations;
        }

        private RegistrationData CalculateRegistrations(INamedTypeSymbol module, HashSet<INamedTypeSymbol>? dependantModules, RegistrationsToInclude registrationsToInclude)
        {
            _cancellationToken.ThrowIfCancellationRequested();

            var genericRegistrations = new GenericRegistrationsResolver.Builder();
            var nonGenericDecorators = ImmutableArray.CreateBuilder<DecoratorSource>();
            var genericDecorators = ImmutableArray.CreateBuilder<DecoratorFactoryMethod>();
            var nonGenericRegistrations = CalculateImportedModuleRegistrations(module, genericRegistrations, nonGenericDecorators, genericDecorators, dependantModules);

            var thisModuleNonGenericRegistrations = CalculateThisModuleRegistrations(module, genericRegistrations, nonGenericDecorators, genericDecorators, registrationsToInclude);

            if (nonGenericRegistrations is not null)
            {
                foreach (var (type, instanceSources) in thisModuleNonGenericRegistrations)
                {
                    nonGenericRegistrations.CreateOrUpdate(
                        type,
                        instanceSources,
                        static (_, thisModuleInstanceSources) => thisModuleInstanceSources,
                        static (_, thisModuleInstanceSources, existing) => existing.MergeWithPreferred(thisModuleInstanceSources));
                }
            }
            else 
            {
                nonGenericRegistrations = thisModuleNonGenericRegistrations;
            }

            return new(nonGenericRegistrations, genericRegistrations, nonGenericDecorators.ToImmutable(), genericDecorators.ToImmutable());
        }

        private Dictionary<ITypeSymbol, InstanceSources>? CalculateImportedModuleRegistrations(
            INamedTypeSymbol module,
            GenericRegistrationsResolver.Builder genericRegistrations,
            ImmutableArray<DecoratorSource>.Builder nonGenericDecorators,
            ImmutableArray<DecoratorFactoryMethod>.Builder genericDecorators,
            HashSet<INamedTypeSymbol>? dependantModules)
        {
            Dictionary<ITypeSymbol, InstanceSources>? importedModuleRegistrations = null;
            foreach (var registerModuleAttribute in module.GetAttributes()
                .Where(x => x.AttributeClass?.Equals(_wellKnownTypes.RegisterModuleAttribute, SymbolEqualityComparer.Default) ?? false)
                .Sort())
            {
                _cancellationToken.ThrowIfCancellationRequested();
                var moduleConstant = registerModuleAttribute.ConstructorArguments.FirstOrDefault();
                if (moduleConstant.Kind != TypedConstantKind.Type)
                {
                    // Invalid code, ignore
                    continue;
                }
                var importedModule = (INamedTypeSymbol)moduleConstant.Value!;
                if (importedModule.IsOrReferencesErrorType())
                {
                    // Invalid code, ignore
                    continue;
                }

                if (!module.HasAtMostInternalVisibility() && importedModule.HasAtMostInternalVisibility())
                {
                    _reportDiagnostic(WarnModuleNotPublic(
                        importedModule,
                        module,
                        registerModuleAttribute.GetLocation(_cancellationToken)));
                }

                var exclusionListConstant = registerModuleAttribute.ConstructorArguments.FirstOrDefault(x => x.Kind == TypedConstantKind.Array);
                if (exclusionListConstant.Kind is not TypedConstantKind.Array)
                {
                    // Invalid code, ignore
                    continue;
                }
                var exclusionListConstants = exclusionListConstant.Values;
                var exclusionList = exclusionListConstants.IsDefault
                    ? new HashSet<ITypeSymbol>(SymbolEqualityComparer.Default)
                    : exclusionListConstants.Select(x => x.Value).OfType<INamedTypeSymbol>().ToHashSet<ITypeSymbol>(SymbolEqualityComparer.Default);

                var moduleRegistrations = GetRegistrations(importedModule, dependantModules);

                AddModuleRegistrations(moduleRegistrations, exclusionList);
            }

            if (module.BaseType is { SpecialType: not SpecialType.System_Object } baseType)
            {
                AddModuleRegistrations(GetBaseClassRegistrations(baseType, dependantModules), ImmutableHashSet<ITypeSymbol>.Empty);
            }

            return importedModuleRegistrations;

            void AddModuleRegistrations(RegistrationData moduleRegistrations, ISet<ITypeSymbol> exclusionList)
            {
                importedModuleRegistrations ??= new(SymbolEqualityComparer.Default);
                foreach (var (type, instanceSources) in moduleRegistrations.NonGenericRegistrations)
                {
                    if (exclusionList.Contains(type))
                        continue;

                    importedModuleRegistrations.CreateOrUpdate(
                        type,
                        instanceSources,
                        static (_, moduleInstanceSources) => moduleInstanceSources,
                        static (_, moduleInstanceSources, existing) => existing.Merge(moduleInstanceSources));
                }

                genericRegistrations.Add(moduleRegistrations.GenericRegistrations);
                nonGenericDecorators.AddRange(moduleRegistrations.NonGenericDecorators);
                genericDecorators.AddRange(moduleRegistrations.GenericDecorators);
            }
        }

        private Dictionary<ITypeSymbol, InstanceSources> CalculateThisModuleRegistrations(
            INamedTypeSymbol module,
            GenericRegistrationsResolver.Builder genericRegistrations,
            ImmutableArray<DecoratorSource>.Builder nonGenericDecorators,
            ImmutableArray<DecoratorFactoryMethod>.Builder genericDecorators,
            RegistrationsToInclude registrationsToInclude)
        {
            var attributes = module.GetAttributes();
            var registrations = new Dictionary<ITypeSymbol, InstanceSources>(SymbolEqualityComparer.Default);
            AppendSimpleRegistrations(registrations, attributes, module);
            AppendFactoryRegistrations(registrations, attributes, module);
            AppendFactoryMethods(registrations, genericRegistrations, module, registrationsToInclude);
            AppendFactoryOfMethods(registrations, genericRegistrations, module, registrationsToInclude);
            AppendInstanceFieldAndProperties(registrations, module, registrationsToInclude);
            AppendDecoratorRegistrations(nonGenericDecorators, attributes, module);
            AppendDecoratorFactoryMethods(nonGenericDecorators, genericDecorators, module, registrationsToInclude);
            AppendRegistrationProviderRegistrations(registrations, genericRegistrations, nonGenericDecorators, genericDecorators, module, registrationsToInclude);
            return registrations;
        }

        private void AppendSimpleRegistrations(Dictionary<ITypeSymbol, InstanceSources> registrations, ImmutableArray<AttributeData> moduleAttributes, ITypeSymbol module)
        {
            foreach (var registerAttribute in moduleAttributes
                .Where(x => x.AttributeClass?.Equals(_wellKnownTypes.RegisterAttribute, SymbolEqualityComparer.Default) ?? false)
                .Sort())
            {
                _cancellationToken.ThrowIfCancellationRequested();
                var countConstructorArguments = registerAttribute.ConstructorArguments.Length;
                if (countConstructorArguments is not (2 or 3))
                {
                    // Invalid code, ignore
                    continue;
                }

                var typeConstant = registerAttribute.ConstructorArguments[0];
                if (typeConstant.Kind != TypedConstantKind.Type)
                {
                    // Invalid code, ignore
                    continue;
                }
                if (!CheckValidType(registerAttribute, typeConstant, module, out var type))
                {
                    continue;
                }
                if (type.IsAbstract)
                {
                    _reportDiagnostic(TypeIsAbstract(
                        type!,
                        registerAttribute.GetLocation(_cancellationToken)));
                    continue;
                }

                if (!TryGetConstructor(registerAttribute, type, out var constructor))
                {
                    continue;
                }

                var scope = countConstructorArguments is 3 && registerAttribute.ConstructorArguments[1] is { Kind: TypedConstantKind.Enum, Value: int scopeInt }
                    ? (Scope)scopeInt
                    : Scope.InstancePerResolution;

                if (scope == Scope.SingleInstance && type.TypeKind == TypeKind.Struct)
                {
                    _reportDiagnostic(StructWithSingleInstanceScope(registerAttribute, type, _cancellationToken));
                    continue;
                }

                var registeredAsConstant = registerAttribute.ConstructorArguments[countConstructorArguments - 1];
                if (registeredAsConstant.Kind is not TypedConstantKind.Array)
                {
                    // error case.
                    continue;
                }
                var registeredAsConstants = registeredAsConstant.Values;
                var registeredAs = registeredAsConstants.IsDefaultOrEmpty
                    ? new[] { typeConstant }
                    : registeredAsConstants.Where(x => x.Kind == TypedConstantKind.Type).ToArray();

                var requiresInitialization = type.AllInterfaces.Contains(_wellKnownTypes.IRequiresInitialization);
                var requiresAsyncInitialization = type.AllInterfaces.Contains(_wellKnownTypes.IRequiresAsyncInitialization);

                if (requiresInitialization && requiresAsyncInitialization)
                {
                    _reportDiagnostic(TypeImplementsSyncAndAsyncRequiresInitialization(type, registerAttribute.GetLocation(_cancellationToken)));
                }

                ReportSuspiciousSimpleRegistrations(type, registerAttribute);

                var registration = new Registration(
                    type,
                    scope,
                    requiresInitialization || requiresAsyncInitialization,
                    constructor,
                    IsAsync: requiresAsyncInitialization);

                foreach (var registeredAsTypeConstant in registeredAs)
                {
                    if (!CheckValidType(registerAttribute, registeredAsTypeConstant, module, out var target))
                    {
                        continue;
                    }

                    if (_compilation.ClassifyConversion(type, target) is not { IsImplicit: true, IsNumeric: false, IsUserDefined: false })
                    {
                        _reportDiagnostic(DoesNotHaveSuitableConversion(registerAttribute, type, target, _cancellationToken));
                        continue;
                    }

                    registrations.WithInstanceSource(ForwardedInstanceSource.Create(target, registration));
                }
            }
        }

        private void ReportSuspiciousSimpleRegistrations(INamedTypeSymbol type, AttributeData registerAttribute)
        {
            if (type.AllInterfaces.FirstOrDefault(x 
                => x.OriginalDefinition.Equals(_wellKnownTypes.IFactory, SymbolEqualityComparer.Default)
                || x.OriginalDefinition.Equals(_wellKnownTypes.IAsyncFactory, SymbolEqualityComparer.Default)) is { } factoryType)
            {
                _reportDiagnostic(WarnSimpleRegistrationImplementingFactory(type, factoryType, registerAttribute.GetLocation(_cancellationToken)));
            }
        }

        private void AppendFactoryRegistrations(Dictionary<ITypeSymbol, InstanceSources> registrations, ImmutableArray<AttributeData> moduleAttributes, ITypeSymbol module)
        {
            foreach (var registerFactoryAttribute in moduleAttributes
                .Where(x => x.AttributeClass?.Equals(_wellKnownTypes.RegisterFactoryAttribute, SymbolEqualityComparer.Default) ?? false)
                .Sort())
            {
                _cancellationToken.ThrowIfCancellationRequested();
                var countConstructorArguments = registerFactoryAttribute.ConstructorArguments.Length;
                if (countConstructorArguments != 3)
                {
                    // Invalid code, ignore
                    continue;
                }

                var typeConstant = registerFactoryAttribute.ConstructorArguments[0];
                if (typeConstant.Kind != TypedConstantKind.Type)
                {
                    // Invalid code, ignore
                    continue;
                }
                if (!CheckValidType(registerFactoryAttribute, typeConstant, module, out var type))
                {
                    continue;
                }
                if (type.IsAbstract)
                {
                    _reportDiagnostic(TypeIsAbstract(
                        type!,
                        registerFactoryAttribute.GetLocation(_cancellationToken)));
                    continue;
                }

                if (!TryGetConstructor(registerFactoryAttribute, type, out var constructor))
                {
                    continue;
                }

                var factoryScope = registerFactoryAttribute.ConstructorArguments[1] is { Kind: TypedConstantKind.Enum, Value: int factoryScopeInt }
                    ? (Scope)factoryScopeInt
                    : Scope.InstancePerResolution;

                var factoryTargetScope = registerFactoryAttribute.ConstructorArguments[2] is { Kind: TypedConstantKind.Enum, Value: int factoryTargetScopeInt }
                    ? (Scope)factoryTargetScopeInt
                    : Scope.InstancePerResolution;

                if (factoryScope == Scope.SingleInstance && type.TypeKind == TypeKind.Struct)
                {
                    _reportDiagnostic(StructWithSingleInstanceScope(registerFactoryAttribute, type, _cancellationToken));
                    continue;
                }

                var requiresInitialization = type.AllInterfaces.Contains(_wellKnownTypes.IRequiresInitialization);
                var requiresAsyncInitialization = type.AllInterfaces.Contains(_wellKnownTypes.IRequiresAsyncInitialization);

                if (requiresInitialization && requiresAsyncInitialization)
                {
                    _reportDiagnostic(TypeImplementsSyncAndAsyncRequiresInitialization(type, registerFactoryAttribute.GetLocation(_cancellationToken)));
                }

                var registration = new Registration(
                    type,
                    factoryScope,
                    requiresInitialization || requiresAsyncInitialization,
                    constructor,
                    IsAsync: requiresAsyncInitialization);

                bool any = false;
                foreach (var factoryType in type.AllInterfaces.Where(x
                    => x.OriginalDefinition.Equals(_wellKnownTypes.IFactory, SymbolEqualityComparer.Default)
                    || x.OriginalDefinition.Equals(_wellKnownTypes.IAsyncFactory, SymbolEqualityComparer.Default)))
                {
                    any = true;

                    var factoryOf = factoryType.TypeArguments.First();

                    if (factoryTargetScope == Scope.SingleInstance && factoryOf.TypeKind == TypeKind.Struct)
                    {
                        _reportDiagnostic(StructWithSingleInstanceScope(registerFactoryAttribute, factoryOf, _cancellationToken));
                        continue;
                    }

                    bool isAsync = factoryType.OriginalDefinition.Equals(_wellKnownTypes.IAsyncFactory, SymbolEqualityComparer.Default);

                    var factoryRegistration = new FactorySource(factoryOf, ForwardedInstanceSource.Create(factoryType, registration), factoryTargetScope, isAsync);

                    registrations.WithInstanceSource(factoryRegistration);
                }
                if (!any)
                {
                    _reportDiagnostic(FactoryRegistrationNotAFactory(
                        type,
                        registerFactoryAttribute.GetLocation(_cancellationToken)));
                }
            }
        }

        private bool CheckValidType(AttributeData registerAttribute, TypedConstant typedConstant, ITypeSymbol module, out INamedTypeSymbol type)
        {
            type = (typedConstant.Value as INamedTypeSymbol)!;
            if (typedConstant.Value is null)
            {
                _reportDiagnostic(InvalidType(
                    (ITypeSymbol)typedConstant.Value!,
                    registerAttribute.GetLocation(_cancellationToken)));
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
                    registerAttribute.GetLocation(_cancellationToken)));
                return false;
            }

            if (!type.IsAccessibleInternally())
            {
                _reportDiagnostic(TypeDoesNotHaveAtLeastInternalAccessibility(
                    type,
                    registerAttribute.GetLocation(_cancellationToken)));
                return false;
            }

            if (!(module.HasAtMostInternalVisibility() || type.IsPublic()))
            {
                _reportDiagnostic(WarnTypeNotPublic(
                    type,
                    module,
                    registerAttribute.GetLocation(_cancellationToken)));
            }

            return true;
        }

        private bool TryGetConstructor(AttributeData registerAttribute, INamedTypeSymbol type, out IMethodSymbol constructor)
        {
            constructor = default!;
            var applicableConstructors = type.InstanceConstructors.Where(x => x.DeclaredAccessibility == Accessibility.Public).ToList();
            if (applicableConstructors.Count == 0)
            {
                _reportDiagnostic(NoConstructor(registerAttribute, type, _cancellationToken));
                return false;
            }

            if (applicableConstructors.Count == 1)
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
                    _reportDiagnostic(MultipleConstructors(registerAttribute, type, _cancellationToken));
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
                        registerAttribute.ApplicationSyntaxReference?.GetSyntax(_cancellationToken).GetLocation() ?? Location.None));
                    return false;
                }
            }
            return true;
        }

        private void AppendFactoryMethods(
            Dictionary<ITypeSymbol, InstanceSources> nonGenericRegistrations,
            GenericRegistrationsResolver.Builder genericRegistrations,
            INamedTypeSymbol module,
            RegistrationsToInclude registrationsToInclude)
        {
            var methods = module.GetMembers().OfType<IMethodSymbol>();
            foreach (var method in registrationsToInclude switch {
                RegistrationsToInclude.ExportedOnly => methods.Where(x => x.IsStatic && x.DeclaredAccessibility == Accessibility.Public),
                RegistrationsToInclude.ExportedAndInherited => methods.Where(x => x.IsStatic && x.DeclaredAccessibility == Accessibility.Public || x.IsProtected()),
                RegistrationsToInclude.All => methods,
                _ => throw new InvalidEnumArgumentException(nameof(registrationsToInclude), (int)registrationsToInclude, typeof(RegistrationsToInclude))
            })
            {
                var instanceSource = CreateInstanceSourceIfFactoryMethod(method, out var attribute);
                if (instanceSource is not null)
                {
                    if (instanceSource.IsOpenGeneric)
                    {
                        genericRegistrations.Add(instanceSource);
                    }
                    else
                    {
                        nonGenericRegistrations.WithInstanceSource(instanceSource);
                    }
                }
            }
        }

        private FactoryMethod? CreateInstanceSourceIfFactoryMethod(IMethodSymbol method, out AttributeData attribute)
        {
            attribute = method.GetAttributes().FirstOrDefault(x
                => x.AttributeClass is { } attribute
                && attribute.Equals(_wellKnownTypes.FactoryAttribute, SymbolEqualityComparer.Default))!;
            if (attribute is not null)
            {
                var countConstructorArguments = attribute.ConstructorArguments.Length;
                if (countConstructorArguments != 1)
                {
                    // Invalid code, ignore
                    return null;
                }

                var scope = attribute.ConstructorArguments[0] is { Kind: TypedConstantKind.Enum, Value: int scopeInt }
                    ? (Scope)scopeInt
                    : Scope.InstancePerResolution;

                var diagnosticLocation = attribute.ApplicationSyntaxReference?.GetSyntax(_cancellationToken).GetLocation() ?? Location.None;
                
                return CreateFactoryMethod(method, scope, diagnosticLocation);
            }

            return null;
        }

        private FactoryMethod? CreateFactoryMethod(IMethodSymbol method, Scope scope, Location diagnosticLocation)
        {
            var returnType = method.ReturnType;
            if (returnType.SpecialType is SpecialType.System_Void)
            {
                _reportDiagnostic(FactoryMethodReturnsVoid(
                    method,
                    diagnosticLocation));
                return null;
            }

            bool isGeneric = method.TypeParameters.Length > 0;
            if (isGeneric && !AllTypeParametersUsedInReturnType(method))
            {
                _reportDiagnostic(NotAllTypeParametersUsedInReturnType(
                    method,
                    diagnosticLocation));

                {
                    return null;
                }
            }

            foreach (var param in method.Parameters)
            {
                if (param.RefKind != RefKind.None)
                {
                    _reportDiagnostic(FactoryMethodParameterIsPassedByRef(
                        method,
                        param,
                        param.DeclaringSyntaxReferences.FirstOrDefault()?.GetSyntax(_cancellationToken).GetLocation() ??
                        Location.None));
                    {
                        return null;
                    }
                }
            }

            if (returnType.IsWellKnownTaskType(_wellKnownTypes, out var taskOfType))
            {
                return new FactoryMethod(method, taskOfType, scope, isGeneric, IsAsync: true);
            }

            return new FactoryMethod(method, returnType, scope, isGeneric, IsAsync: false);
        }

        private static bool AllTypeParametersUsedInReturnType(IMethodSymbol method)
        {
            var usedParameters = new bool[method.TypeParameters.Length];
            Visit(method.ReturnType);
            return usedParameters.All(x => x);
            void Visit(ITypeSymbol type)
            {
                switch (type)
                {
                    case ITypeParameterSymbol typeParameterSymbol:

                        if (SymbolEqualityComparer.Default.Equals(typeParameterSymbol.DeclaringMethod, method))
                        {
                            usedParameters[typeParameterSymbol.Ordinal] = true;
                        }
                        break;

                    case IArrayTypeSymbol { ElementType: var elementType }:

                        Visit(elementType);
                        break;

                    case INamedTypeSymbol { TypeArguments: var typeArguments }:

                        foreach (var typeArgument in typeArguments)
                        {
                            Visit(typeArgument);
                        }

                        break;
                    case IFunctionPointerTypeSymbol { Signature: { ReturnType: var returnType, Parameters: var parameters } }:
                        Visit(returnType);
                        foreach (var parameter in parameters)
                        {
                            Visit(parameter.Type);
                        }
                        break;
                    case IPointerTypeSymbol { PointedAtType: var pointedAtType }:
                        Visit(pointedAtType);
                        break;
                    case IDynamicTypeSymbol:
                        break;
                    default: throw new NotImplementedException(type.ToString());
                }
            }
        }

        private void AppendFactoryOfMethods(
            Dictionary<ITypeSymbol, InstanceSources> nonGenericRegistrations,
            GenericRegistrationsResolver.Builder genericRegistrations,
            INamedTypeSymbol module,
            RegistrationsToInclude registrationsToInclude)
        {
            var methods = module.GetMembers().OfType<IMethodSymbol>();
            foreach (var method in registrationsToInclude switch
            {
                RegistrationsToInclude.ExportedOnly => methods.Where(x => x.IsStatic && x.DeclaredAccessibility == Accessibility.Public),
                RegistrationsToInclude.ExportedAndInherited => methods.Where(x => x.IsStatic && x.DeclaredAccessibility == Accessibility.Public || x.IsProtected()),
                RegistrationsToInclude.All => methods,
                _ => throw new InvalidEnumArgumentException(nameof(registrationsToInclude), (int)registrationsToInclude, typeof(RegistrationsToInclude))
            })
            {
                var instanceSources = CreateInstanceSourcesIfFactoryOfMethod(method);
                foreach (var factoryOfMethod in instanceSources)
                {
                    if (factoryOfMethod.Underlying.IsOpenGeneric)
                    {
                        genericRegistrations.Add(factoryOfMethod);
                    }
                    else
                    {
                        nonGenericRegistrations.WithInstanceSource(factoryOfMethod.Underlying);
                    }
                }
            }
        }

        private IEnumerable<FactoryOfMethod> CreateInstanceSourcesIfFactoryOfMethod(IMethodSymbol method)
        {
            var attributes = method.GetAttributes().Where(x
                => x.AttributeClass is { } attribute
                && attribute.Equals(_wellKnownTypes.FactoryOfAttribute, SymbolEqualityComparer.Default))!;

            foreach (var attribute in attributes)
            {
                var countConstructorArguments = attribute.ConstructorArguments.Length;
                if (countConstructorArguments != 2)
                {
                    // Invalid code, ignore
                    continue;
                }

                var typeConstant = attribute.ConstructorArguments[0];
                if (typeConstant.Kind != TypedConstantKind.Type)
                {
                    // Invalid code, ignore
                    continue;
                }

                var type = (ITypeSymbol)typeConstant.Value!;

                var scope = attribute.ConstructorArguments[1] is { Kind: TypedConstantKind.Enum, Value: int scopeInt }
                    ? (Scope)scopeInt
                    : Scope.InstancePerResolution;

                if (method.ReturnType is { SpecialType: not SpecialType.System_Void } returnType)
                {
                    bool isGeneric = method.TypeParameters.Length > 0;

                    if (!isGeneric)
                    {
                        _reportDiagnostic(FactoryOfMethodMustBeGeneric(
                            method,
                            attribute.ApplicationSyntaxReference?.GetSyntax(_cancellationToken).GetLocation() ?? Location.None));

                        continue;
                    }

                    if (!AllTypeParametersUsedInReturnType(method))
                    {
                        _reportDiagnostic(NotAllTypeParametersUsedInReturnType(
                            method,
                            attribute.ApplicationSyntaxReference?.GetSyntax(_cancellationToken).GetLocation() ?? Location.None));

                        continue;
                    }

                    bool anyPassedByRef = false;
                    foreach (var param in method.Parameters)
                    {
                        if (param.RefKind != RefKind.None)
                        {
                            _reportDiagnostic(FactoryMethodParameterIsPassedByRef(
                                method,
                                param,
                                param.DeclaringSyntaxReferences.FirstOrDefault()?.GetSyntax(_cancellationToken).GetLocation() ?? Location.None));
                            anyPassedByRef = true;
                        }
                    }
                    if (anyPassedByRef)
                        continue;

                    var underlyingFactoryMethod = returnType.IsWellKnownTaskType(_wellKnownTypes, out var taskOfType)
                        ? new FactoryMethod(method, taskOfType, scope, IsOpenGeneric: true, IsAsync: true)
                        : new FactoryMethod(method, returnType, scope, IsOpenGeneric: true, IsAsync: false);

                    if (type is INamedTypeSymbol { IsUnboundGenericType: true })
                    {
                        if (underlyingFactoryMethod.FactoryOfType is not ITypeParameterSymbol)
                        {
                            _reportDiagnostic(FactoryOfOpenGenericMustReturnSingleTypeParamater(
                                method,
                                type,
                                attribute.ApplicationSyntaxReference?.GetSyntax(_cancellationToken).GetLocation() ?? Location.None));
                            continue;
                        }

                        yield return new FactoryOfMethod(underlyingFactoryMethod, type);
                    }
                    else
                    {
                        if (!GenericResolutionHelpers.CanConstructFromGenericMethodReturnType(
                            _compilation,
                            type,
                            underlyingFactoryMethod.FactoryOfType,
                            method,
                            out var constructedFactoryMethod,
                            out var constraintsDoNotMatch))
                        {
                            if (constraintsDoNotMatch)
                            {
                                _reportDiagnostic(CannotConstructFactoryOfTypeFromMethodAsConstraintsDoNotMatch(
                                    method,
                                    type,
                                    attribute.ApplicationSyntaxReference?.GetSyntax(_cancellationToken).GetLocation() ?? Location.None));
                            }
                            else
                            {
                                _reportDiagnostic(CannotConstructFactoryOfTypeFromMethod(
                                    method,
                                    type,
                                    attribute.ApplicationSyntaxReference?.GetSyntax(_cancellationToken).GetLocation() ?? Location.None));
                            }
                            continue;
                        }
                        yield return new FactoryOfMethod(
                            underlyingFactoryMethod with
                            {
                                FactoryOfType = type,
                                Method = constructedFactoryMethod,
                                IsOpenGeneric = false,
                            },
                            type);
                    }
                }
                else
                {
                    _reportDiagnostic(FactoryMethodReturnsVoid(
                        method,
                        attribute.ApplicationSyntaxReference?.GetSyntax(_cancellationToken).GetLocation() ?? Location.None));
                }
            }
        }

        private void AppendInstanceFieldAndProperties(
            Dictionary<ITypeSymbol, InstanceSources> registrations,
            INamedTypeSymbol module,
            RegistrationsToInclude registrationsToInclude)
        {
            var fieldsAndProperties = module.GetMembers().Where(x => x is IFieldSymbol or IPropertySymbol);
            foreach (var fieldOrProperty in registrationsToInclude switch
            {
                RegistrationsToInclude.ExportedOnly => fieldsAndProperties.Where(x => IsPubliclyAccessibleStaticFieldOrProperty(x)),
                RegistrationsToInclude.ExportedAndInherited => fieldsAndProperties.Where(x => IsPubliclyAccessibleStaticFieldOrProperty(x) || IsProtectedFieldOrProperty(x)),
                RegistrationsToInclude.All => fieldsAndProperties,
                _ => throw new InvalidEnumArgumentException(nameof(registrationsToInclude), (int)registrationsToInclude, typeof(RegistrationsToInclude))
            })
            {
                InstanceSource? instanceSource = CreateInstanceSourceIfInstanceFieldOrProperty(fieldOrProperty, out var attribute);
                if (instanceSource is not null)
                {
                    var constructorArguments = attribute.ConstructorArguments;
                    if (!constructorArguments.IsDefault && constructorArguments.Length == 1 && constructorArguments[0] is { Kind: TypedConstantKind.Enum, Value: long value})
                    {
                        var options = (Options)value;
                        instanceSource = ApplyOptions(instanceSource, options, registrations);
                    }

                    registrations.WithInstanceSource(instanceSource);
                }
            }
        }

        private TSource ApplyOptions<TSource>(TSource instanceSource, Options options, Dictionary<ITypeSymbol, InstanceSources> registrations, HashSet<ITypeSymbol>? currentlyVisiting = null) where TSource : InstanceSource
        {
            var useAsFactory = options.HasFlag(Options.UseAsFactory);
            if (useAsFactory && currentlyVisiting is null)
            {
                currentlyVisiting = new HashSet<ITypeSymbol>(SymbolEqualityComparer.Default) { instanceSource.OfType };
            }

            if (currentlyVisiting?.Count > 20)
            {
                /* prevent infinite recursion in case like:
                    public class A<T> : IFactory<A<A<T>>> {}
                */
                return instanceSource;
            }

            if (options.HasFlag(Options.DoNotDecorate) && instanceSource.CanDecorate)
            {
                instanceSource = instanceSource with { CanDecorate = false };
            }

            if (options.HasFlag(Options.AsBaseClasses))
            {
                foreach (var baseType in instanceSource.OfType.GetBaseTypes())
                {
                    if (baseType.SpecialType == SpecialType.System_Object)
                    {
                        continue;
                    }

                    if (currentlyVisiting?.Add(baseType) is false)
                    {
                        continue;
                    }

                    registrations.WithInstanceSource(ForwardedInstanceSource.Create(baseType, instanceSource));

                    currentlyVisiting?.Remove(baseType);
                }
            }

            if (options.HasFlag(Options.AsImplementedInterfaces))
            {
                foreach (var implementedInterface in instanceSource.OfType.AllInterfaces)
                {
                    if (currentlyVisiting?.Add(implementedInterface) is false)
                    {
                        continue;
                    }

                    var forwardedInstanceSource = ForwardedInstanceSource.Create(implementedInterface, instanceSource);
                    ApplyUseAsFactoryOption(forwardedInstanceSource);
                    registrations.WithInstanceSource(forwardedInstanceSource);

                    currentlyVisiting?.Remove(implementedInterface);
                }
            }

            ApplyUseAsFactoryOption(instanceSource);

            return instanceSource;

            void ApplyUseAsFactoryOption(InstanceSource instanceSource)
            {
                if (!useAsFactory)
                {
                    return;
                }

                var type = instanceSource.OfType;
                var isAsync = type.OriginalDefinition.Equals(_wellKnownTypes.IAsyncFactory, SymbolEqualityComparer.Default);
                var isFactory = isAsync || type.OriginalDefinition.Equals(_wellKnownTypes.IFactory, SymbolEqualityComparer.Default);
                if (!isFactory)
                {
                    return;
                }

                var scope = (Scope)(((int)options) >> 24);
                var factoryTarget = ((INamedTypeSymbol)type).TypeArguments[0];
                InstanceSource factorySource = new FactorySource(factoryTarget, instanceSource, scope, isAsync);
                if (options.HasFlag(Options.ApplySameOptionsToFactoryTargets))
                {
                    if (!currentlyVisiting!.Add(factoryTarget))
                    {
                        return;
                    }

                    factorySource = ApplyOptions(factorySource, options, registrations, currentlyVisiting);
                    currentlyVisiting.Remove(factoryTarget);
                }

                registrations.WithInstanceSource(factorySource);
            }
        }

        private static bool IsPubliclyAccessibleStaticFieldOrProperty(ISymbol symbol)
        {
            Debug.Assert(symbol is IFieldSymbol or IPropertySymbol);
            if (!symbol.IsStatic)
                return false;
            if (symbol.DeclaredAccessibility != Accessibility.Public)
                return false;
            if (symbol is IPropertySymbol { GetMethod: { DeclaredAccessibility: not Accessibility.Public } })
                return false;
            return true;
        }

        private static bool IsProtectedFieldOrProperty(ISymbol symbol)
        {
            Debug.Assert(symbol is IFieldSymbol or IPropertySymbol);
            if (symbol.IsProtected() && symbol is not IPropertySymbol { GetMethod: { DeclaredAccessibility: Accessibility.Private } })
                return true;
            return false;
        }

        private InstanceFieldOrProperty? CreateInstanceSourceIfInstanceFieldOrProperty(ISymbol fieldOrProperty, out AttributeData attribute)
        {
            attribute = fieldOrProperty.GetAttributes().FirstOrDefault(x
                => x.AttributeClass is { } attribute
                && attribute.Equals(_wellKnownTypes.InstanceAttribute, SymbolEqualityComparer.Default))!;
            if (attribute is not null)
            {
                if (fieldOrProperty is IFieldSymbol { Type: var fieldType })
                {
                    return new InstanceFieldOrProperty(fieldOrProperty, fieldType);
                }
                if (fieldOrProperty is IPropertySymbol { Type: var propertyType } property)
                {
                    if (property.GetMethod is null)
                    {
                        _reportDiagnostic(InstancePropertyIsWriteOnly(
                            property,
                            attribute.ApplicationSyntaxReference?.GetSyntax(_cancellationToken).GetLocation() ?? Location.None));
                    }
                    else
                    {
                        return new InstanceFieldOrProperty(fieldOrProperty, propertyType);
                    }
                }
                else
                {
                    throw new InvalidOperationException("This location is believed to be unreachable");
                }
            }

            return null;
        }

        private void WarnOnNonStaticPublicOrProtectedFactoryMethods(INamedTypeSymbol module)
        {
            foreach (var method in module.GetMembers().OfType<IMethodSymbol>().Where(x => !(x.IsStatic && x.DeclaredAccessibility == Accessibility.Public || x.IsProtected())))
            {
                var attribute = method.GetAttributes().FirstOrDefault(x
                    => x.AttributeClass is { } attribute
                    && attribute.Equals(_wellKnownTypes.FactoryAttribute, SymbolEqualityComparer.Default));
                if (attribute is not null)
                {
                    var location = attribute.ApplicationSyntaxReference?.GetSyntax(_cancellationToken).GetLocation() ?? Location.None;
                    _reportDiagnostic(WarnFactoryMethodNotPublicStaticOrProtected(module, method, location));
                }
            }
        }

        private void WarnOnNonStaticPublicOrProtectedInstanceFieldsOrProperties(INamedTypeSymbol module)
        {
            foreach (var fieldOrProperty in module.GetMembers().Where(x => x is IFieldSymbol or IPropertySymbol && !IsPubliclyAccessibleStaticFieldOrProperty(x) && !IsProtectedFieldOrProperty(x)))
            {
                var attribute = fieldOrProperty.GetAttributes().FirstOrDefault(x
                    => x.AttributeClass is { } attribute
                    && attribute.Equals(_wellKnownTypes.InstanceAttribute, SymbolEqualityComparer.Default));
                if (attribute is not null)
                {
                    var location = attribute.ApplicationSyntaxReference?.GetSyntax(_cancellationToken).GetLocation() ?? Location.None;
                    _reportDiagnostic(WarnInstanceFieldOrPropertyNotPublicStaticOrProtected(module, fieldOrProperty, location));
                }
            }
        }

        private void WarnOnNonStaticPublicDecoratorFactoryMethods(INamedTypeSymbol module)
        {
            foreach (var method in module.GetMembers().OfType<IMethodSymbol>().Where(x => !(x.IsStatic && x.DeclaredAccessibility == Accessibility.Public || x.IsProtected())))
            {
                var attribute = method.GetAttributes().FirstOrDefault(x
                    => x.AttributeClass is { } attribute
                    && attribute.Equals(_wellKnownTypes.DecoratorFactoryAttribute, SymbolEqualityComparer.Default));
                if (attribute is not null)
                {
                    var location = attribute.ApplicationSyntaxReference?.GetSyntax(_cancellationToken).GetLocation() ?? Location.None;
                    _reportDiagnostic(WarnFactoryMethodNotPublicStaticOrProtected(module, method, location));
                }
            }
        }

        private void AppendDecoratorRegistrations(ImmutableArray<DecoratorSource>.Builder nonGenericDecorators, ImmutableArray<AttributeData> moduleAttributes, ITypeSymbol module)
        {
            foreach (var registerDecoratorAttribute in moduleAttributes
                .Where(x => x.AttributeClass?.Equals(_wellKnownTypes.RegisterDecoratorAttribute, SymbolEqualityComparer.Default) ?? false)
                .Sort())
            {
                _cancellationToken.ThrowIfCancellationRequested();
                var countConstructorArguments = registerDecoratorAttribute.ConstructorArguments.Length;
                if (countConstructorArguments != 3)
                {
                    // Invalid code, ignore
                    continue;
                }

                var typeConstant = registerDecoratorAttribute.ConstructorArguments[0];
                if (typeConstant.Kind != TypedConstantKind.Type)
                {
                    // Invalid code, ignore
                    continue;
                }
                if (!CheckValidType(registerDecoratorAttribute, typeConstant, module, out var type))
                {
                    continue;
                }
                if (type.IsAbstract)
                {
                    _reportDiagnostic(TypeIsAbstract(
                        type!,
                        registerDecoratorAttribute.GetLocation(_cancellationToken)));
                    continue;
                }

                if (!TryGetConstructor(registerDecoratorAttribute, type, out var constructor))
                {
                    continue;
                }

                var decoratorOfConstant = registerDecoratorAttribute.ConstructorArguments[1];
                if (decoratorOfConstant.Kind != TypedConstantKind.Type)
                {
                    // Invalid code, ignore
                    continue;
                }
                if (!CheckValidType(registerDecoratorAttribute, decoratorOfConstant, module, out var decoratedType))
                {
                    continue;
                }

                var requiresInitialization = type.AllInterfaces.Contains(_wellKnownTypes.IRequiresInitialization);
                var requiresAsyncInitialization = type.AllInterfaces.Contains(_wellKnownTypes.IRequiresAsyncInitialization);

                if (requiresInitialization && requiresAsyncInitialization)
                {
                    _reportDiagnostic(TypeImplementsSyncAndAsyncRequiresInitialization(type, registerDecoratorAttribute.GetLocation(_cancellationToken)));
                }

                var conversion = _compilation.ClassifyConversion(type, decoratedType);
                if (conversion is not { IsImplicit: true, IsNumeric: false, IsUserDefined: false })
                {
                    _reportDiagnostic(DoesNotHaveSuitableConversion(registerDecoratorAttribute, type, decoratedType, _cancellationToken));
                    continue;
                }

                var decoratedParameters = constructor.Parameters.Where(x => x.Type.Equals(decoratedType, SymbolEqualityComparer.Default)).ToList();
                if (decoratedParameters.Count == 0)
                {
                    _reportDiagnostic(DecoratorDoesNotHaveParameterOfDecoratedType(registerDecoratorAttribute, type, decoratedType, _cancellationToken));
                    continue;
                }

                if (decoratedParameters.Count > 1)
                {
                    _reportDiagnostic(DecoratorHasMultipleParametersOfDecoratedType(registerDecoratorAttribute, type, decoratedType, _cancellationToken));
                    continue;
                }

                var options = DecoratorOptions.Default;
                if (registerDecoratorAttribute.ConstructorArguments[2] is { Kind: TypedConstantKind.Enum, Value: long value })
                {
                    options = (DecoratorOptions)value;
                }

                var registration = new DecoratorRegistration(
                    type,
                    decoratedType,
                    requiresInitialization || requiresAsyncInitialization,
                    constructor,
                    decoratedParameters[0].Ordinal,
                    options.HasFlag(DecoratorOptions.Dispose),
                    IsAsync: requiresAsyncInitialization);

                nonGenericDecorators.Add(registration);
            }
        }

        private void AppendDecoratorFactoryMethods(
            ImmutableArray<DecoratorSource>.Builder nonGenericDecorators,
            ImmutableArray<DecoratorFactoryMethod>.Builder genericDecorators,
            INamedTypeSymbol module,
            RegistrationsToInclude registrationsToInclude)
        {
            var methods = module.GetMembers().OfType<IMethodSymbol>();
            foreach (var method in registrationsToInclude switch {
                RegistrationsToInclude.ExportedOnly => methods.Where(x => x.IsStatic && x.DeclaredAccessibility == Accessibility.Public),
                RegistrationsToInclude.ExportedAndInherited => methods.Where(x => x.IsStatic && x.DeclaredAccessibility == Accessibility.Public || x.IsProtected()),
                RegistrationsToInclude.All => methods,
                _ => throw new InvalidEnumArgumentException(nameof(registrationsToInclude), (int)registrationsToInclude, typeof(RegistrationsToInclude))
            })
            {
                var decoratorSource = CreateDecoratorSourceIfDecoratorFactoryMethod(method, out var attribute);
                if (decoratorSource is not null)
                {
                    if (decoratorSource.IsOpenGeneric)
                    {
                        genericDecorators.Add(decoratorSource);
                    }
                    else
                    {
                        nonGenericDecorators.Add(decoratorSource);
                    }
                }
            }
        }

        private DecoratorFactoryMethod? CreateDecoratorSourceIfDecoratorFactoryMethod(IMethodSymbol method, out AttributeData attribute)
        {
            attribute = method.GetAttributes().FirstOrDefault(x
                => x.AttributeClass is { } attribute
                && attribute.Equals(_wellKnownTypes.DecoratorFactoryAttribute, SymbolEqualityComparer.Default))!;
            if (attribute is not null)
            {
                if (method.ReturnType is { SpecialType: not SpecialType.System_Void } returnType)
                {
                    bool isGeneric = method.TypeParameters.Length > 0;
                    if (isGeneric && !AllTypeParametersUsedInReturnType(method))
                    {
                        _reportDiagnostic(NotAllTypeParametersUsedInReturnType(
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

                    var isAsync = returnType.IsWellKnownTaskType(_wellKnownTypes, out var taskOfType);
                    var decoratorOfType = isAsync ? taskOfType : returnType;

                    var decoratedParameters = method.Parameters.Where(x => x.Type.Equals(decoratorOfType, SymbolEqualityComparer.Default)).ToList();
                    if (decoratedParameters.Count == 0)
                    {
                        _reportDiagnostic(DecoratorFactoryMethodDoesNotHaveParameterOfDecoratedType(attribute, method, decoratorOfType, _cancellationToken));
                        return null;
                    }

                    if (decoratedParameters.Count > 1)
                    {
                        _reportDiagnostic(DecoratorFactoryMethodHasMultipleParametersOfDecoratedType(attribute, method, decoratorOfType, _cancellationToken));
                        return null;
                    }

                    var options = DecoratorOptions.Default;
                    var constructorArguments = attribute.ConstructorArguments;
                    if (!constructorArguments.IsDefault && constructorArguments.Length == 1 && constructorArguments[0] is { Kind: TypedConstantKind.Enum, Value: long value })
                    {
                        options = (DecoratorOptions)value;
                    }

                    return new DecoratorFactoryMethod(method, decoratorOfType, isGeneric, decoratedParameters[0].Ordinal, options.HasFlag(DecoratorOptions.Dispose), isAsync);
                }

                _reportDiagnostic(FactoryMethodReturnsVoid(
                    method,
                    attribute.ApplicationSyntaxReference?.GetSyntax(_cancellationToken).GetLocation() ?? Location.None));
            }

            return null;
        }

        private void AppendRegistrationProviderRegistrations(
            Dictionary<ITypeSymbol, InstanceSources> registrations,
            GenericRegistrationsResolver.Builder genericRegistrations,
            ImmutableArray<DecoratorSource>.Builder nonGenericDecorators,
            ImmutableArray<DecoratorFactoryMethod>.Builder genericDecorators,
            INamedTypeSymbol module,
            RegistrationsToInclude registrationsToInclude)
        {
            var context = new ModuleContext(module, registrationsToInclude);
            foreach (var provider in _registrationProviders)
            {
                foreach (var registration in provider.GetRegistrationsForModule(context))
                {
                    switch (registration)
                    {
                        case Plugins.Registration.MethodRegistration
                        {
                            Method: var method,
                            Options: var options,
                            Scope: var scope,
                            DiagnosticLocation: var location,
                        }:
                            {
                                if (method.MethodKind == MethodKind.Constructor)
                                {
                                    // TODO: handle constructor
                                }
                                else
                                {
                                    var factoryMethod = CreateFactoryMethod(method, scope, location);
                                    if (factoryMethod != null)
                                    {
                                        factoryMethod = ApplyOptions(factoryMethod, options, registrations);
                                        registrations.WithInstanceSource(factoryMethod);
                                    }
                                    
                                    // TODO: check method class is not generic
                                    // TODO: check method has correct accessibility
                                    // TODO: handle generic methods.
                                }
                            }
                            break;
                        // TODO: handle other registration types 
                    }
                }
            }
        }
        
        private static Diagnostic DoesNotHaveSuitableConversion(AttributeData registerAttribute, INamedTypeSymbol registeredType, INamedTypeSymbol registeredAsType, CancellationToken cancellationToken)
        {
            return Diagnostic.Create(
                new DiagnosticDescriptor(
                    "SI0001",
                    "Registered type does not have an identity, implicit reference, boxing or nullable conversion to registered as type",
                    "'{0}' does not have an identity, implicit reference, boxing or nullable conversion to '{1}'.",
                    "StrongInject",
                    DiagnosticSeverity.Error,
                    isEnabledByDefault: true),
                registerAttribute.ApplicationSyntaxReference?.GetSyntax(cancellationToken).GetLocation() ?? Location.None,
                registeredType,
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

        private static Diagnostic NoConstructor(AttributeData registerAttribute, INamedTypeSymbol type, CancellationToken cancellationToken)
        {
            return Diagnostic.Create(
                new DiagnosticDescriptor(
                    "SI0005",
                    "Registered type does not have any public constructors",
                    "'{0}' does not have any public constructors.",
                    "StrongInject",
                    DiagnosticSeverity.Error,
                    isEnabledByDefault: true),
                registerAttribute.ApplicationSyntaxReference?.GetSyntax(cancellationToken).GetLocation() ?? Location.None,
                type);
        }

        private static Diagnostic MultipleConstructors(AttributeData registerAttribute, INamedTypeSymbol type, CancellationToken cancellationToken)
        {
            return Diagnostic.Create(
                new DiagnosticDescriptor(
                    "SI0006",
                    "Registered type has multiple non-default public constructors",
                    "'{0}' has multiple non-default public constructors.",
                    "StrongInject",
                    DiagnosticSeverity.Error,
                    isEnabledByDefault: true),
                registerAttribute.ApplicationSyntaxReference?.GetSyntax(cancellationToken).GetLocation() ?? Location.None,
                type);
        }

        private static Diagnostic StructWithSingleInstanceScope(AttributeData registerAttribute, ITypeSymbol type, CancellationToken cancellationToken)
        {
            return Diagnostic.Create(
                new DiagnosticDescriptor(
                    "SI0008",
                    "Struct cannot have Single Instance scope",
                    "'{0}' is a struct and cannot have a Single Instance scope.",
                    "StrongInject",
                    DiagnosticSeverity.Error,
                    isEnabledByDefault: true),
                registerAttribute.ApplicationSyntaxReference?.GetSyntax(cancellationToken).GetLocation() ?? Location.None,
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

        private static Diagnostic NotAllTypeParametersUsedInReturnType(IMethodSymbol methodSymbol, Location location)
        {
            return Diagnostic.Create(
                new DiagnosticDescriptor(
                    "SI0020",
                    "All type parameters must be used in return type of generic Factory Method",
                    "All type parameters must be used in return type of generic factory method '{0}'",
                    "StrongInject",
                    DiagnosticSeverity.Error,
                    isEnabledByDefault: true),
                location,
                methodSymbol);
        }

        private static Diagnostic InstancePropertyIsWriteOnly(IPropertySymbol propertySymbol, Location location)
        {
            return Diagnostic.Create(
                new DiagnosticDescriptor(
                    "SI0021",
                    "Instance property is write only",
                    "Instance property '{0}' is write only.",
                    "StrongInject",
                    DiagnosticSeverity.Error,
                    isEnabledByDefault: true),
                location,
                propertySymbol);
        }

        private static Diagnostic DecoratorDoesNotHaveParameterOfDecoratedType(AttributeData registerDecoratorAttribute, INamedTypeSymbol decoratorType, INamedTypeSymbol decoratedType, CancellationToken cancellationToken)
        {
            return Diagnostic.Create(
                new DiagnosticDescriptor(
                    "SI0022",
                    "Decorator Type does not have a constructor parameter of decorated type",
                    "Decorator '{0}' does not have a constructor parameter of decorated type '{1}'.",
                    "StrongInject",
                    DiagnosticSeverity.Error,
                    isEnabledByDefault: true),
                registerDecoratorAttribute.ApplicationSyntaxReference?.GetSyntax(cancellationToken).GetLocation() ?? Location.None,
                decoratorType,
                decoratedType);
        }

        private static Diagnostic DecoratorHasMultipleParametersOfDecoratedType(AttributeData registerDecoratorAttribute, INamedTypeSymbol decoratorType, INamedTypeSymbol decoratedType, CancellationToken cancellationToken)
        {
            return Diagnostic.Create(
                new DiagnosticDescriptor(
                    "SI0023",
                    "Decorator Type has multiple constructor parameters of decorated type",
                    "Decorator '{0}' has multiple constructor parameters of decorated type '{1}'.",
                    "StrongInject",
                    DiagnosticSeverity.Error,
                    isEnabledByDefault: true),
                registerDecoratorAttribute.ApplicationSyntaxReference?.GetSyntax(cancellationToken).GetLocation() ?? Location.None,
                decoratorType,
                decoratedType);
        }

        private static Diagnostic DecoratorFactoryMethodDoesNotHaveParameterOfDecoratedType(AttributeData decoratorFactoryAttribute, IMethodSymbol method, ITypeSymbol decoratedType, CancellationToken cancellationToken)
        {
            return Diagnostic.Create(
                new DiagnosticDescriptor(
                    "SI0024",
                    "Decorator Factory Method does not have a parameter of decorated type",
                    "Decorator Factory '{0}' does not have a parameter of decorated type '{1}'.",
                    "StrongInject",
                    DiagnosticSeverity.Error,
                    isEnabledByDefault: true),
                decoratorFactoryAttribute.ApplicationSyntaxReference?.GetSyntax(cancellationToken).GetLocation() ?? Location.None,
                method,
                decoratedType);
        }

        private static Diagnostic DecoratorFactoryMethodHasMultipleParametersOfDecoratedType(AttributeData decoratorFactoryAttribute, IMethodSymbol method, ITypeSymbol decoratedType, CancellationToken cancellationToken)
        {
            return Diagnostic.Create(
                new DiagnosticDescriptor(
                    "SI0025",
                    "Decorator Factory Method has multiple constructor parameters of decorated type",
                    "Decorator Factory '{0}' has multiple constructor parameters of decorated type '{1}'.",
                    "StrongInject",
                    DiagnosticSeverity.Error,
                    isEnabledByDefault: true),
                decoratorFactoryAttribute.ApplicationSyntaxReference?.GetSyntax(cancellationToken).GetLocation() ?? Location.None,
                method,
                decoratedType);
        }

        private static Diagnostic TypeDoesNotHaveAtLeastInternalAccessibility(ITypeSymbol typeSymbol, Location location)
        {
            return Diagnostic.Create(
                new DiagnosticDescriptor(
                    "SI0026",
                    "Type must have at least internal Accessibility. Try making it internal or public.",
                    "'{0}' must have at least internal Accessibility. Try making it internal or public.",
                    "StrongInject",
                    DiagnosticSeverity.Error,
                    isEnabledByDefault: true),
                location,
                typeSymbol);
        }

        private static Diagnostic FactoryOfMethodMustBeGeneric(IMethodSymbol methodSymbol, Location location)
        {
            return Diagnostic.Create(
                new DiagnosticDescriptor(
                    "SI0027",
                    "Method marked with FactoryOfAttribute must be generic.",
                    "Method '{0}' marked with FactoryOfAttribute must be generic.",
                    "StrongInject",
                    DiagnosticSeverity.Error,
                    isEnabledByDefault: true),
                location,
                methodSymbol);
        }

        private static Diagnostic FactoryOfOpenGenericMustReturnSingleTypeParamater(IMethodSymbol methodSymbol, ITypeSymbol ofType, Location location)
        {
            return Diagnostic.Create(
                new DiagnosticDescriptor(
                    "SI0028",
                    "Method marked with FactoryOfAttribute of open generic type must have a single type parameter, and return that type parameter.",
                    "Method '{0}' marked with FactoryOfAttribute of open generic type '{1}' must have a single type parameter, and return that type parameter.",
                    "StrongInject",
                    DiagnosticSeverity.Error,
                    isEnabledByDefault: true),
                location,
                methodSymbol,
                ofType);
        }

        private static Diagnostic CannotConstructFactoryOfTypeFromMethod(IMethodSymbol methodSymbol, ITypeSymbol ofType, Location location)
        {
            return Diagnostic.Create(
                new DiagnosticDescriptor(
                    "SI0029",
                    "FactoryOfAttribute Type cannot be constructed from the return type of method.",
                    "FactoryOfAttribute Type '{0}' cannot be constructed from the return type '{1}' of method '{2}'.",
                    "StrongInject",
                    DiagnosticSeverity.Error,
                    isEnabledByDefault: true),
                location,
                ofType,
                methodSymbol.ReturnType,
                methodSymbol);
        }

        private static Diagnostic CannotConstructFactoryOfTypeFromMethodAsConstraintsDoNotMatch(IMethodSymbol methodSymbol, ITypeSymbol ofType, Location location)
        {
            return Diagnostic.Create(
                new DiagnosticDescriptor(
                    "SI0030",
                    "FactoryOfAttribute Type cannot be constructed from the return type of method as constraints do not match.",
                    "FactoryOfAttribute Type '{0}' cannot be constructed from the return type '{1}' of method '{2}'  as constraints do not match.",
                    "StrongInject",
                    DiagnosticSeverity.Error,
                    isEnabledByDefault: true),
                location,
                ofType,
                methodSymbol.ReturnType,
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

        private static Diagnostic WarnFactoryMethodNotPublicStaticOrProtected(ITypeSymbol module, IMethodSymbol method, Location location)
        {
            return Diagnostic.Create(
                new DiagnosticDescriptor(
                    "SI1002",
                    "Factory Method is not either public and static, or protected, and containing module is not a container, so will be ignored",
                    "Factory method '{0}' is not either public and static, or protected, and containing module '{1}' is not a container, so will be ignored.",
                    "StrongInject",
                    DiagnosticSeverity.Warning,
                    isEnabledByDefault: true),
                location,
                method,
                module);
        }

        private static Diagnostic WarnInstanceFieldOrPropertyNotPublicStaticOrProtected(ITypeSymbol module, ISymbol fieldOrProperty, Location location)
        {
            return Diagnostic.Create(
                new DiagnosticDescriptor(
                    "SI1004",
                    "Instance FieldOrProperty is not either public and static, or protected, and containing module is not a container, so will be ignored",
                    "Instance {0} '{1}' is not either public and static, or protected, and containing module '{2}' is not a container, so will be ignored.",
                    "StrongInject",
                    DiagnosticSeverity.Warning,
                    isEnabledByDefault: true),
                location,
                fieldOrProperty is IFieldSymbol ? "field" : "property",
                fieldOrProperty,
                module);
        }

        private static Diagnostic WarnTypeNotPublic(ITypeSymbol typeSymbol, ITypeSymbol moduleSymbol, Location location)
        {
            return Diagnostic.Create(
                new DiagnosticDescriptor(
                    "SI1005",
                    "Type is not public, but is registered with public Module. If Module is imported outside this assembly this may result in errors. Try making Module internal.",
                    "'{0}' is not public, but is registered with public module '{1}'. If '{1}' is imported outside this assembly this may result in errors. Try making '{1}' internal.",
                    "StrongInject",
                    DiagnosticSeverity.Warning,
                    isEnabledByDefault: true),
                location,
                typeSymbol,
                moduleSymbol);
        }

        private static Diagnostic WarnModuleNotPublic(ITypeSymbol importedModuleSymbol, ITypeSymbol moduleSymbol, Location location)
        {
            return Diagnostic.Create(
                new DiagnosticDescriptor(
                    "SI1006",
                    "Module is not public, but is imported by public Module. If Module is imported outside this assembly this may result in errors. Try making Module internal.",
                    "'{0}' is not public, but is imported by public module '{1}'. If '{1}' is imported outside this assembly this may result in errors. Try making '{1}' internal.",
                    "StrongInject",
                    DiagnosticSeverity.Warning,
                    isEnabledByDefault: true),
                location,
                importedModuleSymbol,
                moduleSymbol);
        }
    }
}
