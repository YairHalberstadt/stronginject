using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System;

namespace StrongInject.Generator
{
    internal record WellKnownTypes(INamedTypeSymbol Owned, INamedTypeSymbol AsyncOwned)
    {
        public const string IREQUIRES_INITIALIZATION_EMIT_NAME = "global::StrongInject.IRequiresInitialization";
        public const string IREQUIRES_ASYNC_INITIALIZATION_EMIT_NAME = "global::StrongInject.IRequiresAsyncInitialization";
        public const string HELPERS_EMIT_NAME = "global::StrongInject.Helpers";

        public const string IDISPOSABLE_EMIT_NAME = "global::System.IDisposable";
        public const string IASYNC_DISPOSABLE_EMIT_NAME = "global::System.IAsyncDisposable";
        public const string CONCURRENT_BAG_ACTION_EMIT_NAME = "global::System.Collections.Concurrent.ConcurrentBag<global::System.Action>";
        public const string CONCURRENT_BAG_FUNC_TASK_EMIT_NAME = "global::System.Collections.Concurrent.ConcurrentBag<global::System.Func<global::System.Threading.Tasks.ValueTask>>";
        public const string VALUE_TASK_EMIT_NAME = "global::System.Threading.Tasks.ValueTask";
        public const string OBJECT_DISPOSED_EXCEPTION_EMIT_NAME = "global::System.ObjectDisposedException";
        
        public static string ConstructedOwnedEmitName(string typeArgument) => $"global::StrongInject.Owned<{typeArgument}>";
        public static string ConstructedAsyncOwnedEmitName(string typeArgument) => $"global::StrongInject.AsyncOwned<{typeArgument}>";
        public static string ConstructedValueTask1EmitName(string typeArgument) => $"global::System.Threading.Tasks.ValueTask<{typeArgument}>";
        
        public static string ConstructedOwnedDisplayName(string typeArgument) => $"StrongInject.Owned<{typeArgument}>";
        public static string ConstructedAsyncOwnedDisplayName(string typeArgument) => $"StrongInject.AsyncOwned<{typeArgument}>";
        
        
        private const string STRONGINJECT_NAMESPACE = "StrongInject";
        private const string SYSTEM_NAMESPACE = "System";
        
        private const string ICONTAINER_MD_NAME = "IContainer`1";
        private const string IASYNC_CONTAINER_MD_NAME = "IAsyncContainer`1";
        private const string IFACTORY_MD_NAME = "IFactory`1";
        private const string IASYNC_FACTORY_MD_NAME = "IAsyncFactory`1";
        private const string IREQUIRES_INITIALIZATION_MD_NAME = "IRequiresInitialization";
        private const string IREQUIRES_ASYNC_INITIALIZATION_MD_NAME = "IRequiresAsyncInitialization";
        private const string OWNED_MD_NAME = "Owned`1";
        private const string IOWNED_MD_NAME = "IOwned`1";
        private const string ASYNC_OWNED_MD_NAME = "AsyncOwned`1";
        private const string IASYNC_OWNED_MD_NAME = "IAsyncOwned`1";
        
        private const string REGISTER_ATTRIBUTE_MD_NAME = "RegisterAttribute";
        private const string REGISTER_ATTRIBUTE_1_MD_NAME = "RegisterAttribute`1";
        private const string REGISTER_ATTRIBUTE_2_MD_NAME = "RegisterAttribute`2";
        private const string REGISTER_MODULE_MD_NAME = "RegisterModuleAttribute";
        private const string REGISTER_FACTORY_ATTRIBUTE_MD_NAME = "RegisterFactoryAttribute";
        private const string REGISTER_DECORATOR_ATTRIBUTE_MD_NAME = "RegisterDecoratorAttribute";
        private const string REGISTER_DECORATOR_ATTRIBUTE_2_MD_NAME = "RegisterDecoratorAttribute`2";
        private const string FACTORY_ATTRIBUTE_MD_NAME = "FactoryAttribute";
        private const string DECORATOR_FACTORY_ATTRIBUTE_MD_NAME = "DecoratorFactoryAttribute";
        private const string FACTORY_OF_ATTRIBUTE_MD_NAME = "FactoryOfAttribute";
        private const string INSTANCE_ATTRIBUTE_MD_NAME = "InstanceAttribute";
        
        private const string IDISPOSABLE_MD_NAME = "IDisposable";
        private const string IASYNC_DISPOSABLE_MD_NAME = "IAsyncDisposable";
        private const string TASK_1_MD_NAME = "Task`1";
        private const string VALUE_TASK_1_MD_NAME = "ValueTask`1";
        
        
        private const string ICONTAINER_SOURCE_NAME = "IContainer";
        private const string IASYNC_CONTAINER_SOURCE_NAME = "IAsyncContainer";
        
