using Microsoft.CodeAnalysis;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;

namespace StrongInject.Generator
{
    internal record WellKnownTypes(
        INamedTypeSymbol IContainer,
        INamedTypeSymbol IAsyncContainer,
        INamedTypeSymbol IFactory,
        INamedTypeSymbol IAsyncFactory,
        INamedTypeSymbol IRequiresInitialization,
        INamedTypeSymbol IRequiresAsyncInitialization,
        INamedTypeSymbol IDisposable,
        INamedTypeSymbol IAsyncDisposable,
        INamedTypeSymbol ConcurrentBagOfAction,
        INamedTypeSymbol ConcurrentBagOfFuncTask,
        INamedTypeSymbol IOwned,
        INamedTypeSymbol Owned,
        INamedTypeSymbol IAsyncOwned,
        INamedTypeSymbol AsyncOwned,
        INamedTypeSymbol RegisterAttribute,
        INamedTypeSymbol RegisterAttribute_1,
        INamedTypeSymbol RegisterAttribute_2,
        INamedTypeSymbol RegisterModuleAttribute,
        INamedTypeSymbol RegisterFactoryAttribute,
        INamedTypeSymbol RegisterDecoratorAttribute,
        INamedTypeSymbol RegisterDecoratorAttribute_2,
        INamedTypeSymbol FactoryAttribute,
        INamedTypeSymbol DecoratorFactoryAttribute,
        INamedTypeSymbol FactoryOfAttribute,
        INamedTypeSymbol InstanceAttribute,
        INamedTypeSymbol ValueTask,
        INamedTypeSymbol ValueTask1,
        INamedTypeSymbol Task1,
        INamedTypeSymbol ObjectDisposedException,
        INamedTypeSymbol Helpers)
    {
        public const string STRONGINJECT_NAMESPACE = "StrongInject";
        public const string ICONTAINER_MD_NAME = "IContainer`1";
        public const string IASYNC_CONTAINER_MD_NAME = "IAsyncContainer`1";
        public const string REGISTER_ATTRIBUTE_MD_NAME = "RegisterAttribute";
        public const string REGISTER_ATTRIBUTE_1_MD_NAME = "RegisterAttribute`1";
        public const string REGISTER_ATTRIBUTE_2_MD_NAME = "RegisterAttribute`2";
        public const string REGISTER_MODULE_MD_NAME = "RegisterModuleAttribute";
        public const string REGISTER_FACTORY_ATTRIBUTE_MD_NAME = "RegisterFactoryAttribute";
        public const string REGISTER_DECORATOR_ATTRIBUTE_MD_NAME = "RegisterDecoratorAttribute";
        public const string REGISTER_DECORATOR_ATTRIBUTE_2_MD_NAME = "RegisterDecoratorAttribute`2";
        public const string FACTORY_ATTRIBUTE_MD_NAME = "FactoryAttribute";
        public const string DECORATOR_FACTORY_ATTRIBUTE_MD_NAME = "DecoratorFactoryAttribute";
        public const string FACTORY_OF_ATTRIBUTE_MD_NAME = "FactoryOfAttribute";
        public const string INSTANCE_ATTRIBUTE_MD_NAME = "InstanceAttribute";
        
        public static bool TryCreate(Compilation compilation, Action<Diagnostic> reportDiagnostic, out WellKnownTypes wellKnownTypes)
        {
            var iContainer = compilation.GetTypeOrReport(STRONGINJECT_NAMESPACE + "." + ICONTAINER_MD_NAME, reportDiagnostic);
            var iAsyncContainer = compilation.GetTypeOrReport(STRONGINJECT_NAMESPACE + "." + IASYNC_CONTAINER_MD_NAME, reportDiagnostic);
            var iFactory = compilation.GetTypeOrReport("StrongInject.IFactory`1", reportDiagnostic);
            var iAsyncFactory = compilation.GetTypeOrReport("StrongInject.IAsyncFactory`1", reportDiagnostic);
            var iRequiresInitialization = compilation.GetTypeOrReport("StrongInject.IRequiresInitialization", reportDiagnostic);
            var iRequiresAsyncInitialization = compilation.GetTypeOrReport("StrongInject.IRequiresAsyncInitialization", reportDiagnostic);
            var iDisposable = compilation.GetTypeOrReport("System.IDisposable", reportDiagnostic);
            var iAsyncDisposable = compilation.GetTypeOrReport("System.IAsyncDisposable", reportDiagnostic);
            var action = compilation.GetTypeOrReport("System.Action", reportDiagnostic);
            var concurrentBag = compilation.GetTypeOrReport("System.Collections.Concurrent.ConcurrentBag`1", reportDiagnostic);
            var concurrentBagOfAction = action is null
                ? null
                : concurrentBag?.Construct(action);
            var valueTask = compilation.GetTypeOrReport("System.Threading.Tasks.ValueTask", reportDiagnostic);
            var funcOfTask = valueTask is null
                ? null
                : compilation.GetTypeOrReport("System.Func`1", reportDiagnostic)?.Construct(valueTask);
            var concurrentBagOfFuncTask = funcOfTask is null
                ? null
                : concurrentBag?.Construct(funcOfTask);
            var iOwned = compilation.GetTypeOrReport("StrongInject.IOwned`1", reportDiagnostic);
            var owned = compilation.GetTypeOrReport("StrongInject.Owned`1", reportDiagnostic);
            var iAsyncOwned = compilation.GetTypeOrReport("StrongInject.IAsyncOwned`1", reportDiagnostic);
            var asyncOwned = compilation.GetTypeOrReport("StrongInject.AsyncOwned`1", reportDiagnostic);
            var registerAttribute = compilation.GetTypeOrReport(STRONGINJECT_NAMESPACE + "." + REGISTER_ATTRIBUTE_MD_NAME, reportDiagnostic);
            var registerAttribute_1 = compilation.GetTypeOrReport(STRONGINJECT_NAMESPACE + "." + REGISTER_ATTRIBUTE_1_MD_NAME, reportDiagnostic);
            var registerAttribute_2 = compilation.GetTypeOrReport(STRONGINJECT_NAMESPACE + "." + REGISTER_ATTRIBUTE_2_MD_NAME, reportDiagnostic);
            var registerModuleAttribute = compilation.GetTypeOrReport(STRONGINJECT_NAMESPACE + "." + REGISTER_MODULE_MD_NAME, reportDiagnostic);
            var registerFactoryAttribute = compilation.GetTypeOrReport(STRONGINJECT_NAMESPACE + "." + REGISTER_FACTORY_ATTRIBUTE_MD_NAME, reportDiagnostic);
            var registerDecoratorAttribute = compilation.GetTypeOrReport(STRONGINJECT_NAMESPACE + "." + REGISTER_DECORATOR_ATTRIBUTE_MD_NAME, reportDiagnostic);
            var registerDecoratorAttribute_2 = compilation.GetTypeOrReport(STRONGINJECT_NAMESPACE + "." + REGISTER_DECORATOR_ATTRIBUTE_2_MD_NAME, reportDiagnostic);
            var factoryAttribute = compilation.GetTypeOrReport(STRONGINJECT_NAMESPACE + "." + FACTORY_ATTRIBUTE_MD_NAME, reportDiagnostic);
            var decoratorFactoryAttribute = compilation.GetTypeOrReport(STRONGINJECT_NAMESPACE + "." + DECORATOR_FACTORY_ATTRIBUTE_MD_NAME, reportDiagnostic);
            var factoryOfAttribute = compilation.GetTypeOrReport(STRONGINJECT_NAMESPACE + "." + FACTORY_OF_ATTRIBUTE_MD_NAME, reportDiagnostic);
            var instanceAttribute = compilation.GetTypeOrReport(STRONGINJECT_NAMESPACE + "." + INSTANCE_ATTRIBUTE_MD_NAME, reportDiagnostic);
            var valueTask1 = compilation.GetTypeOrReport("System.Threading.Tasks.ValueTask`1", reportDiagnostic);
            var task1 = compilation.GetTypeOrReport("System.Threading.Tasks.Task`1", reportDiagnostic);
            var objectDisposedException = compilation.GetTypeOrReport("System.ObjectDisposedException", reportDiagnostic);
            var helpers = compilation.GetTypeOrReport("StrongInject.Helpers", reportDiagnostic);

            if (iContainer is null
                || iAsyncContainer is null
                || iFactory is null
                || iAsyncFactory is null
                || iRequiresInitialization is null
                || iRequiresAsyncInitialization is null
                || iDisposable is null
                || iAsyncDisposable is null
                || concurrentBagOfAction is null
                || concurrentBagOfFuncTask is null
                || iOwned is null
                || owned is null
                || iAsyncOwned is null
                || asyncOwned is null
                || registerAttribute is null
                || registerAttribute_1 is null
                || registerAttribute_2 is null
                || registerModuleAttribute is null
                || registerFactoryAttribute is null
                || registerDecoratorAttribute is null
                || registerDecoratorAttribute_2 is null
                || factoryAttribute is null
                || decoratorFactoryAttribute is null
                || factoryOfAttribute is null
                || instanceAttribute is null
                || valueTask is null
                || valueTask1 is null
                || task1 is null
                || objectDisposedException is null
                || helpers is null)
            {
                wellKnownTypes = null!;
                return false;
            }

            wellKnownTypes = new WellKnownTypes(
                IContainer: iContainer,
                IAsyncContainer: iAsyncContainer,
                IFactory: iFactory,
                IAsyncFactory: iAsyncFactory,
                IRequiresInitialization: iRequiresInitialization,
                IRequiresAsyncInitialization: iRequiresAsyncInitialization,
                IDisposable: iDisposable,
                IAsyncDisposable: iAsyncDisposable,
                ConcurrentBagOfAction: concurrentBagOfAction,
                ConcurrentBagOfFuncTask: concurrentBagOfFuncTask,
                IOwned: iOwned,
                Owned: owned,
                IAsyncOwned: iAsyncOwned,
                AsyncOwned: asyncOwned,
                RegisterAttribute: registerAttribute,
                RegisterAttribute_1: registerAttribute_1,
                RegisterAttribute_2: registerAttribute_2,
                RegisterModuleAttribute: registerModuleAttribute,
                RegisterFactoryAttribute: registerFactoryAttribute,
                RegisterDecoratorAttribute: registerDecoratorAttribute,
                RegisterDecoratorAttribute_2: registerDecoratorAttribute_2,
                FactoryAttribute: factoryAttribute,
                DecoratorFactoryAttribute: decoratorFactoryAttribute,
                FactoryOfAttribute: factoryOfAttribute,
                InstanceAttribute: instanceAttribute,
                ValueTask: valueTask,
                ValueTask1: valueTask1,
                Task1: task1,
                ObjectDisposedException: objectDisposedException,
                Helpers: helpers);

            return true;
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

        private static bool IsStrongInjectType(INamedTypeSymbol? type)
        {
            return type is
            {
                ContainingType: null,
                ContainingNamespace:
                {
                    Name: STRONGINJECT_NAMESPACE,
                    ContainingNamespace: null or { IsGlobalNamespace: true }
                }
            };
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
    }
}