using Microsoft.CodeAnalysis;
using StrongInject.Runtime;

namespace StrongInject.Generator
{
    internal record InstanceSource(Lifetime lifetime) { }

    internal record Registration(
        INamedTypeSymbol type,
        ITypeSymbol registeredAs,
        Lifetime lifetime,
        bool requiresAsyncInitialization,
        IMethodSymbol constructor) : InstanceSource(lifetime) {}
    internal record InstanceProvider(ITypeSymbol providedType, IFieldSymbol instanceProviderField, INamedTypeSymbol castTo) : InstanceSource(Lifetime.InstancePerDependency) { }
    internal record FactoryRegistration(ITypeSymbol factoryType, ITypeSymbol factoryOf, Lifetime lifetime) : InstanceSource(lifetime) { }
}
