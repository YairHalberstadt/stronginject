using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;
using StrongInject.Runtime;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace StrongInject.Generator
{
    [Generator]
    public class ContainerSourceGenerator : ISourceGenerator
    {
        public void Execute(SourceGeneratorContext context)
        {
            var cancellationToken = context.CancellationToken;
            var registrationAttribute = context.Compilation.GetTypeOrReport(typeof(RegistrationAttribute), context.ReportDiagnostic);
            var moduleRegistrationAttribute = context.Compilation.GetTypeOrReport(typeof(ModuleRegistrationAttribute), context.ReportDiagnostic);
            var iRequiresInitializationType = context.Compilation.GetTypeOrReport(typeof(IRequiresInitialization), context.ReportDiagnostic);
            var valueTask1Type = context.Compilation.GetTypeOrReport(typeof(ValueTask<>), context.ReportDiagnostic);
            var interlockedType = context.Compilation.GetTypeOrReport(typeof(Interlocked), context.ReportDiagnostic);
            var helpersType = context.Compilation.GetTypeOrReport(typeof(Helpers), context.ReportDiagnostic);
            var iAsyncDisposableType = context.Compilation.GetTypeOrReport(typeof(IAsyncDisposable), context.ReportDiagnostic);
            var iDisposableType = context.Compilation.GetTypeOrReport(typeof(IDisposable), context.ReportDiagnostic);

            if (registrationAttribute is null 
                || moduleRegistrationAttribute is null 
                || iRequiresInitializationType is null 
                || valueTask1Type is null 
                || interlockedType is null
                || helpersType is null
                || iAsyncDisposableType is null
                || iDisposableType is null)
            {
                return;
            }

            var registrationCalculator = new RegistrationCalculator(context.Compilation, context.ReportDiagnostic, cancellationToken);
            var containerInterface = context.Compilation.GetType(typeof(IContainer<>));
            var instanceProviderInterface = context.Compilation.GetType(typeof(IInstanceProvider<>));

            foreach (var syntaxTree in context.Compilation.SyntaxTrees)
            {
                cancellationToken.ThrowIfCancellationRequested();

                var semanticModel = context.Compilation.GetSemanticModel(syntaxTree);
                var modules = syntaxTree.GetRoot(cancellationToken).DescendantNodesAndSelf().OfType<ClassDeclarationSyntax>()
                    .Select(x => semanticModel.GetDeclaredSymbol(x, cancellationToken))
                    .OfType<INamedTypeSymbol>()
                    .Where(x =>
                        x.GetAttributes().Any(x =>
                            x.AttributeClass is { } attribute && attribute.Equals(registrationAttribute, SymbolEqualityComparer.Default)
                            && attribute.Equals(moduleRegistrationAttribute, SymbolEqualityComparer.Default))
                        || x.AllInterfaces.Any(x => x.OriginalDefinition.Equals(containerInterface, SymbolEqualityComparer.Default)));

                foreach (var module in modules)
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    // do this even if not a container to report diagnostics
                    var registrations = registrationCalculator.GetRegistrations(module);

                    GenerateResolveMethods(module, registrations);
                };
            }

            void GenerateResolveMethods(INamedTypeSymbol module, IReadOnlyDictionary<ITypeSymbol, InstanceSource> registrations)
            {
                Dictionary<ITypeSymbol, InstanceSource>? instanceSources = null;
                Dictionary<InstanceSource, string>? singleInstanceMethodNames = null;
                StringBuilder? containerMembersSource = null;
                foreach (var constructedContainerInterface in module.AllInterfaces.Where(x => x.OriginalDefinition.Equals(containerInterface, SymbolEqualityComparer.Default)))
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    instanceSources ??= GatherInstanceSources(instanceProviderInterface, module, registrations, context.ReportDiagnostic, cancellationToken);

                    var target = constructedContainerInterface.TypeArguments[0];

                    if (DependencyChecker.HasCircularOrMissingDependencies(
                            target,
                            instanceSources,
                            context.ReportDiagnostic,
                            // Ideally we would use the location of the interface in the base list, however getting that location is complex and not critical for now.
                            // See http://sourceroslyn.io/#Microsoft.CodeAnalysis.CSharp/Symbols/Source/SourceMemberContainerSymbol_ImplementationChecks.cs,333
                            ((ClassDeclarationSyntax)module.DeclaringSyntaxReferences[0].GetSyntax()).Identifier.GetLocation()))
                    {
                        // error reported. Move on to next containerInterface
                        continue;
                    }

                    var methodSource = new StringBuilder();
                    var methodSymbol = (IMethodSymbol)constructedContainerInterface.GetMembers()[0];
                    methodSource.Append("async ");
                    methodSource.Append(methodSymbol.ReturnType.FullName());
                    methodSource.Append(" ");
                    methodSource.Append(constructedContainerInterface.FullName());
                    methodSource.Append(".");
                    methodSource.Append(methodSymbol.Name);
                    methodSource.Append("<");
                    for (int i = 0; i < methodSymbol.TypeParameters.Length; i++)
                    {
                        var typeParam = methodSymbol.TypeParameters[i];
                        if (i != 0)
                            methodSource.Append(",");
                        methodSource.Append(typeParam.Name);
                    }
                    methodSource.Append(">(");
                    for (int i = 0; i < methodSymbol.Parameters.Length; i++)
                    {
                        var param = methodSymbol.Parameters[i];
                        if (i != 0)
                            methodSource.Append(",");
                        methodSource.Append(param.Type.FullName());
                        methodSource.Append(" ");
                        methodSource.Append(param.Name);
                    }
                    methodSource.Append("){");
                    var resultVariableName = CreateVariable(instanceSources[target], methodSource, isSingleInstanceCreation: false, out var orderOfCreation);
                    methodSource.Append("var result = await func((");
                    methodSource.Append(target.FullName());
                    methodSource.Append(")");
                    methodSource.Append(resultVariableName);
                    methodSource.Append(", param);");

                    for(int i = orderOfCreation.Count - 1; i >= 0; i--)
                    {
                        var (variableName, source) = orderOfCreation[i];
                        switch (source)
                        {
                            case FactoryRegistration { lifetime: Lifetime.InstancePerDependency }:
                                methodSource.Append("await ");
                                methodSource.Append(helpersType.FullName());
                                methodSource.Append("." + nameof(Helpers.DisposeAsync) + "(");
                                methodSource.Append(variableName);
                                methodSource.Append(");");
                                break;
                            case InstanceProvider { instanceProviderField: var field, castTo: var cast }:
                                methodSource.Append("await ((");
                                methodSource.Append(cast.FullName());
                                methodSource.Append(")this.");
                                methodSource.Append(field.Name);
                                methodSource.Append(")." + nameof(IInstanceProvider<object>.ReleaseAsync) + "(");
                                methodSource.Append(variableName);
                                methodSource.Append(");");
                                break;
                            case Registration { type: var type, lifetime: Lifetime.InstancePerDependency }:
                                if (type.AllInterfaces.Contains(iAsyncDisposableType))
                                {
                                    methodSource.Append("await ((");
                                    methodSource.Append(iAsyncDisposableType.FullName());
                                    methodSource.Append(")");
                                    methodSource.Append(variableName);
                                    methodSource.Append(")." + nameof(IAsyncDisposable.DisposeAsync) + "(");
                                    methodSource.Append(");");
                                }
                                else if (type.AllInterfaces.Contains(iDisposableType))
                                {
                                    methodSource.Append("((");
                                    methodSource.Append(iDisposableType.FullName());
                                    methodSource.Append(")");
                                    methodSource.Append(variableName);
                                    methodSource.Append(")." + nameof(IDisposable.Dispose) + "(");
                                    methodSource.Append(");");
                                }
                                break;
                        }
                    }
                    methodSource.Append("return result;}");

                    (containerMembersSource ??= new()).Append(methodSource);
                }

                if (containerMembersSource is not null)
                {
                    var file = new StringBuilder("#pragma warning disable CS1998\n");
                    bool requiresNamespace = module.ContainingNamespace is { IsGlobalNamespace: false };
                    if (requiresNamespace)
                    {
                        file.Append("namespace ");
                        file.Append(module.ContainingNamespace.FullName());
                        file.Append("{");
                    }
                    file.Append("partial class ");
                    file.Append(module.Name);
                    file.Append("{");
                    file.Append(containerMembersSource);
                    file.Append("}");
                    if (requiresNamespace)
                    {
                        file.Append("}");
                    }

                    context.AddSource(
                        module.Name + ".generated.cs",
                        CSharpSyntaxTree.ParseText(SourceText.From(file.ToString())).GetRoot().NormalizeWhitespace().SyntaxTree.GetText());
                }

                string CreateVariable(InstanceSource target, StringBuilder methodSource, bool isSingleInstanceCreation, out List<(string variableName, InstanceSource source)> orderOfCreation)
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    orderOfCreation = new();
                    var orderOfCreationTemp = orderOfCreation;
                    var variables = new Dictionary<InstanceSource, string>(InstanceSourceComparer.Instance);
                    var outerTarget = target;
                    return CreateVariableInternal(target);
                    string CreateVariableInternal(InstanceSource target)
                    {
                        if (!variables.TryGetValue(target, out var variableName))
                        {
                            variableName = "_" + variables.Count;
                            variables.Add(target, variableName);
                            switch (target)
                            {
                                case InstanceProvider(_, var field, var castTo) instanceProvider:
                                    methodSource.Append("var ");
                                    methodSource.Append(variableName);
                                    methodSource.Append("=await((");
                                    methodSource.Append(castTo.FullName());
                                    methodSource.Append(")this.");
                                    methodSource.Append(field.Name);
                                    methodSource.Append(")." + nameof(IInstanceProvider<object>.GetAsync) + "();");
                                    break;
                                case { lifetime: Lifetime.SingleInstance } registration
                                    when !(isSingleInstanceCreation && ReferenceEquals(outerTarget, target)):
                                    methodSource.Append("var ");
                                    methodSource.Append(variableName);
                                    methodSource.Append("=await ");
                                    methodSource.Append(GetSingleInstanceMethodName(registration));
                                    methodSource.Append("();");
                                    break;
                                case FactoryRegistration(var factoryType, var factoryOf, var lifetime) registration:
                                    var factory = CreateVariableInternal(instanceSources[factoryType]);
                                    methodSource.Append("var ");
                                    methodSource.Append(variableName);
                                    methodSource.Append("=await((");
                                    methodSource.Append(factoryType.FullName());
                                    methodSource.Append(")");
                                    methodSource.Append(factory);
                                    methodSource.Append(")." + nameof(IFactory<object>.CreateAsync) + "();");
                                    break;
                                case Registration(var type, _, var lifetime, var requiresAsyncInitialization, var constructor) registration:
                                    var variableSource = new StringBuilder();
                                    variableSource.Append("var ");
                                    variableSource.Append(variableName);
                                    variableSource.Append("=new ");
                                    variableSource.Append(type.FullName());
                                    variableSource.Append("(");
                                    for (int i = 0; i < constructor.Parameters.Length; i++)
                                    {
                                        if (i != 0)
                                        {
                                            variableSource.Append(",");
                                        }
                                        IParameterSymbol? parameter = constructor.Parameters[i];
                                        var source = instanceSources[parameter.Type];
                                        var variable = CreateVariableInternal(source);
                                        if (source is InstanceProvider or FactoryRegistration)
                                        {
                                            variableSource.Append(variable);
                                        }
                                        else if (source is Registration { registeredAs: var castTarget })
                                        {
                                            variableSource.Append("(");
                                            variableSource.Append(castTarget.FullName());
                                            variableSource.Append(")");
                                            variableSource.Append(variable);
                                        }
                                    }
                                    variableSource.Append(");");
                                    methodSource.Append(variableSource);

                                    if (requiresAsyncInitialization)
                                    {
                                        methodSource.Append("await ((");
                                        methodSource.Append(iRequiresInitializationType.FullName());
                                        methodSource.Append(")");
                                        methodSource.Append(variableName);
                                        methodSource.Append(")." + nameof(IRequiresInitialization.InitializeAsync) + "();");
                                    }

                                    break;
                            }

                        }
                        orderOfCreationTemp.Add((variableName, target));
                        return variableName;
                    }

                    string GetSingleInstanceMethodName(InstanceSource registration)
                    {
                        singleInstanceMethodNames ??= new(InstanceSourceComparer.Instance);
                        if (!singleInstanceMethodNames.TryGetValue(registration, out var singleInstanceMethodName))
                        {
                            var type = registration switch
                            {
                                Registration { type: var t } => t,
                                FactoryRegistration { factoryOf: var t } => t,
                                _ => throw new InvalidOperationException(),
                            };

                            var singleInstanceFieldName = "_singleInstanceField" + singleInstanceMethodNames.Count;
                            singleInstanceMethodName = "GetSingleInstanceField" + singleInstanceMethodNames.Count;
                            singleInstanceMethodNames.Add(registration, singleInstanceMethodName);
                            (containerMembersSource ??= new()).Append("private " + type.FullName() + " " + singleInstanceFieldName + ";");

                            var methodSource = new StringBuilder();
                            methodSource.Append("private async ");
                            methodSource.Append(valueTask1Type.Construct(type));
                            methodSource.Append(" ");
                            methodSource.Append(singleInstanceMethodName);
                            methodSource.Append("(){");
                            methodSource.Append("if (!object." + nameof(ReferenceEquals) + "(");
                            methodSource.Append(singleInstanceFieldName);
                            methodSource.Append(",null");
                            methodSource.Append("))");
                            methodSource.Append("return ");
                            methodSource.Append(singleInstanceFieldName);
                            methodSource.Append(";");
                            var variableName = CreateVariable(registration, methodSource, isSingleInstanceCreation: true, out _);

                            methodSource.Append(interlockedType.FullName());
                            methodSource.Append("." + nameof(Interlocked.CompareExchange));
                            methodSource.Append("(ref ");
                            methodSource.Append(singleInstanceFieldName);
                            methodSource.Append(",");
                            methodSource.Append(variableName);
                            methodSource.Append(",");
                            methodSource.Append("null");
                            methodSource.Append(");");
                            methodSource.Append("return ");
                            methodSource.Append(singleInstanceFieldName);
                            methodSource.Append(";}");
                            containerMembersSource.Append(methodSource);
                        }
                        return singleInstanceMethodName;
                    }
                }
            }
        }

        private static Dictionary<ITypeSymbol, InstanceSource> GatherInstanceSources(INamedTypeSymbol? instanceProviderInterface, INamedTypeSymbol module, IReadOnlyDictionary<ITypeSymbol, InstanceSource> registrations, Action<Diagnostic> reportDiagnostic, CancellationToken cancellationToken)
        {
            var instanceSources = registrations.ToDictionary(x => x.Key, x => x.Value);
            var instanceProviders = new Dictionary<ITypeSymbol, InstanceProvider>();
            foreach (var instanceProviderField in module.GetMembers()
                .OfType<IFieldSymbol>()
                .Where(x =>
                    !x.IsStatic
                    && x.Type.AllInterfacesAndSelf().Any(x => x.OriginalDefinition.Equals(instanceProviderInterface, SymbolEqualityComparer.Default))))
            {
                foreach (var constructedInstanceProviderInterface in instanceProviderField.Type.AllInterfacesAndSelf().Where(x => x.OriginalDefinition.Equals(instanceProviderInterface, SymbolEqualityComparer.Default)))
                {
                    var providedType = constructedInstanceProviderInterface.TypeArguments[0];
                    if (instanceProviders.TryGetValue(providedType, out var existing))
                    {
                        var exisingField = existing.instanceProviderField;
                        reportDiagnostic(DuplicateInstanceProviders(existing.instanceProviderField, existing.instanceProviderField, instanceProviderField, providedType, cancellationToken));
                        reportDiagnostic(DuplicateInstanceProviders(instanceProviderField, existing.instanceProviderField, instanceProviderField, providedType, cancellationToken));
                        continue;
                    }
                    var instanceProvider = new InstanceProvider(providedType, instanceProviderField, constructedInstanceProviderInterface);
                    instanceProviders[providedType] = instanceProvider;
                    instanceSources[providedType] = instanceProvider;
                }
            }
            return instanceSources;
        }

        private static Diagnostic DuplicateInstanceProviders(IFieldSymbol fieldForLocation, IFieldSymbol firstField, IFieldSymbol secondField, ITypeSymbol providedType, CancellationToken cancellationToken)
        {
            return Diagnostic.Create(
                new DiagnosticDescriptor(
                    "SI0301",
                    "Duplicate instance providers for type",
                    "Both fields '{0}' and '{1}' are instance providers for '{2}'",
                    "StrongInject",
                    DiagnosticSeverity.Error,
                    isEnabledByDefault: true),
                fieldForLocation.DeclaringSyntaxReferences.FirstOrDefault()?.GetSyntax(cancellationToken).GetLocation() ?? Location.None,
                firstField,
                secondField,
                providedType);
        }

        public void Initialize(InitializationContext context)
        {
        }

        private class InstanceSourceComparer : IEqualityComparer<InstanceSource>
        {
            private InstanceSourceComparer() { }

            public static InstanceSourceComparer Instance { get; } = new InstanceSourceComparer();

            public bool Equals(InstanceSource x, InstanceSource y)
            {
                return (x, y) switch
                {
                    (null, null) => true,
                    (Registration rX, Registration rY) => rX.lifetime == rY.lifetime && rX.type.Equals(rY.type, SymbolEqualityComparer.Default),
                    (FactoryRegistration fX, FactoryRegistration fY) => fX.lifetime == fY.lifetime && fX.factoryType.Equals(fY.factoryType, SymbolEqualityComparer.Default),
                    (InstanceProvider iX, InstanceProvider iY) => iX.providedType.Equals(iY.providedType, SymbolEqualityComparer.Default),
                    _ => false,
                };
            }

            public int GetHashCode(InstanceSource obj)
            {
                return obj switch
                {
                    null => 0,
                    Registration r => HashCode.Combine(r.lifetime, r.type),
                    InstanceProvider i => HashCode.Combine(i.instanceProviderField),
                    FactoryRegistration f => HashCode.Combine(f.lifetime, f.factoryType),
                    _ => throw new SwitchExpressionException(obj),
                };
            }
        }
    }
}
