using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using StrongInject.Generator.Visitors;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;

namespace StrongInject.Generator
{
    internal class ContainerGenerator
    {
        public static string GenerateContainerImplementations(
            INamedTypeSymbol container,
            InstanceSourcesScope containerScope,
            WellKnownTypes wellKnownTypes,
            Action<Diagnostic> reportDiagnostic,
            CancellationToken cancellationToken) => new ContainerGenerator(
                container,
                containerScope,
                wellKnownTypes,
                reportDiagnostic,
                cancellationToken).GenerateContainerImplementations();

        private readonly INamedTypeSymbol _container;
        private readonly WellKnownTypes _wellKnownTypes;
        private readonly Action<Diagnostic> _reportDiagnostic;
        private readonly CancellationToken _cancellationToken;
        private readonly InstanceSourcesScope _containerScope;

        private readonly Dictionary<InstanceSource, (string name, string disposeFieldName, string lockName)> _singleInstanceMethods = new();
        private readonly StringBuilder _containerMembersSource = new();
        private readonly List<(INamedTypeSymbol containerInterface, bool isAsync)> _containerInterfaces;
        private readonly bool _implementsSyncContainer;
        private readonly bool _implementsAsyncContainer;
        private readonly Location _containerDeclarationLocation;

        private ContainerGenerator(
            INamedTypeSymbol container,
            InstanceSourcesScope containerScope,
            WellKnownTypes wellKnownTypes,
            Action<Diagnostic> reportDiagnostic,
            CancellationToken cancellationToken)
        {
            _container = container;
            _wellKnownTypes = wellKnownTypes;
            _reportDiagnostic = reportDiagnostic;
            _cancellationToken = cancellationToken;
            _containerScope = containerScope;

            _containerInterfaces = _container.AllInterfaces
                .Where(x
                    => x.OriginalDefinition.Equals(_wellKnownTypes.IContainer, SymbolEqualityComparer.Default)
                    || x.OriginalDefinition.Equals(_wellKnownTypes.IAsyncContainer, SymbolEqualityComparer.Default))
                .Select(x => (containerInterface: x, isAsync: x.OriginalDefinition.Equals(_wellKnownTypes.IAsyncContainer, SymbolEqualityComparer.Default)))
                .ToList();

            foreach (var (_, isAsync) in _containerInterfaces)
            {
                (isAsync ? ref _implementsAsyncContainer : ref _implementsSyncContainer) = true;
            }

            // Ideally we would use the location of the interface in the base list, however getting that location is complex and not critical for now.
            // See http://sourceroslyn.io/#Microsoft.CodeAnalysis.CSharp/Symbols/Source/SourceMemberContainerSymbol_ImplementationChecks.cs,333
            _containerDeclarationLocation = ((TypeDeclarationSyntax)_container.DeclaringSyntaxReferences[0].GetSyntax()).Identifier.GetLocation();
        }

