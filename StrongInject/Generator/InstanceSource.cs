using Microsoft.CodeAnalysis;

namespace StrongInject.Generator
{
    internal record InstanceSource(Scope scope, bool isAsync) { }

    internal record Registration(
        INamedTypeSymbol type,
        ITypeSymbol registeredAs,
        Scope scope,
        bool requiresInitialization,
        IMethodSymbol constructor,
        bool isAsync) : InstanceSource(scope, isAsync)
    { }
    internal record InstanceProvider(
        ITypeSymbol providedType,
        IFieldSymbol instanceProviderField,
        INamedTypeSymbol castTo,
        bool isAsync) : InstanceSource(Scope.InstancePerResolution, isAsync) { }
    internal record FactoryRegistration(ITypeSymbol factoryType,
        ITypeSymbol factoryOf,
        Scope scope,
        bool isAsync) : InstanceSource(scope, isAsync) { }
}