        private const string REGISTER_ATTRIBUTE_SOURCE_NAME = "RegisterAttribute";
        private const string REGISTER_ATTRIBUTE_SHORT_SOURCE_NAME = "Register";
        private const string REGISTER_MODULE_SOURCE_NAME = "RegisterModuleAttribute";
        private const string REGISTER_MODULE_SHORT_SOURCE_NAME = "RegisterModule";
        private const string REGISTER_FACTORY_ATTRIBUTE_SOURCE_NAME = "RegisterFactoryAttribute";
        private const string REGISTER_FACTORY_ATTRIBUTE_SHORT_SOURCE_NAME = "RegisterFactory";
        private const string REGISTER_DECORATOR_ATTRIBUTE_SOURCE_NAME = "RegisterDecoratorAttribute";
        private const string REGISTER_DECORATOR_ATTRIBUTE_SHORT_SOURCE_NAME = "RegisterDecorator";
        private const string FACTORY_ATTRIBUTE_SOURCE_NAME = "FactoryAttribute";
        private const string FACTORY_ATTRIBUTE_SHORT_SOURCE_NAME = "Factory";
        private const string DECORATOR_FACTORY_ATTRIBUTE_SOURCE_NAME = "DecoratorFactoryAttribute";
        private const string DECORATOR_FACTORY_ATTRIBUTE_SHORT_SOURCE_NAME = "DecoratorFactory";
        private const string FACTORY_OF_ATTRIBUTE_SOURCE_NAME = "FactoryOfAttribute";
        private const string FACTORY_OF_ATTRIBUTE_SHORT_SOURCE_NAME = "FactoryOf";
        private const string INSTANCE_ATTRIBUTE_SOURCE_NAME = "InstanceAttribute";
        private const string INSTANCE_ATTRIBUTE_SHORT_SOURCE_NAME = "Instance";
        
        
        public static bool TryCreate(Compilation compilation, Action<Diagnostic> reportDiagnostic, out WellKnownTypes wellKnownTypes)
        {
            var owned = compilation.GetTypeOrReport("StrongInject.Owned`1", reportDiagnostic);
            var asyncOwned = compilation.GetTypeOrReport("StrongInject.AsyncOwned`1", reportDiagnostic);

            if (owned is null || asyncOwned is null)
            {
                wellKnownTypes = null!;
                return false;
            }

            wellKnownTypes = new WellKnownTypes(Owned: owned, AsyncOwned: asyncOwned);

            return true;
        }

        private static bool IsStrongInjectType(ITypeSymbol? type)
        {
            return type is
            {
                ContainingType: null,
                ContainingNamespace:
                {
                    Name: STRONGINJECT_NAMESPACE,
                    ContainingNamespace: { IsGlobalNamespace: true }
                }
            };
        }
        
        private static bool IsSystemType(ITypeSymbol? type)
        {
            return type is
            {
                ContainingType: null,
                ContainingNamespace:
                {
                    Name: SYSTEM_NAMESPACE,
                    ContainingNamespace: { IsGlobalNamespace: true }
                }
            };
        }
        
        private static bool IsSystemThreadingTasksType(ITypeSymbol? type)
        {
            return type is
            {
                ContainingType: null,
                ContainingNamespace:
                {
                    Name: "Tasks",
                    ContainingNamespace:
                    {
                        Name: "Threading",
                        ContainingNamespace:
                        {
                            Name: SYSTEM_NAMESPACE,
                            ContainingNamespace: { IsGlobalNamespace: true }
                        }
                    }
                }
            };
        }
        
        public static bool IsContainerOrAsyncContainer(INamedTypeSymbol? type)
        {
            if (!IsStrongInjectType(type))
                return false;

            switch (type?.OriginalDefinition.MetadataName)
            {
                case ICONTAINER_MD_NAME:
                case IASYNC_CONTAINER_MD_NAME:
                    return true;
                default:
                    return false;
            }
        }
        
        public static bool IsAsyncContainer(INamedTypeSymbol? type)
        {
            if (!IsStrongInjectType(type))
                return false;

            return type?.OriginalDefinition.MetadataName is IASYNC_CONTAINER_MD_NAME;
        }
        
        public static bool IsRequiresInitialization(INamedTypeSymbol? type)
        {
            if (!IsStrongInjectType(type))
                return false;

            return type?.OriginalDefinition.MetadataName is IREQUIRES_INITIALIZATION_MD_NAME;
        }
        