        private string GenerateContainerImplementations()
        {
            Debug.Assert(_containerMembersSource.Length == 0);

            bool requiresUnsafe = false;

            foreach (var (constructedContainerInterface, isAsync) in _containerInterfaces)
            {
                _cancellationToken.ThrowIfCancellationRequested();

                var target = constructedContainerInterface.TypeArguments[0];

                var runMethodSource = new StringBuilder();
                var runMethodSymbol = (IMethodSymbol)constructedContainerInterface.GetMembers(isAsync
                    ? "RunAsync"
                    : "Run")[0];
                if (isAsync)
                {
                    runMethodSource.Append("async ");
                }
                runMethodSource.Append(runMethodSymbol.ReturnType.FullName());
                runMethodSource.Append(' ');
                runMethodSource.Append(constructedContainerInterface.FullName());
                runMethodSource.Append('.');
                runMethodSource.Append(runMethodSymbol.Name);
                runMethodSource.Append('<');
                for (int i = 0; i < runMethodSymbol.TypeParameters.Length; i++)
                {
                    var typeParam = runMethodSymbol.TypeParameters[i];
                    if (i != 0)
                        runMethodSource.Append(',');
                    runMethodSource.Append(typeParam.Name);
                }
                runMethodSource.Append(">(");
                for (int i = 0; i < runMethodSymbol.Parameters.Length; i++)
                {
                    var param = runMethodSymbol.Parameters[i];
                    if (i != 0)
                        runMethodSource.Append(',');
                    runMethodSource.Append(param.Type.FullName());
                    runMethodSource.Append(' ');
                    runMethodSource.Append(param.Name);
                }
                runMethodSource.Append("){");

                var resolveMethodSource = new StringBuilder();
                var resolveMethodSymbol = (IMethodSymbol)constructedContainerInterface.GetMembers(isAsync
                    ? "ResolveAsync"
                    : "Resolve")[0];
                if (isAsync)
                {
                    resolveMethodSource.Append("async ");
                }
                resolveMethodSource.Append(resolveMethodSymbol.ReturnType.FullName());
                resolveMethodSource.Append(' ');
                resolveMethodSource.Append(constructedContainerInterface.FullName());
                resolveMethodSource.Append('.');
                resolveMethodSource.Append(resolveMethodSymbol.Name);
                resolveMethodSource.Append("(){");

                if (DependencyCheckerVisitor.HasCircularOrMissingDependencies(
                        target,
                        isAsync,
                        _containerScope,
                        _reportDiagnostic,
                        _containerDeclarationLocation))
                {
                    // error reported. Implement with throwing implementation to remove NotImplemented error CS0535
                    const string THROW_NOT_IMPLEMENTED_EXCEPTION = "throw new global::System.NotImplementedException();}";
                    runMethodSource.Append(THROW_NOT_IMPLEMENTED_EXCEPTION);
                    resolveMethodSource.Append(THROW_NOT_IMPLEMENTED_EXCEPTION);

                }
                else
                {
                    requiresUnsafe |= RequiresUnsafeVisitor.RequiresUnsafe(target, _containerScope);

                    var variableCreationSource = new StringBuilder();
                    variableCreationSource.Append("if(Disposed)");
                    ThrowObjectDisposedException(variableCreationSource);

                    var ops = LoweringVisitor.LowerResolution(
                        target: _containerScope[target],
                        containerScope: _containerScope,
                        disposalLowerer: CreateDisposalLowerer(isAsync),
                        isSingleInstanceCreation: false,
                        isAsyncContext: isAsync,
                        out var resultVariableName);

                    CreateVariables(
                        ops,
                        variableCreationSource,
                        _containerScope);

                    var disposalSource = new StringBuilder();
                    EmitDisposals(disposalSource, ops);

                    runMethodSource.Append(variableCreationSource);
                    runMethodSource.Append(runMethodSymbol.TypeParameters[0].Name);
                    runMethodSource.Append(" result;try{result=");
                    if (isAsync)
                    {
                        runMethodSource.Append("await func(");
                    }
                    else
                    {
                        runMethodSource.Append("func(");
                    }
                    runMethodSource.Append(resultVariableName);
                    runMethodSource.Append(", param);}finally{");
                    runMethodSource.Append(disposalSource);
                    runMethodSource.Append("}return result;}");

                    var ownedType = (isAsync
                        ? _wellKnownTypes.AsyncOwned
                        : _wellKnownTypes.Owned).Construct(target);
                    resolveMethodSource.Append(variableCreationSource);
                    resolveMethodSource.Append("return new ");
                    resolveMethodSource.Append(ownedType.FullName());
                    resolveMethodSource.Append('(');
                    resolveMethodSource.Append(resultVariableName);
                    resolveMethodSource.Append(isAsync ? ",async()=>{" : ",()=>{");
                    resolveMethodSource.Append(disposalSource);
                    resolveMethodSource.Append("});}");
                }

                _containerMembersSource.Append(runMethodSource);
                _containerMembersSource.Append(resolveMethodSource);
            }

            var file = new StringBuilder("#pragma warning disable CS1998\n");
            var closingBraceCount = 0;
            if (_container.ContainingNamespace is { IsGlobalNamespace: false })
            {
                closingBraceCount++;
                file.Append("namespace ");
                file.Append(_container.ContainingNamespace.FullName());
                file.Append('{');
            }

            foreach (var type in _container.GetContainingTypesAndThis().Reverse())
            {
                closingBraceCount++;
                if (requiresUnsafe)
                {
                    file.Append("unsafe ");
                }
                file.Append("partial class ");
                file.Append(type.NameWithGenerics());
                file.Append('{');
            }

            GenerateDisposeMethods(_implementsSyncContainer, _implementsAsyncContainer, file);

            file.Append(_containerMembersSource);

            for (int i = 0; i < closingBraceCount; i++)
            {
                file.Append('}');
            }

            return file.ToString();
        }

