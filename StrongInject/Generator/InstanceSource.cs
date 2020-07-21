using Microsoft.CodeAnalysis;

namespace StrongInject.Generator
{
    internal record InstanceSource(Scope scope) { }

    internal record Registration(
        INamedTypeSymbol type,
        ITypeSymbol registeredAs,
        Scope scope,
        bool requiresAsyncInitialization,
        IMethodSymbol constructor) : InstanceSource(scope)
    { }
    internal record InstanceProvider(ITypeSymbol providedType, IFieldSymbol instanceProviderField, INamedTypeSymbol castTo) : InstanceSource(Scope.InstancePerResolution) { }
    internal record FactoryRegistration(ITypeSymbol factoryType, ITypeSymbol factoryOf, Scope scope) : InstanceSource(scope) { }
}
