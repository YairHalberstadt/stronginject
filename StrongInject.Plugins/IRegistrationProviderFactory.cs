using Microsoft.CodeAnalysis;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Text;

namespace StrongInject.Plugins
{
    public static class PluginCollection
    {
        private static List<IRegistrationProviderFactory> _registrationProviderFactories = new();

        public static IRegistrationProviderFactory[] RegistrationProviderFactories =>
            _registrationProviderFactories.ToArray();

        public static void Register(IRegistrationProviderFactory registrationProviderFactory) =>
            _registrationProviderFactories.Add(registrationProviderFactory);
    }
    
    public interface IRegistrationProviderFactory
    {
        IRegistrationProvider? Create(GeneratorExecutionContext context);
        
        void Initialize(GeneratorInitializationContext context);
    }

    public interface IRegistrationProvider
    {
        IEnumerable<Registration> GetRegistrationsForModule(ModuleContext context);

        Registration? GetLastDitchRegistrationForType(INamedTypeSymbol type);

        void Complete();
    }

    public record Registration(Location DiagnosticLocation)
    {
        public record MethodRegistration(IMethodSymbol Method, Scope Scope, Options Options, Location DiagnosticLocation) : Registration(DiagnosticLocation);
        public record DecoratorRegistration(IMethodSymbol Method, Scope Scope, Options Options, Location DiagnosticLocation) : Registration(DiagnosticLocation);

        public record CustomRegistration(
            ITypeSymbol GeneratedExpressionResultType,
            ImmutableArray<Dependency> Dependencies,
            ImmutableArray<ITypeParameterSymbol> TypeParameters,
            CustomRegistration.GenerateInstantiationExpression CallBack,
            Scope Scope,
            Options Options,
            Location DiagnosticLocation) : Registration(DiagnosticLocation)
        {
            public delegate void GenerateInstantiationExpression(StringBuilder stringBuilder, ImmutableArray<string?> dependencies, ImmutableArray<ITypeSymbol> typeArguments);
        }
    }

    public record Dependency(ITypeSymbol Type, bool IsOptional);

    public record ModuleContext(INamedTypeSymbol Module, ModuleContext.RegistrationsToInclude ToInclude)
    {
        public enum RegistrationsToInclude
        {
            ExportedOnly,
            ExportedAndInherited,
            All,
        }
    }
}