        private string GetSingleInstanceMethod(InstanceSource instanceSource, bool isAsync)
        {
            if (!_singleInstanceMethods.TryGetValue(instanceSource, out var singleInstanceMethod))
            {
                var type = instanceSource switch
                {
                    Registration { Type: var t } => t,
                    FactorySource { FactoryOf: var t } => t,
                    FactoryMethod { FactoryOfType: var t } => t,
                    WrappedDecoratorInstanceSource { OfType: var t } => t,
                    _ => throw new InvalidOperationException(),
                };

                var index = _singleInstanceMethods.Count;

                var singleInstanceFieldName = "_" + type.ToLowerCaseIdentifier("singleInstance") + "Field" + index;
                var name = "Get" + type.ToIdentifier("SingleInstance") + "Field" + index;
                var disposeFieldName = "_disposeAction" + index;
                var lockName = "_lock" + index;
                singleInstanceMethod = (name, disposeFieldName, lockName);
                _singleInstanceMethods.Add(instanceSource, singleInstanceMethod);
                _containerMembersSource.Append("private " + type.FullName() + ' ' + singleInstanceFieldName + ';');
                _containerMembersSource.Append("private global::System.Threading.SemaphoreSlim _lock" + index + "=new global::System.Threading.SemaphoreSlim(1);");
                _containerMembersSource.Append((_implementsAsyncContainer
                    ? "private global::System.Func<global::System.Threading.Tasks.ValueTask>"
                    : "private global::System.Action") + " _disposeAction" + index + ';');
                var methodSource = new StringBuilder();
                methodSource.Append(isAsync ? "private async " : "private ");
                methodSource.Append(isAsync ? _wellKnownTypes.ValueTask1.Construct(type).FullName() : type.FullName());
                methodSource.Append(' ');
                methodSource.Append(name);
                methodSource.Append("(){if(!object." + nameof(ReferenceEquals) + '(');
                methodSource.Append(singleInstanceFieldName);
                methodSource.Append(",null");
                methodSource.Append("))");
                methodSource.Append("return ");
                methodSource.Append(singleInstanceFieldName);
                methodSource.Append(';');
                if (isAsync)
                    methodSource.Append("await ");
                methodSource.Append("this.");
                methodSource.Append(lockName);
                methodSource.Append(isAsync ? ".WaitAsync();" : ".Wait();");
                methodSource.Append("try{if(this.Disposed)");
                ThrowObjectDisposedException(methodSource);

                var ops = LoweringVisitor.LowerResolution(
                    target: instanceSource,
                    containerScope: _containerScope,
                    disposalLowerer: CreateDisposalLowerer(_implementsAsyncContainer),
                    isSingleInstanceCreation: true,
                    isAsyncContext: isAsync,
                    out var variableName);
                CreateVariables(
                    ops,
                    methodSource,
                    _containerScope);

                methodSource.Append("this.");
                methodSource.Append(singleInstanceFieldName);
                methodSource.Append('=');
                methodSource.Append(variableName);
                methodSource.Append(';');
                methodSource.Append("this.");
                methodSource.Append(disposeFieldName);
                methodSource.Append('=');
                if (_implementsAsyncContainer)
                    methodSource.Append("async");
                methodSource.Append("() => {");
                EmitDisposals(methodSource, ops);
                methodSource.Append("};");
                methodSource.Append("}finally{this.");
                methodSource.Append(lockName);
                methodSource.Append(".Release();}return ");
                methodSource.Append(singleInstanceFieldName);
                methodSource.Append(";}");
                _containerMembersSource.Append(methodSource);
            }
            return singleInstanceMethod.name;
        }

