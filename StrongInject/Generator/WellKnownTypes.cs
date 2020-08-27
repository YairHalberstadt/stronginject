using Microsoft.CodeAnalysis;
using System;
using System.Threading.Tasks;

namespace StrongInject.Generator
{
    internal record WellKnownTypes(
        INamedTypeSymbol IContainer,
        INamedTypeSymbol IAsyncContainer,
        INamedTypeSymbol IInstanceProvider,
        INamedTypeSymbol IAsyncInstanceProvider,
        INamedTypeSymbol IFactory,
        INamedTypeSymbol IAsyncFactory,
        INamedTypeSymbol IRequiresInitialization,
        INamedTypeSymbol IRequiresAsyncInitialization,
        INamedTypeSymbol IDisposable,
        INamedTypeSymbol IAsyncDisposable,
        INamedTypeSymbol Owned,
        INamedTypeSymbol AsyncOwned,
        INamedTypeSymbol RegisterAttribute,
        INamedTypeSymbol RegisterModuleAttribute,
        INamedTypeSymbol RegisterFactoryAttribute,
        INamedTypeSymbol FactoryAttribute,
        INamedTypeSymbol InstanceAttribute,
        INamedTypeSymbol ValueTask1,
        INamedTypeSymbol Task1,
        INamedTypeSymbol ObjectDisposedException,
        INamedTypeSymbol Helpers)
    {
        public static bool TryCreate(Compilation compilation, Action<Diagnostic> reportDiagnostic, out WellKnownTypes wellKnownTypes)
        {
            var iContainer = compilation.GetTypeOrReport(typeof(IContainer<>), reportDiagnostic);
            var iAsyncContainer = compilation.GetTypeOrReport("StrongInject.IAsyncContainer`1", reportDiagnostic);
            var iInstanceProvider = compilation.GetTypeOrReport(typeof(IInstanceProvider<>), reportDiagnostic);
            var iAsyncInstanceProvider = compilation.GetTypeOrReport(typeof(IAsyncInstanceProvider<>), reportDiagnostic);
            var iFactory = compilation.GetTypeOrReport(typeof(IFactory<>), reportDiagnostic);
            var iAsyncFactory = compilation.GetTypeOrReport(typeof(IAsyncFactory<>), reportDiagnostic);
            var iRequiresInitialization = compilation.GetTypeOrReport(typeof(IRequiresInitialization), reportDiagnostic);
            var iRequiresAsyncInitialization = compilation.GetTypeOrReport(typeof(IRequiresAsyncInitialization), reportDiagnostic);
            var iDisposable = compilation.GetTypeOrReport(typeof(IDisposable), reportDiagnostic);
            var iAsyncDisposable = compilation.GetTypeOrReport("System.IAsyncDisposable", reportDiagnostic);
            var owned = compilation.GetTypeOrReport(typeof(Owned<>), reportDiagnostic);
            var asyncOwned = compilation.GetTypeOrReport("StrongInject.AsyncOwned`1", reportDiagnostic);
            var registerAttribute = compilation.GetTypeOrReport(typeof(RegisterAttribute), reportDiagnostic);
            var registerModuleAttribute = compilation.GetTypeOrReport(typeof(RegisterModuleAttribute), reportDiagnostic);
            var registerFactoryAttribute = compilation.GetTypeOrReport(typeof(RegisterFactoryAttribute), reportDiagnostic);
            var factoryAttribute = compilation.GetTypeOrReport(typeof(FactoryAttribute), reportDiagnostic);
            var instanceAttribute = compilation.GetTypeOrReport(typeof(InstanceAttribute), reportDiagnostic);
            var valueTask1 = compilation.GetTypeOrReport(typeof(ValueTask<>), reportDiagnostic);
            var task1 = compilation.GetTypeOrReport(typeof(Task<>), reportDiagnostic);
            var objectDisposedException = compilation.GetTypeOrReport(typeof(ObjectDisposedException), reportDiagnostic);
            var helpers = compilation.GetTypeOrReport(typeof(Helpers), reportDiagnostic);

            if (iContainer is null
                || iAsyncContainer is null
                || iInstanceProvider is null
                || iAsyncInstanceProvider is null
                || iFactory is null
                || iAsyncFactory is null
                || iRequiresInitialization is null
                || iRequiresAsyncInitialization is null
                || iDisposable is null
                || iAsyncDisposable is null
                || owned is null
                || asyncOwned is null
                || registerAttribute is null
                || registerModuleAttribute is null
                || registerFactoryAttribute is null
                || factoryAttribute is null
                || instanceAttribute is null
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
                IInstanceProvider: iInstanceProvider,
                IAsyncInstanceProvider: iAsyncInstanceProvider,
                IFactory: iFactory,
                IAsyncFactory: iAsyncFactory,
                IRequiresInitialization: iRequiresInitialization,
                IRequiresAsyncInitialization: iRequiresAsyncInitialization,
                IDisposable: iDisposable,
                IAsyncDisposable: iAsyncDisposable,
                Owned: owned,
                AsyncOwned: asyncOwned,
                RegisterAttribute: registerAttribute,
                RegisterModuleAttribute: registerModuleAttribute,
                RegisterFactoryAttribute: registerFactoryAttribute,
                FactoryAttribute: factoryAttribute,
                InstanceAttribute: instanceAttribute,
                ValueTask1: valueTask1,
                Task1: task1,
                ObjectDisposedException: objectDisposedException,
                Helpers: helpers);

            return true;
        }
    }
}