using Microsoft.CodeAnalysis;
using StrongInject.Runtime;

namespace StrongInject.Generator
{
    internal record InstanceSource { }

    internal record Registration(
        INamedTypeSymbol type,
        ITypeSymbol registeredAs,
        Lifetime lifetime,
        INamedTypeSymbol castTarget,
        bool isFactory,
        bool requiresAsyncInitialization,
        IMethodSymbol constructor) : InstanceSource {}
    internal record InstanceProvider(ITypeSymbol providedType, IFieldSymbol instanceProviderField, INamedTypeSymbol castTo) : InstanceSource { }
    internal record InstanceParam(ITypeSymbol paramType, IParameterSymbol param) : InstanceSource { }
}