        public static bool IsRequiresAsyncInitialization(INamedTypeSymbol? type)
        {
            if (!IsStrongInjectType(type))
                return false;

            return type?.OriginalDefinition.MetadataName is IREQUIRES_ASYNC_INITIALIZATION_MD_NAME;
        }
        
        public static bool IsFactoryOrAsyncFactory(ITypeSymbol? type)
        {
            if (!IsStrongInjectType(type))
                return false;

            switch (type?.OriginalDefinition.MetadataName)
            {
                case IFACTORY_MD_NAME:
                case IASYNC_FACTORY_MD_NAME:
                    return true;
                default:
                    return false;
            }
        }
        
        public static bool IsAsyncFactory(ITypeSymbol? type)
        {
            if (!IsStrongInjectType(type))
                return false;

            return type?.OriginalDefinition.MetadataName is IASYNC_FACTORY_MD_NAME;
        }
        
        public static bool IsOwnedOrIOwned(ITypeSymbol? type)
        {
            if (!IsStrongInjectType(type))
                return false;

            switch (type?.OriginalDefinition.MetadataName)
            {
                case OWNED_MD_NAME:
                case IOWNED_MD_NAME:
                    return true;
                default:
                    return false;
            }
        }
        
        public static bool IsAsyncOwnedOrIAsyncOwned(ITypeSymbol? type)
        {
            if (!IsStrongInjectType(type))
                return false;

            switch (type?.OriginalDefinition.MetadataName)
            {
                case ASYNC_OWNED_MD_NAME:
                case IASYNC_OWNED_MD_NAME:
                    return true;
                default:
                    return false;
            }
        }
        
        public static bool IsClassAttribute(INamedTypeSymbol? type)
        {
            if (!IsStrongInjectType(type))
                return false;

            switch (type?.OriginalDefinition.MetadataName)
            {
                case REGISTER_ATTRIBUTE_MD_NAME:
                case REGISTER_ATTRIBUTE_1_MD_NAME:
                case REGISTER_ATTRIBUTE_2_MD_NAME:
                case REGISTER_DECORATOR_ATTRIBUTE_MD_NAME:
                case REGISTER_DECORATOR_ATTRIBUTE_2_MD_NAME:
                case REGISTER_FACTORY_ATTRIBUTE_MD_NAME:
                case REGISTER_MODULE_MD_NAME:
                    return true;
                default:
                    return false;
            }
        }

        public static bool IsMethodAttribute(INamedTypeSymbol? type)
        {
            if (!IsStrongInjectType(type))
                return false;
            
            switch (type?.OriginalDefinition.MetadataName)
            {
                case FACTORY_ATTRIBUTE_MD_NAME:
                case FACTORY_OF_ATTRIBUTE_MD_NAME:
                case DECORATOR_FACTORY_ATTRIBUTE_MD_NAME:
                    return true;
                default:
                    return false;
            }
        }
        
        public static bool IsInstanceAttribute(INamedTypeSymbol? type)
        {
            if (!IsStrongInjectType(type))
                return false;
            
            return type?.OriginalDefinition.MetadataName is INSTANCE_ATTRIBUTE_MD_NAME;
        }
        
        public static bool IsRegisterAttribute(INamedTypeSymbol? type)
        {
            if (!IsStrongInjectType(type))
                return false;
            
            return type?.OriginalDefinition.MetadataName is REGISTER_ATTRIBUTE_MD_NAME;
        }
        
        public static bool IsRegisterAttribute1(INamedTypeSymbol? type)
        {
            if (!IsStrongInjectType(type))
                return false;
            
            return type?.OriginalDefinition.MetadataName is REGISTER_ATTRIBUTE_1_MD_NAME;
        }
        
        public static bool IsRegisterAttribute2(INamedTypeSymbol? type)
        {
            if (!IsStrongInjectType(type))
                return false;
            
            return type?.OriginalDefinition.MetadataName is REGISTER_ATTRIBUTE_2_MD_NAME;
        }
        
        public static bool IsRegisterModuleAttribute(INamedTypeSymbol? type)
        {
            if (!IsStrongInjectType(type))
                return false;
            
            return type?.OriginalDefinition.MetadataName is REGISTER_MODULE_MD_NAME;
        }
        
        public static bool IsRegisterFactoryAttribute(INamedTypeSymbol? type)
        {
            if (!IsStrongInjectType(type))
                return false;
            
            return type?.OriginalDefinition.MetadataName is REGISTER_FACTORY_ATTRIBUTE_MD_NAME;
        }
        
        public static bool IsRegisterDecoratorAttribute(INamedTypeSymbol? type)
        {
            if (!IsStrongInjectType(type))
                return false;
            
            return type?.OriginalDefinition.MetadataName is REGISTER_DECORATOR_ATTRIBUTE_MD_NAME;
        }
        
