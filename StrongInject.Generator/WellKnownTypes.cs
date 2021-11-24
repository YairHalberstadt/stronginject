using Microsoft.CodeAnalysis;
using System;
using System.Collections.Generic;

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
        public static bool TryCreate(Compilation compilation, Action<Diagnostic> reportDiagnostic, out WellKnownTypes wellKnownTypes)
        {
            var iContainer = compilation.GetTypeOrReport("StrongInject.IContainer`1", reportDiagnostic);
            var iAsyncContainer = compilation.GetTypeOrReport("StrongInject.IAsyncContainer`1", reportDiagnostic);
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
            var registerAttribute = compilation.GetTypeOrReport("StrongInject.RegisterAttribute", reportDiagnostic);
            var registerAttribute_1 = compilation.GetTypeOrReport("StrongInject.RegisterAttribute`1", reportDiagnostic);
            var registerAttribute_2 = compilation.GetTypeOrReport("StrongInject.RegisterAttribute`2", reportDiagnostic);
            var registerModuleAttribute = compilation.GetTypeOrReport("StrongInject.RegisterModuleAttribute", reportDiagnostic);
            var registerFactoryAttribute = compilation.GetTypeOrReport("StrongInject.RegisterFactoryAttribute", reportDiagnostic);
            var registerDecoratorAttribute = compilation.GetTypeOrReport("StrongInject.RegisterDecoratorAttribute", reportDiagnostic);
            var registerDecoratorAttribute_2 = compilation.GetTypeOrReport("StrongInject.RegisterDecoratorAttribute`2", reportDiagnostic);
            var factoryAttribute = compilation.GetTypeOrReport("StrongInject.FactoryAttribute", reportDiagnostic);
            var decoratorFactoryAttribute = compilation.GetTypeOrReport("StrongInject.DecoratorFactoryAttribute", reportDiagnostic);
            var factoryOfAttribute = compilation.GetTypeOrReport("StrongInject.FactoryOfAttribute", reportDiagnostic);
            var instanceAttribute = compilation.GetTypeOrReport("StrongInject.InstanceAttribute", reportDiagnostic);
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
        
        public IEnumerable<INamedTypeSymbol> GetClassAttributes()
        {
            return new[]
            { 
                RegisterAttribute,
                RegisterAttribute_1,
                RegisterAttribute_2,
                RegisterDecoratorAttribute,
                RegisterDecoratorAttribute_2,
                RegisterFactoryAttribute,
                RegisterModuleAttribute,
            };
        }

        public IEnumerable<INamedTypeSymbol> GetMemberAttributes()
        {
            return new[]
            {
                FactoryAttribute,
                FactoryOfAttribute,
                DecoratorFactoryAttribute,
                InstanceAttribute,
            };
        }
    }
}