        private DisposalLowerer CreateDisposalLowerer(bool disposeAsynchronously)
            => new DisposalLowerer(disposeAsynchronously, _wellKnownTypes, _reportDiagnostic, _containerDeclarationLocation);

        private void CreateVariables(
            ImmutableArray<Operation> operations,
            StringBuilder methodSource,
            InstanceSourcesScope instanceSourcesScope)
        {
            _cancellationToken.ThrowIfCancellationRequested();

            foreach (var operation in operations)
            {
                if (operation.Statement is AwaitStatement
                    {
                        VariableName: var awaitResultVariableName,
                        Type: var awaitResultType,
                        HasAwaitStartedVariableName: var hasAwaitStartedVariableName,
                        HasAwaitCompletedVariableName: var hasAwaitCompletedVariableName
                    })
                {
                    methodSource.Append("var ");
                    methodSource.Append(hasAwaitStartedVariableName);
                    methodSource.Append("=false;");

                    if (awaitResultType is not null)
                    {
                        methodSource.Append("var ");
                        methodSource.Append(awaitResultVariableName);
                        methodSource.Append("=default(");
                        methodSource.Append(awaitResultType.FullName());
                        methodSource.Append(");");

                        if (operation.CanDisposeAwaitStatementResultLocally())
                        {
                            methodSource.Append("var ");
                            methodSource.Append(hasAwaitCompletedVariableName);
                            methodSource.Append("=false;");
                        }
                    }
                }
                else
                {
                    var (type, name) = operation.Statement switch
                    {
                        SingleInstanceReferenceStatement { VariableName: var variableName, Source: var source, IsAsync: var isAsync } => (
                            isAsync
                                ? _wellKnownTypes.ValueTask1.Construct(source.OfType)
                                : source.OfType,
                            variableName),
                        DelegateCreationStatement { VariableName: var variableName, Source: var source } => (source.OfType, variableName),
                        DependencyCreationStatement { VariableName: var variableName, Source: { OfType: var ofType } source } => (source.IsAsync
                            ? source switch
                            {
                                Registration or WrappedDecoratorInstanceSource { Decorator: DecoratorRegistration } => ofType,
                                FactorySource => _wellKnownTypes.ValueTask1.Construct(ofType),
                                FactoryMethod { Method: { ReturnType: var returnType } } => returnType,
                                WrappedDecoratorInstanceSource { Decorator: DecoratorFactoryMethod { Method: { ReturnType: var returnType } } } => returnType,
                                _ => throw new NotImplementedException(source.GetType().ToString())
                            }
                            : ofType, variableName),
                        DisposeActionsCreationStatement { VariableName: var variableName, Type: var disposeActionsType } => (disposeActionsType, variableName),
                        InitializationStatement { VariableName: var variableName } => (variableName is null ? null : _wellKnownTypes.ValueTask, variableName),
                        _ => throw new NotImplementedException(operation.Statement.GetType().ToString()),
                    };
                    if (type is not null)
                    {
                        methodSource.Append(type.FullName());
                        methodSource.Append(" ");
                        methodSource.Append(name);
                        methodSource.Append(";");
                    }
                }
            }

            for (var i = 0; i < operations.Length; i++)
            {
                var operation = operations[i];
                EmitStatement(operation);

                if (i != operations.Length - 1 && (operation.CanDisposeLocally || operation.AwaitStatement is not null))
                {
                    methodSource.Append("try{");
                }
            }

            for (var i = operations.Length - 2; i >= 0; i--)
            {
                var operation = operations[i];
                if (operation.CanDisposeLocally || operation.AwaitStatement is not null)
                {
                    methodSource.Append("}catch{");
                    if (operation.AwaitStatement is 
                    {
                        HasAwaitStartedVariableName: var hasAwaitStartedVariableName,
                        VariableName: var variableName,
                        VariableToAwaitName: var variableToAwaitName,
                        HasAwaitCompletedVariableName: var hasAwaitCompletedVariableName
                    })
                    {
                        methodSource.Append("if(!");
                        methodSource.Append(hasAwaitStartedVariableName);
                        methodSource.Append("){");
                        if (!operation.CanDisposeLocally)
                        {
                            methodSource.Append("_=");
                            methodSource.Append(variableToAwaitName);
                            var isValueTask = operation.Statement switch
                            {
                                SingleInstanceReferenceStatement => true,
                                InitializationStatement => true,
                                DependencyCreationStatement { Source: var source } => source switch
                                {
                                    FactorySource => true,
                                    FactoryMethod { Method: { ReturnType: var returnType } }
                                        => returnType.OriginalDefinition.Equals(_wellKnownTypes.ValueTask1, SymbolEqualityComparer.Default),
                                    WrappedDecoratorInstanceSource { Decorator: DecoratorFactoryMethod { Method: { ReturnType: var returnType } } }
                                        => returnType.OriginalDefinition.Equals(_wellKnownTypes.ValueTask1, SymbolEqualityComparer.Default),
                                    _ => throw new NotImplementedException(source.GetType().ToString())
                                },
                                _ => throw new NotImplementedException(operation.Statement.GetType().ToString()),
                            };
                            if (isValueTask)
                            {
                                methodSource.Append(".AsTask()");
                            }
                            methodSource.Append(".ContinueWith(failedTask => _ = failedTask.Exception, global::System.Threading.Tasks.TaskContinuationOptions.OnlyOnFaulted);}");
                        }
                        else
                        {
                            methodSource.Append(variableName);
                            methodSource.Append("=await ");
                            methodSource.Append(variableToAwaitName);
                            methodSource.Append(";}else if (!");
                            methodSource.Append(hasAwaitCompletedVariableName);
                            methodSource.Append("){throw;}");
                        }
                    }
                    if (operation.CanDisposeLocally)
                    {
                        EmitDisposal(methodSource, operation);
                    }
                    methodSource.Append("throw;}");
                }
            }

            void EmitStatement(Operation operation)
            {
                switch (operation.Statement)
                {
                    case SingleInstanceReferenceStatement(var variableName, var source, var isAsync):
                        {
                            var name = GetSingleInstanceMethod(source, isAsync);
                            methodSource.Append(variableName);
                            methodSource.Append('=');
                            methodSource.Append(name);
                            methodSource.Append("();");
                        }
                        break;
                    case DisposeActionsCreationStatement(var variableName, var type):
                        {
                            methodSource.Append(variableName);
                            methodSource.Append("=new ");
                            methodSource.Append(type.FullName());
                            methodSource.Append("();");
                            break;
                        }
                    case DelegateCreationStatement
                    {
                        VariableName: var variableName,
                        Source:
                        {
                            DelegateType: var delegateType,
                            IsAsync: var isAsync,
                            Parameters: var parameters,
                            ReturnType: var returnType
                        } source,
                        InternalOperations: var internalOps,
                        InternalTargetName: var internalTargetName,
                        DisposeActionsName: var disposeActionsName
                    }:
                        var isDisposed = operation.Disposal is not null;
                        var disposeAsynchronously = operation.Disposal is { IsAsync: true };

                        methodSource.Append(variableName);
                        methodSource.Append(isAsync ? "=async(" : "=(");

                        var newScope = instanceSourcesScope.Enter(source);
                        foreach (var (parameter, index) in parameters.WithIndex())
                        {
                            if (index != 0)
                                methodSource.Append(',');
                            methodSource.Append(((DelegateParameter)newScope[parameter.Type]).Name);
                        }

                        methodSource.Append(")=>{");

                        CreateVariables(
                            internalOps,
                            methodSource,
                            newScope);

                        if (isDisposed)
                        {
                            methodSource.Append(disposeActionsName);
                            methodSource.Append(".Add(");
                            if (disposeAsynchronously)
                                methodSource.Append("async");
                            methodSource.Append("() => {");
                            EmitDisposals(methodSource, internalOps);
                            methodSource.Append("});");
                        }

                        methodSource.Append("return ");
                        methodSource.Append(internalTargetName);
                        methodSource.Append(";};");
                        break;
                    case DependencyCreationStatement(var variableName, var source, var dependencies):

                        switch (source)
                        {
                            case FactorySource(var factoryOf, var underlying, var scope, var isAsync) registration:
                                var factoryName = dependencies[0];
                                methodSource.Append(variableName);
                                methodSource.Append('=');
                                methodSource.Append(factoryName);
                                methodSource.Append('.');
                                methodSource.Append(isAsync
                                    ? "CreateAsync"
                                    : "Create");
                                methodSource.Append("();");
                                break;
                            case Registration(_, _, _, var constructor, _) registration:
                                {
                                    GenerateMethodCall(variableName, constructor, dependencies);
                                    break;
                                }
                            case FactoryMethod(var method, _, _, _, _) registration:
                                {
                                    GenerateMethodCall(variableName, method, dependencies);

                                    break;
                                }
                            case ArraySource(var arrayType, _, var sources):
                                {
                                    methodSource.Append(variableName);
                                    methodSource.Append("=new ");
                                    methodSource.Append(arrayType.FullName());
                                    methodSource.Append('{');

                                    var elementType = arrayType.ElementType.FullName();
                                    foreach (var dependency in dependencies)
                                    {
                                        methodSource.Append('(');
                                        methodSource.Append(elementType);
                                        methodSource.Append(')');
                                        methodSource.Append(dependency);
                                        methodSource.Append(',');
                                    }
                                    methodSource.Append("};");
                                    break;
                                }
                            case WrappedDecoratorInstanceSource(var decoratorSource, var instanceSource):
                                {
                                    switch (decoratorSource)
                                    {
                                        case DecoratorRegistration(_, _, _, var constructor, _, _, _):
                                            GenerateMethodCall(variableName, constructor, dependencies);
                                            break;
                                        case DecoratorFactoryMethod(var method, var returnType, _, _, _, _) registration:
                                            GenerateMethodCall(variableName, method, dependencies);
                                            break;
                                        default: throw new NotImplementedException(decoratorSource.GetType().ToString());
                                    }
                                    break;
                                }
                            case InstanceFieldOrProperty { FieldOrPropertySymbol: var fieldOrPropertySymbol }:
                                {
                                    methodSource.Append(variableName);
                                    methodSource.Append("=");
                                    GenerateMemberAccess(methodSource, fieldOrPropertySymbol);
                                    methodSource.Append(";");
                                    break;
                                }
                            case ForwardedInstanceSource { AsType: var asType }:
                                {
                                    methodSource.Append(variableName);
                                    methodSource.Append("=(");
                                    methodSource.Append(asType.FullName());
                                    methodSource.Append(")");
                                    methodSource.Append(dependencies[0]);
                                    methodSource.Append(";");
                                    break;
                                }
                            default:
                                throw new NotImplementedException(source.GetType().ToString());
                        }
                        break;
                    case InitializationStatement(var variableName, var variableToInitializeName, var isAsync):
                        GenerateInitializeCall(methodSource, variableName, variableToInitializeName, isAsync);
                        break;
                    case AwaitStatement
                    {
                        VariableName: var variableName,
                        VariableToAwaitName: var variableToAwaitName,
                        HasAwaitStartedVariableName: var hasAwaitStartedVariableName,
                        HasAwaitCompletedVariableName: var hasAwaitCompletedVariableName
                    }:
                        methodSource.Append(hasAwaitStartedVariableName);
                        methodSource.Append("=true;");
                        if (variableName is not null)
                        {
                            methodSource.Append(variableName);
                            methodSource.Append('=');
                        }
                        methodSource.Append("await ");
                        methodSource.Append(variableToAwaitName);
                        methodSource.Append(";");
                        if (operation.CanDisposeAwaitStatementResultLocally())
                        {
                            methodSource.Append(hasAwaitCompletedVariableName);
                            methodSource.Append("=true;");
                        }
                        break;
                    default:
                        throw new NotImplementedException(operation.Statement.GetType().ToString());
                }

                void GenerateMethodCall(string variableName, IMethodSymbol method, ImmutableArray<string?> dependencies)
                {
                    methodSource.Append(variableName);
                    methodSource.Append('=');
                    if (method.MethodKind == MethodKind.Constructor)
                    {
                        methodSource.Append("new ");
                        methodSource.Append(method.ContainingType.FullName());
                    }
                    else
                    {
                        GenerateMemberAccess(methodSource, method);
                        if (method.TypeArguments.Length > 0)
                        {
                            methodSource.Append('<');
                            for (int i = 0; i < method.TypeArguments.Length; i++)
                            {
                                var typeArgument = method.TypeArguments[i];
                                if (i != 0)
                                    methodSource.Append(',');
                                methodSource.Append(typeArgument.FullName());
                            }
                            methodSource.Append('>');
                        }
                    }
                    methodSource.Append('(');
                    bool isFirst = true;
                    for (int i = 0; i < method.Parameters.Length; i++)
                    {
                        IParameterSymbol? parameter = method.Parameters[i];
                        var name = dependencies[i];
                        if (name is not null)
                        {
                            if (!isFirst)
                            {
                                methodSource.Append(',');
                            }
                            isFirst = false;
                            methodSource.Append(parameter.Name);
                            methodSource.Append(':');
                            methodSource.Append(name);
                        }
                    }
                    methodSource.Append(");");
                }
            }
        }