        public static bool IsRegisterDecoratorAttribute2(INamedTypeSymbol? type)
        {
            if (!IsStrongInjectType(type))
                return false;
            
            return type?.OriginalDefinition.MetadataName is REGISTER_DECORATOR_ATTRIBUTE_2_MD_NAME;
        }
        
        public static bool IsDecoratorFactoryAttribute(INamedTypeSymbol? type)
        {
            if (!IsStrongInjectType(type))
                return false;
            
            return type?.OriginalDefinition.MetadataName is DECORATOR_FACTORY_ATTRIBUTE_MD_NAME;
        }
        
        public static bool IsFactoryAttribute(INamedTypeSymbol? type)
        {
            if (!IsStrongInjectType(type))
                return false;
            
            return type?.OriginalDefinition.MetadataName is FACTORY_ATTRIBUTE_MD_NAME;
        }
        
        public static bool IsFactoryOfAttribute(INamedTypeSymbol? type)
        {
            if (!IsStrongInjectType(type))
                return false;
            
            return type?.OriginalDefinition.MetadataName is FACTORY_OF_ATTRIBUTE_MD_NAME;
        }
        
        public static bool IsIDisposable(INamedTypeSymbol? type)
        {
            if (!IsSystemType(type))
                return false;

            return type?.OriginalDefinition.MetadataName is IDISPOSABLE_MD_NAME;
        }
        
        public static bool IsIAsyncDisposable(INamedTypeSymbol? type)
        {
            if (!IsSystemType(type))
                return false;

            return type?.OriginalDefinition.MetadataName is IASYNC_DISPOSABLE_MD_NAME;
        }
        
        public static bool IsValueTask1(ITypeSymbol? type)
        {
            if (!IsSystemThreadingTasksType(type))
                return false;

            return type?.OriginalDefinition.MetadataName is VALUE_TASK_1_MD_NAME;
        }
        
        public static bool IsTask1OrValueTask1(ITypeSymbol? type)
        {
            if (!IsSystemThreadingTasksType(type))
                return false;

            return type?.OriginalDefinition.MetadataName is TASK_1_MD_NAME or VALUE_TASK_1_MD_NAME;
        }

        public static bool IsContainerCandidate(NameSyntax name)
        {
            var (identifier, arity) = GetIdentifier(name);

            return arity == 1 && identifier is ICONTAINER_SOURCE_NAME or IASYNC_CONTAINER_SOURCE_NAME;
        }
        
        public static bool IsClassAttributeCandidate(NameSyntax name)
        {
            var (identifier, _) = GetIdentifier(name);
            switch (identifier)
            {
                case REGISTER_ATTRIBUTE_SOURCE_NAME:
                case REGISTER_ATTRIBUTE_SHORT_SOURCE_NAME:
                case REGISTER_DECORATOR_ATTRIBUTE_SOURCE_NAME:
                case REGISTER_DECORATOR_ATTRIBUTE_SHORT_SOURCE_NAME:
                case REGISTER_FACTORY_ATTRIBUTE_SOURCE_NAME:
                case REGISTER_FACTORY_ATTRIBUTE_SHORT_SOURCE_NAME:
                case REGISTER_MODULE_SOURCE_NAME:
                case REGISTER_MODULE_SHORT_SOURCE_NAME:
                    return true;
                default:
                    return false;
            }
        }
        
        public static bool IsMemberAttributeCandidate(NameSyntax name)
        {
            var (identifier, _) = GetIdentifier(name);
            
            switch (identifier)
            {
                case FACTORY_ATTRIBUTE_SOURCE_NAME:
                case FACTORY_ATTRIBUTE_SHORT_SOURCE_NAME:
                case FACTORY_OF_ATTRIBUTE_SOURCE_NAME:
                case FACTORY_OF_ATTRIBUTE_SHORT_SOURCE_NAME:
                case DECORATOR_FACTORY_ATTRIBUTE_SOURCE_NAME:
                case DECORATOR_FACTORY_ATTRIBUTE_SHORT_SOURCE_NAME:
                case INSTANCE_ATTRIBUTE_SOURCE_NAME:
                case INSTANCE_ATTRIBUTE_SHORT_SOURCE_NAME:
                    return true;
                default:
                    return false;
            }
        }

        private static (string identifier, int arity) GetIdentifier(NameSyntax name)
        {
            var simpleName = name switch
            {
                SimpleNameSyntax s => s,
                AliasQualifiedNameSyntax { Name: var s } => s,
                QualifiedNameSyntax { Right: var s } => s,
                var other => throw new NotImplementedException(other.GetType().ToString())
            };

            return (simpleName.Identifier.Text, simpleName.Arity);
        }
    }
}