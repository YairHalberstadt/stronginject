using Microsoft.CodeAnalysis;
using System;
using System.Threading.Tasks;

namespace StrongInject.Generator
{
    internal record WellKnownTypes(
        INamedTypeSymbol iContainer,
        INamedTypeSymbol iAsyncContainer,
        INamedTypeSymbol iInstanceProvider,
        INamedTypeSymbol iAsyncInstanceProvider,
        INamedTypeSymbol iRequiresInitialization,
        INamedTypeSymbol iRequiresAsyncInitialization,
        INamedTypeSymbol iDisposable,
        INamedTypeSymbol iAsyncDisposable,
        INamedTypeSymbol registrationAttribute,
        INamedTypeSymbol moduleRegistrationAttribute,
        INamedTypeSymbol factoryRegistrationAttribute,
        INamedTypeSymbol valueTask1,
        INamedTypeSymbol objectDisposedException,
        INamedTypeSymbol helpers)
    {
        public static bool TryCreate(Compilation compilation, Action<Diagnostic> reportDiagnostic, out WellKnownTypes wellKnownTypes)
        {
            var iContainer = compilation.GetTypeOrReport(typeof(IContainer<>), reportDiagnostic);
            var iAsyncContainer = compilation.GetTypeOrReport("StrongInject.IAsyncContainer`1", reportDiagnostic);
            var iInstanceProvider = compilation.GetTypeOrReport(typeof(IInstanceProvider<>), reportDiagnostic);
            var iAsyncInstanceProvider = compilation.GetTypeOrReport(typeof(IAsyncInstanceProvider<>), reportDiagnostic);
            var iRequiresInitialization = compilation.GetTypeOrReport(typeof(IRequiresInitialization), reportDiagnostic);
            var iRequiresAsyncInitialization = compilation.GetTypeOrReport(typeof(IRequiresAsyncInitialization), reportDiagnostic);
            var iDisposable = compilation.GetTypeOrReport(typeof(IDisposable), reportDiagnostic);
            var iAsyncDisposable = compilation.GetTypeOrReport("System.IAsyncDisposable", reportDiagnostic);
            var registrationAttribute = compilation.GetTypeOrReport(typeof(RegistrationAttribute), reportDiagnostic);
            var moduleRegistrationAttribute = compilation.GetTypeOrReport(typeof(ModuleRegistrationAttribute), reportDiagnostic);
            var factoryRegistrationAttribute = compilation.GetTypeOrReport(typeof(FactoryRegistrationAttribute), reportDiagnostic);
            var valueTask1 = compilation.GetTypeOrReport(typeof(ValueTask<>), reportDiagnostic);
            var objectDisposedException = compilation.GetTypeOrReport(typeof(ObjectDisposedException), reportDiagnostic);
            var helpers = compilation.GetTypeOrReport(typeof(Helpers), reportDiagnostic);

            if (iContainer is null
                || iAsyncContainer is null
                || iInstanceProvider is null
                || iAsyncInstanceProvider is null
                || iRequiresInitialization is null
                || iRequiresAsyncInitialization is null
                || iDisposable is null
                || iAsyncDisposable is null
                || registrationAttribute is null
                || moduleRegistrationAttribute is null
                || factoryRegistrationAttribute is null
                || valueTask1 is null
                || objectDisposedException is null
                || helpers is null)
            {
                wellKnownTypes = null!;
                return false;
            }

            wellKnownTypes = new WellKnownTypes(
                iContainer: iContainer,
                iAsyncContainer: iAsyncContainer,
                iInstanceProvider: iInstanceProvider,
                iAsyncInstanceProvider: iAsyncInstanceProvider,
                iRequiresInitialization: iRequiresInitialization,
                iRequiresAsyncInitialization: iRequiresAsyncInitialization,
                iDisposable: iDisposable,
                iAsyncDisposable: iAsyncDisposable,
                registrationAttribute: registrationAttribute,
                moduleRegistrationAttribute: moduleRegistrationAttribute,
                factoryRegistrationAttribute: factoryRegistrationAttribute,
                valueTask1: valueTask1,
                objectDisposedException: objectDisposedException,
                helpers: helpers);

            return true;
        }
    }
}