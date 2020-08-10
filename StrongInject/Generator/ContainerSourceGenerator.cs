using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;
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
    internal class ContainerSourceGenerator : ISourceGenerator
    {
        public void Execute(SourceGeneratorContext context)
        {
            try
            {
                ExecuteInternal(context);
            }
            catch (Exception e)
            {
                //This is temporary till https://github.com/dotnet/roslyn/issues/46084 is fixed
                context.ReportDiagnostic(Diagnostic.Create(
                    new DiagnosticDescriptor(
                        "SI0000",
                        "An exception was thrown by the StrongInject generator",
                        "An exception was thrown by the StrongInject generator: '{0}'",
                        "StrongInject",
                        DiagnosticSeverity.Error,
                        isEnabledByDefault: true),
                    Location.None,
                    e.ToString()));
            }
        }

        //By not inlining we make sure we can catch assembly loading errors when jitting this method
        [MethodImpl(MethodImplOptions.NoInlining)]
        private void ExecuteInternal(SourceGeneratorContext context)
        {
            var cancellationToken = context.CancellationToken;
            var registrationAttribute = context.Compilation.GetTypeOrReport(typeof(RegistrationAttribute), context.ReportDiagnostic);
            var moduleRegistrationAttribute = context.Compilation.GetTypeOrReport(typeof(ModuleRegistrationAttribute), context.ReportDiagnostic);
            var iRequiresInitializationType = context.Compilation.GetTypeOrReport(typeof(IRequiresInitialization), context.ReportDiagnostic);
            var iRequiresAsyncInitializationType = context.Compilation.GetTypeOrReport(typeof(IRequiresAsyncInitialization), context.ReportDiagnostic);
            var valueTask1Type = context.Compilation.GetTypeOrReport(typeof(ValueTask<>), context.ReportDiagnostic);
            var interlockedType = context.Compilation.GetTypeOrReport(typeof(Interlocked), context.ReportDiagnostic);
            var helpersType = context.Compilation.GetTypeOrReport(typeof(Helpers), context.ReportDiagnostic);
            var iAsyncDisposableType = context.Compilation.GetTypeOrReport("System.IAsyncDisposable", context.ReportDiagnostic);
            var iDisposableType = context.Compilation.GetTypeOrReport(typeof(IDisposable), context.ReportDiagnostic);
            var objectDisposedExceptionType = context.Compilation.GetTypeOrReport(typeof(ObjectDisposedException), context.ReportDiagnostic);

            if (registrationAttribute is null
                || moduleRegistrationAttribute is null
                || iRequiresInitializationType is null
                || iRequiresAsyncInitializationType is null
                || valueTask1Type is null
                || interlockedType is null
                || helpersType is null
                || iAsyncDisposableType is null
                || iDisposableType is null
                || objectDisposedExceptionType is null)
            {
                return;
            }

            var registrationCalculator = new RegistrationCalculator(context.Compilation, context.ReportDiagnostic, cancellationToken);
            var containerInterface = context.Compilation.GetType(typeof(IContainer<>));
            var asyncContainerInterface = context.Compilation.GetTypeByMetadataName("StrongInject.IAsyncContainer`1");
            var instanceProviderInterface = context.Compilation.GetType(typeof(IInstanceProvider<>));
            var asyncInstanceProviderInterface = context.Compilation.GetType(typeof(IAsyncInstanceProvider<>));

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
                        || x.AllInterfaces.Any(x
                            => x.OriginalDefinition.Equals(containerInterface, SymbolEqualityComparer.Default)
                            || x.OriginalDefinition.Equals(asyncContainerInterface, SymbolEqualityComparer.Default)));

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
                InstanceSourcesScope? containerScope = null;
                Dictionary<InstanceSource, (string name, bool isAsync, string disposeFieldName, string lockName)>? singleInstanceMethods = null;
                StringBuilder? containerMembersSource = null;

                var containerInterfaces = module.AllInterfaces
                    .Where(x
                        => x.OriginalDefinition.Equals(containerInterface, SymbolEqualityComparer.Default)
                        || x.OriginalDefinition.Equals(asyncContainerInterface, SymbolEqualityComparer.Default))
                    .Select(x => (containerInterface: x, isAsync: x.OriginalDefinition.Equals(asyncContainerInterface, SymbolEqualityComparer.Default)))
                    .ToList();

                var implementsSyncContainer = false;
                var implementsAsyncContainer = false;
                foreach (var (_, isAsync) in containerInterfaces)
                {
                    (isAsync ? ref implementsAsyncContainer : ref implementsSyncContainer) = true;
                }

                foreach (var (constructedContainerInterface, isAsync) in containerInterfaces)
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    containerScope ??= CreateContainerScope(instanceProviderInterface, asyncInstanceProviderInterface, module, registrations, context.Compilation, context.ReportDiagnostic, cancellationToken);

                    var target = constructedContainerInterface.TypeArguments[0];

                    var methodSource = new StringBuilder();
                    var methodSymbol = (IMethodSymbol)constructedContainerInterface.GetMembers()[0];
                    if (isAsync)
                    {
                        methodSource.Append("async ");
                    }
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

                    if (DependencyChecker.HasCircularOrMissingDependencies(
                            target,
                            isAsync,
                            containerScope,
                            context.ReportDiagnostic,
                            // Ideally we would use the location of the interface in the base list, however getting that location is complex and not critical for now.
                            // See http://sourceroslyn.io/#Microsoft.CodeAnalysis.CSharp/Symbols/Source/SourceMemberContainerSymbol_ImplementationChecks.cs,333
                            ((ClassDeclarationSyntax)module.DeclaringSyntaxReferences[0].GetSyntax()).Identifier.GetLocation()))
                    {
                        // error reported. Implement with throwing implementation to remove NotImplemented error CS0535
                        methodSource.Append("throw new global::System.NotImplementedException();}");
                    }
                    else
                    {
                        methodSource.Append("if(Disposed)");
                        ThrowObjectDisposedException(methodSource);
                        var resultVariableName = CreateVariable(containerScope[target], methodSource, containerScope, isSingleInstanceCreation: false, out var orderOfCreation, isAsync);
                        methodSource.Append(methodSymbol.TypeParameters[0].Name);
                        methodSource.Append(" result;try{result=");
                        if (isAsync)
                        {
                            methodSource.Append("await func((");
                        }
                        else
                        {
                            methodSource.Append("func((");
                        }
                        methodSource.Append(target.FullName());
                        methodSource.Append(")");
                        methodSource.Append(resultVariableName);
                        methodSource.Append(", param);}finally{");
                        GenerateDisposeCode(methodSource, orderOfCreation, isAsync, singleInstanceTarget: null);
                        methodSource.Append("}return result;}");
                    }

                    (containerMembersSource ??= new()).Append(methodSource);

                    void GenerateDisposeCode(StringBuilder methodSource, List<(string variableName, string? disposeActionsName, InstanceSource source)> orderOfCreation, bool isAsync, InstanceSource? singleInstanceTarget)
                    {
                        for (int i = orderOfCreation.Count - 1; i >= 0; i--)
                        {
                            var (variableName, disposeActionName, source) = orderOfCreation[i];
                            switch (source)
                            {
                                case { scope: Scope.SingleInstance } when !source.Equals(singleInstanceTarget):
                                    break;
                                case FactoryRegistration:
                                    if (isAsync)
                                    {
                                        methodSource.Append("await ");
                                    }
                                    methodSource.Append(helpersType.FullName());
                                    methodSource.Append(".");
                                    methodSource.Append(isAsync
                                        ? nameof(Helpers.DisposeAsync)
                                        : nameof(Helpers.Dispose));
                                    methodSource.Append("(");
                                    methodSource.Append(variableName);
                                    methodSource.Append(");");
                                    break;
                                case InstanceProvider { instanceProviderField: var field, castTo: var cast, isAsync: var isAsyncInstanceProvider }:
                                    if (isAsyncInstanceProvider)
                                    {
                                        methodSource.Append("await ");
                                    }
                                    methodSource.Append("((");
                                    methodSource.Append(cast.FullName());
                                    methodSource.Append(")this.");
                                    methodSource.Append(field.Name);
                                    methodSource.Append(").");
                                    methodSource.Append(isAsyncInstanceProvider
                                        ? nameof(IAsyncInstanceProvider<object>.ReleaseAsync)
                                        : nameof(IInstanceProvider<object>.Release));
                                    methodSource.Append("(");
                                    methodSource.Append(variableName);
                                    methodSource.Append(");");
                                    break;
                                case Registration { type: var type }:
                                    var isAsyncDisposable = type.AllInterfaces.Contains(iAsyncDisposableType);
                                    if (isAsync && isAsyncDisposable)
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
                                    else if (isAsyncDisposable)
                                    {
                                        context.ReportDiagnostic(WarnIAsyncDisposableInSynchronousResolution(
                                            type,
                                            constructedContainerInterface,
                                            module,
                                            cancellationToken));
                                    }
                                    break;
                                case DelegateParameter:
                                    break;
                                case DelegateSource:
                                    methodSource.Append("foreach (var disposeAction in ");
                                    methodSource.Append(disposeActionName);
                                    methodSource.Append(isAsync ? ")await disposeAction();" : ")disposeAction();");
                                    break;
                            }
                        }
                    }

                    string CreateVariable(
                        InstanceSource target,
                        StringBuilder methodSource,
                        InstanceSourcesScope instanceSourcesScope,
                        bool isSingleInstanceCreation,
                        out List<(string variableName, string? disposeActionsName, InstanceSource source)> orderOfCreation,
                        bool disposeAsynchronously)
                    {
                        cancellationToken.ThrowIfCancellationRequested();
                        orderOfCreation = new();
                        var orderOfCreationTemp = orderOfCreation;
                        var variables = new Dictionary<InstanceSource, string>(InstanceSourceComparer.Instance);
                        var outerTarget = target;
                        return CreateVariableInternal(target, instanceSourcesScope);
                        string CreateVariableInternal(InstanceSource target, InstanceSourcesScope instanceSourcesScope)
                        {
                            instanceSourcesScope = instanceSourcesScope.Enter(target);
                            if (target is DelegateParameter { name: var variableName })
                                return variableName;
                            string? disposeActionsName = null;
                            if (target.scope == Scope.InstancePerDependency || !variables.TryGetValue(target, out variableName))
                            {
                                variableName = "_" + variables.Count;
                                variables.Add(target, variableName);
                                switch (target)
                                {
                                    case InstanceProvider(_, var field, var castTo, var isAsync) instanceProvider:
                                        methodSource.Append("var ");
                                        methodSource.Append(variableName);
                                        methodSource.Append(isAsync ? "=await((" : "=((");
                                        methodSource.Append(castTo.FullName());
                                        methodSource.Append(")this.");
                                        methodSource.Append(field.Name);
                                        methodSource.Append(").");
                                        methodSource.Append(isAsync
                                            ? nameof(IAsyncInstanceProvider<object>.GetAsync)
                                            : nameof(IInstanceProvider<object>.Get));
                                        methodSource.Append("();");
                                        break;
                                    case { scope: Scope.SingleInstance } registration
                                        when !(isSingleInstanceCreation && ReferenceEquals(outerTarget, target)):
                                        {
                                            var (name, isAsync) = GetSingleInstanceMethod(registration, instanceSourcesScope);
                                            methodSource.Append("var ");
                                            methodSource.Append(variableName);
                                            methodSource.Append(isAsync ? "=await " : "=");
                                            methodSource.Append(name);
                                            methodSource.Append("();");
                                        }
                                        break;
                                    case FactoryRegistration(var factoryType, var factoryOf, var scope, var isAsync) registration:
                                        var factory = CreateVariableInternal(containerScope[factoryType], instanceSourcesScope);
                                        methodSource.Append("var ");
                                        methodSource.Append(variableName);
                                        methodSource.Append(isAsync ? "=await((" : "=((");
                                        methodSource.Append(factoryType.FullName());
                                        methodSource.Append(")");
                                        methodSource.Append(factory);
                                        methodSource.Append(").");
                                        methodSource.Append(isAsync
                                            ? nameof(IAsyncFactory<object>.CreateAsync)
                                            : nameof(IFactory<object>.Create));
                                        methodSource.Append("();");
                                        break;
                                    case Registration(var type, _, var scope, var requiresInitialization, var constructor, var isAsync) registration:
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
                                            var source = instanceSourcesScope[parameter.Type];
                                            var variable = CreateVariableInternal(source, instanceSourcesScope);
                                            if (source is Registration { registeredAs: var castTarget })
                                            {
                                                variableSource.Append("(");
                                                variableSource.Append(castTarget.FullName());
                                                variableSource.Append(")");
                                                variableSource.Append(variable);
                                            }
                                            else
                                            {
                                                variableSource.Append(variable);
                                            }
                                        }
                                        variableSource.Append(");");
                                        methodSource.Append(variableSource);

                                        if (requiresInitialization)
                                        {
                                            methodSource.Append(isAsync ? "await ((" : "((");
                                            methodSource.Append(isAsync
                                                ? iRequiresAsyncInitializationType.FullName()
                                                : iRequiresInitializationType.FullName());
                                            methodSource.Append(")");
                                            methodSource.Append(variableName);
                                            methodSource.Append(").");
                                            methodSource.Append(isAsync
                                                ? nameof(IRequiresAsyncInitialization.InitializeAsync)
                                                : nameof(IRequiresInitialization.Initialize));
                                            methodSource.Append("();");
                                        }

                                        break;
                                    case DelegateSource(var delegateType, var returnType, var parameters, var isAsync):
                                        {
                                            disposeActionsName = "disposeActions" + instanceSourcesScope.Depth + variableName;
                                            methodSource.Append("var ");
                                            methodSource.Append(disposeActionsName);
                                            methodSource.Append(disposeAsynchronously
                                                ? "=new global::System.Collections.Concurrent.ConcurrentBag<global::System.Func<global::System.Threading.Tasks.ValueTask>>();"
                                                : "=new global::System.Collections.Concurrent.ConcurrentBag<global::System.Action>();");
                                            methodSource.Append(delegateType.FullName());
                                            methodSource.Append(" ");
                                            methodSource.Append(variableName);
                                            methodSource.Append(isAsync ? "=async(" : "=(");
                                            foreach (var (parameter, index) in parameters.WithIndex())
                                            {
                                                if (index != 0)
                                                    methodSource.Append(",");
                                                methodSource.Append(((DelegateParameter)instanceSourcesScope[parameter.Type]).name);
                                            }
                                            methodSource.Append(")=>{");
                                            var variable = CreateVariable(instanceSourcesScope[returnType], methodSource, instanceSourcesScope, isSingleInstanceCreation: false, out var delegateOrderOfCreation, disposeAsynchronously);
                                            methodSource.Append(disposeActionsName);
                                            methodSource.Append(".Add(");
                                            if (disposeAsynchronously)
                                                methodSource.Append("async");
                                            methodSource.Append("() => {");
                                            GenerateDisposeCode(methodSource, delegateOrderOfCreation, disposeAsynchronously, null);
                                            methodSource.Append("});return ");
                                            methodSource.Append(variable);
                                            methodSource.Append(";};");
                                            break;
                                        }
                                }

                            }
                            orderOfCreationTemp.Add((variableName, disposeActionsName, target));
                            return variableName;
                        }

                        (string name, bool isAsync) GetSingleInstanceMethod(InstanceSource instanceSource, InstanceSourcesScope instanceSourcesScope)
                        {
                            singleInstanceMethods ??= new(InstanceSourceComparer.Instance);
                            if (!singleInstanceMethods.TryGetValue(instanceSource, out var singleInstanceMethod))
                            {
                                var type = instanceSource switch
                                {
                                    Registration { type: var t } => t,
                                    FactoryRegistration { factoryOf: var t } => t,
                                    _ => throw new InvalidOperationException(),
                                };

                                var index = singleInstanceMethods.Count;
                                var singleInstanceFieldName = "_singleInstanceField" + index;
                                var name = "GetSingleInstanceField" + index;
                                var isAsync = DependencyChecker.RequiresAsync(instanceSource, containerScope);
                                var disposeFieldName = "_disposeAction" + index;
                                var lockName = "_lock" + index;
                                singleInstanceMethod = (name, isAsync, disposeFieldName, lockName);
                                singleInstanceMethods.Add(instanceSource, singleInstanceMethod);
                                (containerMembersSource ??= new()).Append("private " + type.FullName() + " " + singleInstanceFieldName + ";");
                                containerMembersSource.Append("private global::System.Threading.SemaphoreSlim _lock" + index + "=new global::System.Threading.SemaphoreSlim(1);");
                                containerMembersSource.Append((implementsAsyncContainer 
                                    ? "private global::System.Func<global::System.Threading.Tasks.ValueTask>"
                                    : "private global::System.Action") + " _disposeAction" + index + ";");
                                var methodSource = new StringBuilder();
                                methodSource.Append(isAsync ? "private async " : "private ");
                                methodSource.Append(isAsync ? valueTask1Type.Construct(type).FullName() : type.FullName());
                                methodSource.Append(" ");
                                methodSource.Append(name);
                                methodSource.Append("(){");
                                methodSource.Append("if (!object." + nameof(ReferenceEquals) + "(");
                                methodSource.Append(singleInstanceFieldName);
                                methodSource.Append(",null");
                                methodSource.Append("))");
                                methodSource.Append("return ");
                                methodSource.Append(singleInstanceFieldName);
                                methodSource.Append(";");
                                if (isAsync)
                                    methodSource.Append("await ");
                                methodSource.Append("this.");
                                methodSource.Append(lockName);
                                methodSource.Append(isAsync ? ".WaitAsync();" : ".Wait();");
                                methodSource.Append("try{if(this.Disposed)");
                                ThrowObjectDisposedException(methodSource);
                                var variableName = CreateVariable(instanceSource, methodSource, containerScope, isSingleInstanceCreation: true, out var orderOfCreation, implementsAsyncContainer);
                                methodSource.Append("this.");
                                methodSource.Append(singleInstanceFieldName);
                                methodSource.Append("=");
                                methodSource.Append(variableName);
                                methodSource.Append(";");
                                methodSource.Append("this.");
                                methodSource.Append(disposeFieldName);
                                methodSource.Append("=");
                                if (implementsAsyncContainer)
                                    methodSource.Append("async");
                                methodSource.Append("() => {");
                                GenerateDisposeCode(methodSource, orderOfCreation, implementsAsyncContainer, instanceSource);
                                methodSource.Append("};");
                                methodSource.Append("}finally{this.");
                                methodSource.Append(lockName);
                                methodSource.Append(".Release();}return ");
                                methodSource.Append(singleInstanceFieldName);
                                methodSource.Append(";}");
                                containerMembersSource.Append(methodSource);
                            }
                            return (singleInstanceMethod.name, singleInstanceMethod.isAsync);
                        }
                    }
                
                    void ThrowObjectDisposedException(StringBuilder methodSource)
                    {
                        methodSource.Append("throw new ");
                        methodSource.Append(objectDisposedExceptionType.FullName());
                        methodSource.Append("(nameof(");
                        methodSource.Append(module.Name);
                        methodSource.Append("));");
                    }
                }

                if (containerMembersSource is not null)
                {
                    var file = new StringBuilder("#pragma warning disable CS1998\n");
                    var closingBraceCount = 0;
                    if (module.ContainingNamespace is { IsGlobalNamespace: false })
                    {
                        closingBraceCount++;
                        file.Append("namespace ");
                        file.Append(module.ContainingNamespace.FullName());
                        file.Append("{");
                    }

                    foreach (var type in module.GetContainingTypesAndThis().Reverse())
                    {
                        closingBraceCount++;
                        file.Append("partial class ");
                        file.Append(type.NameWithGenerics());
                        file.Append(@"{");
                    }

                    GenerateDisposeMethods(implementsSyncContainer, implementsAsyncContainer, file, containerScope, singleInstanceMethods);

                    file.Append(containerMembersSource);

                    for (int i = 0; i < closingBraceCount; i++)
                    {
                        file.Append("}");
                    }

                    var source = CSharpSyntaxTree.ParseText(SourceText.From(file.ToString(), Encoding.UTF8)).GetRoot().NormalizeWhitespace().SyntaxTree.GetText();
                    context.AddSource(
                        GenerateNameHint(module),
                        source);
                }
            }
        }

        private static void GenerateDisposeMethods(bool anySync, bool anyAsync, StringBuilder file, InstanceSourcesScope? containerScope, IReadOnlyDictionary<InstanceSource, (string name, bool isAsync, string disposeFieldName, string lockName)>? singleInstanceMethods)
        {
            file.Append(@"private int _disposed = 0; private bool Disposed => _disposed != 0;");
            var singleInstanceMethodsDisposalOrderings = singleInstanceMethods is null
                ? Enumerable.Empty<(string name, bool isAsync, string disposeFieldName, string lockName)>()
                : DependencyChecker.GetPartialOrderingOfSingleInstanceDependencies(containerScope!, singleInstanceMethods.Keys.ToHashSet()).Select(x => singleInstanceMethods[x]);

            if (anyAsync)
            {
                file.Append(@"public async global::System.Threading.Tasks.ValueTask DisposeAsync() {
var disposed = global::System.Threading.Interlocked.Exchange(ref this._disposed, 1);
if (disposed != 0) return;");
                foreach (var (_, _, disposeFieldName, lockName) in singleInstanceMethodsDisposalOrderings)
                {
                    file.Append(@"await this.");
                    file.Append(lockName);
                    file.Append(".WaitAsync();try{await(this.");
                    file.Append(disposeFieldName);
                    file.Append("?.Invoke()??default);}finally{this.");
                    file.Append(lockName);
                    file.Append(@".Release();}");
                }
                file.Append(@"}");
                if (anySync)
                {
                    file.Append(@"void global::System.IDisposable.Dispose() { throw new global::StrongInject.StrongInjectException(""This container requires async disposal""); }");
                }
            }
            else
            {
                file.Append(@"public void Dispose() {
var disposed = global::System.Threading.Interlocked.Exchange(ref this._disposed, 1);
if (disposed != 0) return;");
                foreach (var (_, _, disposeFieldName, lockName) in singleInstanceMethodsDisposalOrderings)
                {
                    file.Append(@"this.");
                    file.Append(lockName);
                    file.Append(".Wait();try{this.");
                    file.Append(disposeFieldName);
                    file.Append("?.Invoke();}finally{this.");
                    file.Append(lockName);
                    file.Append(@".Release();}");
                }
                file.Append(@"}");
            }
        }

        private static string GenerateNameHint(INamedTypeSymbol container)
        {
            var stringBuilder = new StringBuilder(container.ContainingNamespace.FullName());
            foreach (var type in container.GetContainingTypesAndThis().Reverse())
            {
                stringBuilder.Append(type.Name);
                if (type.TypeParameters.Length > 0)
                {
                    stringBuilder.Append("_");
                    stringBuilder.Append(type.TypeParameters.Length);
                }    
            }
            stringBuilder.Append(".generated.cs");
            return stringBuilder.ToString();
        }

        private static InstanceSourcesScope CreateContainerScope(INamedTypeSymbol? instanceProviderInterface, INamedTypeSymbol? asyncInstanceProviderInterface, INamedTypeSymbol module, IReadOnlyDictionary<ITypeSymbol, InstanceSource> registrations, Compilation compilation, Action<Diagnostic> reportDiagnostic, CancellationToken cancellationToken)
        {
            var instanceSources = registrations.ToDictionary(x => x.Key, x => x.Value);
            var instanceProviders = new Dictionary<ITypeSymbol, InstanceProvider>();
            foreach (var field in module.GetMembers()
                .OfType<IFieldSymbol>()
                .Where(x => !x.IsStatic))
            {
                foreach (var constructedInstanceProviderInterface in field.Type.AllInterfacesAndSelf().Where(x
                    => x.OriginalDefinition.Equals(instanceProviderInterface, SymbolEqualityComparer.Default)
                    || x.OriginalDefinition.Equals(asyncInstanceProviderInterface, SymbolEqualityComparer.Default)))
                {
                    var providedType = constructedInstanceProviderInterface.TypeArguments[0];
                    if (instanceProviders.TryGetValue(providedType, out var existing))
                    {
                        var exisingField = existing.instanceProviderField;
                        reportDiagnostic(DuplicateInstanceProviders(existing.instanceProviderField, existing.instanceProviderField, field, providedType, cancellationToken));
                        reportDiagnostic(DuplicateInstanceProviders(field, existing.instanceProviderField, field, providedType, cancellationToken));
                        continue;
                    }
                    var isAsync = constructedInstanceProviderInterface.OriginalDefinition.Equals(asyncInstanceProviderInterface, SymbolEqualityComparer.Default);
                    var instanceProvider = new InstanceProvider(providedType, field, constructedInstanceProviderInterface, isAsync);
                    instanceProviders[providedType] = instanceProvider;
                    instanceSources[providedType] = instanceProvider;
                }
            }
            return new(instanceSources, compilation);
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

        private static Diagnostic WarnIAsyncDisposableInSynchronousResolution(ITypeSymbol type, INamedTypeSymbol containerInterface, INamedTypeSymbol container, CancellationToken cancellationToken)
        {
            return Diagnostic.Create(
                new DiagnosticDescriptor(
                    "SI1301",
                    "Cannot call asynchronous dispose for Type in implementation of synchronous container",
                    "Cannot call asynchronous dispose for '{0}' in implementation of synchronous '{1}.Run'",
                    "StrongInject",
                    DiagnosticSeverity.Warning,
                    isEnabledByDefault: true),
                container.DeclaringSyntaxReferences.FirstOrDefault()?.GetSyntax(cancellationToken).GetLocation() ?? Location.None,
                type,
                containerInterface);
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
                    ({ scope: Scope.InstancePerDependency }, _) => false,
                    (Registration rX, Registration rY) => rX.scope == rY.scope && rX.type.Equals(rY.type, SymbolEqualityComparer.Default),
                    (FactoryRegistration fX, FactoryRegistration fY) => fX.scope == fY.scope && fX.factoryType.Equals(fY.factoryType, SymbolEqualityComparer.Default),
                    (InstanceProvider iX, InstanceProvider iY) => iX.providedType.Equals(iY.providedType, SymbolEqualityComparer.Default),
                    (DelegateSource dX, DelegateSource dY) => dX.delegateType.Equals(dY.delegateType, SymbolEqualityComparer.Default),
                    (DelegateParameter dX, DelegateParameter dY) => dX.parameter.Equals(dY.parameter, SymbolEqualityComparer.Default),
                    _ => false,
                };
            }

            public int GetHashCode(InstanceSource obj)
            {
                return obj switch
                {
                    null => 0,
                    { scope: Scope.InstancePerDependency } => new Random().Next(),
                    Registration r => 5 + r.scope.GetHashCode() * 17 + r.type.GetHashCode(),
                    InstanceProvider i => 7 + i.instanceProviderField.GetHashCode(),
                    FactoryRegistration f => 13 + f.scope.GetHashCode() * 17 + f.factoryType.GetHashCode(),
                    DelegateSource d => 17 + d.delegateType.GetHashCode(),
                    DelegateParameter dp => 19 + dp.parameter.GetHashCode(),
                    _ => throw new InvalidOperationException("This location is thought to be unreachable"),
                };
            }
        }
    }
}