        private void GenerateInitializeCall(StringBuilder methodSource, string? variableName, string variableToInitializeName, bool isAsync)
        {
            if (isAsync)
            {
                methodSource.Append(variableName);
                methodSource.Append("=((");
                methodSource.Append(_wellKnownTypes.IRequiresAsyncInitialization.FullName());
            }
            else
            {
                methodSource.Append("((");
                methodSource.Append(_wellKnownTypes.IRequiresInitialization.FullName());
            }
            methodSource.Append(')');
            methodSource.Append(variableToInitializeName);
            methodSource.Append(").");
            methodSource.Append(isAsync
                ? "InitializeAsync"
                : "Initialize");
            methodSource.Append("();");
        }

        private static void GenerateMemberAccess(StringBuilder methodSource, ISymbol member)
        {
            if (member.IsStatic)
            {
                methodSource.Append(member.ContainingType.FullName());
            }
            else
            {
                methodSource.Append("this");
            }

            methodSource.Append('.');
            methodSource.Append(member.Name);
        }

        private void EmitDisposals(StringBuilder methodSource, ImmutableArray<Operation> operations)
        {
            for (int i = operations.Length - 1; i >= 0; i--)
            {
                EmitDisposal(methodSource, operations[i]);
            }
        }

        private void EmitDisposal(StringBuilder methodSource, Operation operation)
        {
            switch (operation.Disposal)
            {
                case Disposal.DelegateDisposal
                {
                    DisposeActionsName: var disposeActionsName,
                    IsAsync: var isAsync,
                }:
                    {
                        methodSource.Append("foreach (var disposeAction in ");
                        methodSource.Append(disposeActionsName);
                        methodSource.Append(isAsync ? ")await disposeAction();" : ")disposeAction();");
                        break;
                    }
                case Disposal.FactoryDisposal { VariableName: var variableName, FactoryName: var factoryName, IsAsync: var isAsync }:
                    if (isAsync)
                    {
                        methodSource.Append("await ");
                    }
                    methodSource.Append(factoryName);
                    methodSource.Append('.');
                    methodSource.Append(isAsync
                        ? "ReleaseAsync"
                        : "Release");
                    methodSource.Append('(');
                    methodSource.Append(variableName);
                    methodSource.Append(");");
                    break;
                case Disposal.DisposalHelpers { VariableName: var variableName, IsAsync: var isAsync }:
                    if (isAsync)
                    {
                        methodSource.Append("await ");
                    }
                    methodSource.Append(_wellKnownTypes.Helpers.FullName());
                    methodSource.Append('.');
                    methodSource.Append(isAsync
                        ? "DisposeAsync"
                        : "Dispose");
                    methodSource.Append('(');
                    methodSource.Append(variableName);
                    methodSource.Append(");");
                    break;
                case Disposal.IDisposable { VariableName: var variableName, IsAsync: var isAsync }:
                    if (isAsync)
                    {
                        methodSource.Append("await ((");
                        methodSource.Append(_wellKnownTypes.IAsyncDisposable.FullName());
                        methodSource.Append(')');
                        methodSource.Append(variableName);
                        methodSource.Append(")." + nameof(IAsyncDisposable.DisposeAsync) + '(');
                        methodSource.Append(");");
                    }
                    else
                    {
                        methodSource.Append("((");
                        methodSource.Append(_wellKnownTypes.IDisposable.FullName());
                        methodSource.Append(')');
                        methodSource.Append(variableName);
                        methodSource.Append(")." + nameof(IDisposable.Dispose) + '(');
                        methodSource.Append(");");
                    }
                    break;
                case null:
                    break;
                case var disposal:
                    throw new NotImplementedException(disposal.GetType().ToString());
            }
        }

        private void GenerateDisposeMethods(bool anySync, bool anyAsync, StringBuilder file)
        {
            file.Append(@"private int _disposed = 0; private bool Disposed => _disposed != 0;");
            var singleInstanceMethodsDisposalOrderings = _singleInstanceMethods.Count == 0
                ? Enumerable.Empty<(string name, string disposeFieldName, string lockName)>()
                : PartialOrderingOfSingleInstanceDependenciesVisitor.GetPartialOrdering(_containerScope, _singleInstanceMethods.Keys.ToHashSet()).Select(x => _singleInstanceMethods[x]);

            if (anyAsync)
            {
                file.Append(@"public async global::System.Threading.Tasks.ValueTask DisposeAsync() {
var disposed = global::System.Threading.Interlocked.Exchange(ref this._disposed, 1);
if (disposed != 0) return;");
                foreach (var (_, disposeFieldName, lockName) in singleInstanceMethodsDisposalOrderings)
                {
                    file.Append(@"await this.");
                    file.Append(lockName);
                    file.Append(".WaitAsync();try{await(this.");
                    file.Append(disposeFieldName);
                    file.Append("?.Invoke()??default);}finally{this.");
                    file.Append(lockName);
                    file.Append(@".Release();}");
                }
                file.Append('}');
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
                foreach (var (_, disposeFieldName, lockName) in singleInstanceMethodsDisposalOrderings)
                {
                    file.Append(@"this.");
                    file.Append(lockName);
                    file.Append(".Wait();try{this.");
                    file.Append(disposeFieldName);
                    file.Append("?.Invoke();}finally{this.");
                    file.Append(lockName);
                    file.Append(@".Release();}");
                }
                file.Append('}');
            }
        }

        void ThrowObjectDisposedException(StringBuilder methodSource)
        {
            methodSource.Append("throw new ");
            methodSource.Append(_wellKnownTypes.ObjectDisposedException.FullName());
            methodSource.Append("(nameof(");
            methodSource.Append(_container.NameWithTypeParameters());
            methodSource.Append("));");
        }
    }
}
