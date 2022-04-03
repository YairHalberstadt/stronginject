using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using StrongInject.Generator.Visitors;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
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
        private AutoIndenter _containerMembersSource = default!;
        private readonly List<(INamedTypeSymbol containerInterface, bool isAsync)> _containerInterfaces;
        private readonly bool _implementsSyncContainer;
        private readonly bool _implementsAsyncContainer;
        private readonly Location _containerDeclarationLocation;
        private readonly RequiresAsyncChecker _requiresAsyncChecker;

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
            _requiresAsyncChecker = new RequiresAsyncChecker(_containerScope, _cancellationToken);

            _containerInterfaces = _container.AllInterfaces
                .Where(x=> WellKnownTypes.IsContainerOrAsyncContainer(x))
                .Select(x => (containerInterface: x, isAsync: WellKnownTypes.IsAsyncContainer(x)))
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
            Debug.Assert(_containerMembersSource is null);
            var classMembersIndent = _container.GetContainingTypesAndThis().Count()
                                     + (_container.ContainingNamespace is { IsGlobalNamespace: false } ? 1 : 0);
            _containerMembersSource = new AutoIndenter(classMembersIndent);

            bool requiresUnsafe = false;

            foreach (var (constructedContainerInterface, isAsync) in _containerInterfaces)
            {
                _cancellationToken.ThrowIfCancellationRequested();

                var target = constructedContainerInterface.TypeArguments[0];

                var runMethodSource = _containerMembersSource.GetSubIndenter();
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
                        runMethodSource.Append(", ");
                    runMethodSource.Append(typeParam.Name);
                }

                runMethodSource.Append(">(");
                for (int i = 0; i < runMethodSymbol.Parameters.Length; i++)
                {
                    var param = runMethodSymbol.Parameters[i];
                    if (i != 0)
                        runMethodSource.Append(", ");
                    runMethodSource.Append(param.Type.FullName());
                    runMethodSource.Append(' ');
                    runMethodSource.Append(param.Name);
                }

                runMethodSource.AppendLine(")");
                runMethodSource.AppendLine("{");

                var resolveMethodSource = _containerMembersSource.GetSubIndenter();
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
                resolveMethodSource.AppendLine("()");
                resolveMethodSource.AppendLine("{");


                if (DependencyCheckerVisitor.CheckDependencies(
                        target,
                        isAsync,
                        _containerScope,
                        _reportDiagnostic,
                        _containerDeclarationLocation,
                        _cancellationToken))
                {
                    // error reported. Implement with throwing implementation to remove NotImplemented error CS0535
                    const string THROW_NOT_IMPLEMENTED_EXCEPTION = "throw new global::System.NotImplementedException();";
                    runMethodSource.AppendLine(THROW_NOT_IMPLEMENTED_EXCEPTION);
                    resolveMethodSource.AppendLine(THROW_NOT_IMPLEMENTED_EXCEPTION);
                }
                else
                {
                    requiresUnsafe |= RequiresUnsafeVisitor.RequiresUnsafe(target, _containerScope, _cancellationToken);

                    var variableCreationSource = runMethodSource.GetSubIndenter();
                    variableCreationSource.AppendLine("if (Disposed)");
                    ThrowObjectDisposedException(variableCreationSource);

                    var ops = LoweringVisitor.LowerResolution(
                        requiresAsyncChecker: _requiresAsyncChecker,
                        target: _containerScope[target],
                        containerScope: _containerScope,
                        disposalLowerer: CreateDisposalLowerer(new DisposalStyle(isAsync, DisposalStyleDeterminant.Container)),
                        isSingleInstanceCreation: false,
                        isAsyncContext: isAsync,
                        out var resultVariableName,
                        _cancellationToken);

                    CreateVariables(
                        ops,
                        variableCreationSource,
                        _containerScope);

                    runMethodSource.Append(variableCreationSource);
                    runMethodSource.Append(runMethodSymbol.TypeParameters[0].Name);
                    runMethodSource.AppendLine(" result;");
                    runMethodSource.AppendLine("try");
                    runMethodSource.AppendLine("{");
                    runMethodSource.Append("result = ");
                    runMethodSource.Append(isAsync ? "await func(" : "func(");
                    runMethodSource.Append(resultVariableName);
                    runMethodSource.AppendLine(", param);");

                    runMethodSource.AppendLine("}");
                    runMethodSource.AppendLine("finally");
                    runMethodSource.AppendLine("{");

                    var disposalSource = runMethodSource.GetSubIndenter();
                    EmitDisposals(disposalSource, ops);
                    runMethodSource.Append(disposalSource);

                    runMethodSource.AppendLine("}");
                    runMethodSource.AppendLine("return result;");

                    var ownedTypeName = isAsync
                        ? WellKnownTypes.ConstructedAsyncOwnedEmitName(target.FullName())
                        : WellKnownTypes.ConstructedOwnedEmitName(target.FullName());
                    resolveMethodSource.Append(variableCreationSource);
                    resolveMethodSource.Append("return new ");
                    resolveMethodSource.Append(ownedTypeName);
                    resolveMethodSource.Append('(');
                    resolveMethodSource.Append(resultVariableName);
                    resolveMethodSource.AppendLine(isAsync ? ", async () =>" : ", () =>");
                    resolveMethodSource.AppendLine("{");

                    resolveMethodSource.Append(disposalSource);

                    resolveMethodSource.AppendLine("});");
                }

                runMethodSource.AppendLine("}");
                resolveMethodSource.AppendLine("}");

                _containerMembersSource.AppendLine();
                _containerMembersSource.Append(runMethodSource);
                _containerMembersSource.AppendLine();
                _containerMembersSource.Append(resolveMethodSource);
            }
            
            var file = new AutoIndenter(0);
            file.AppendLine("#pragma warning disable CS1998");
            if (_container.ContainingNamespace is { IsGlobalNamespace: false })
            {
                file.Append("namespace ");
                file.AppendLine(_container.ContainingNamespace.FullName());
                file.AppendLine("{");
            }

            foreach (var type in _container.GetContainingTypesAndThis().Reverse())
            {
                if (requiresUnsafe)
                {
                    file.Append("unsafe ");
                }
                file.Append("partial class ");
                file.AppendLine(type.NameWithGenerics());
                file.AppendLine("{");
            }

            GenerateDisposeMethods(_implementsSyncContainer, _implementsAsyncContainer, file);

            file.Append(_containerMembersSource);

            while (file.Indent != 0)
            {
                file.AppendLine("}");
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
                var singleInstanceFieldSetName = singleInstanceFieldName + "Set";
                var lockName = "_lock" + index;
                var disposeFieldName = "_disposeAction" + index;
                var name = "Get" + type.ToIdentifier("SingleInstance") + "Field" + index;
                singleInstanceMethod = (name, disposeFieldName, lockName);
                _singleInstanceMethods.Add(instanceSource, singleInstanceMethod);

                var methodSource = _containerMembersSource.GetSubIndenter();
                methodSource.Append("private ");
                methodSource.Append(type.FullName());
                methodSource.Append(' ');
                methodSource.Append(singleInstanceFieldName);
                methodSource.AppendLine(';');

                methodSource.Append("private bool ");
                methodSource.Append(singleInstanceFieldSetName);
                methodSource.AppendLine(';');

                methodSource.Append("private global::System.Threading.SemaphoreSlim ");
                methodSource.Append(lockName);
                methodSource.AppendLine(" = new global::System.Threading.SemaphoreSlim(1);");

                methodSource.Append((_implementsAsyncContainer
                    ? "private global::System.Func<global::System.Threading.Tasks.ValueTask> "
                    : "private global::System.Action "));
                methodSource.Append(disposeFieldName);
                methodSource.AppendLine(';');

                methodSource.Append(isAsync ? "private async " : "private ");
                methodSource.Append(isAsync ? WellKnownTypes.ConstructedValueTask1EmitName(type.FullName()) : type.FullName());
                methodSource.Append(' ');
                methodSource.Append(name);
                methodSource.AppendLine("()");
                methodSource.AppendLine("{");
                CheckFieldSet(methodSource, singleInstanceFieldSetName, singleInstanceFieldName);

                if (isAsync)
                    methodSource.Append("await ");
                methodSource.Append("this.");
                methodSource.Append(lockName);
                methodSource.AppendLine(isAsync ? ".WaitAsync();" : ".Wait();");

                methodSource.AppendLine("try");
                methodSource.AppendLine("{");
                CheckFieldSet(methodSource, singleInstanceFieldSetName, singleInstanceFieldName);

                methodSource.AppendLine("if (this.Disposed)");
                ThrowObjectDisposedException(methodSource);

                var ops = LoweringVisitor.LowerResolution(
                    requiresAsyncChecker: _requiresAsyncChecker,
                    target: instanceSource,
                    containerScope: _containerScope,
                    disposalLowerer: CreateDisposalLowerer(new DisposalStyle(_implementsAsyncContainer, DisposalStyleDeterminant.Container)),
                    isSingleInstanceCreation: true,
                    isAsyncContext: isAsync,
                    out var variableName,
                    _cancellationToken);
                CreateVariables(
                    ops,
                    methodSource,
                    _containerScope);

                methodSource.Append("this.");
                methodSource.Append(singleInstanceFieldName);
                methodSource.Append(" = ");
                methodSource.Append(variableName);
                methodSource.AppendLine(';');

                methodSource.Append("this.");
                methodSource.Append(singleInstanceFieldSetName);
                methodSource.AppendLine(" = true;");

                methodSource.Append("this.");
                methodSource.Append(disposeFieldName);
                methodSource.Append(" = ");
                if (_implementsAsyncContainer)
                    methodSource.Append("async ");
                methodSource.AppendLine("() =>");
                methodSource.AppendLine("{");
                EmitDisposals(methodSource, ops);
                methodSource.AppendLine("};");
                methodSource.AppendLine("}");
                methodSource.AppendLine("finally");
                methodSource.AppendLine("{");

                methodSource.Append("this.");
                methodSource.Append(lockName);
                methodSource.AppendLine(".Release();");

                methodSource.AppendLine("}");
                methodSource.Append("return ");
                methodSource.Append(singleInstanceFieldName);
                methodSource.AppendLine(";");
                methodSource.AppendLine("}");

                _containerMembersSource.AppendLine();
                _containerMembersSource.Append(methodSource);
            }

            return singleInstanceMethod.name;

            static void CheckFieldSet(AutoIndenter methodSource, string singleInstanceFieldSetName, string singleInstanceFieldName)
            {
                methodSource.Append("if (this.");
                methodSource.Append(singleInstanceFieldSetName);
                methodSource.AppendLine(")");
                methodSource.AppendIndented("return this.");
                methodSource.Append(singleInstanceFieldName);
                methodSource.AppendLine(';');
            }
        }

        private DisposalLowerer CreateDisposalLowerer(DisposalStyle disposalStyle)
            => new(disposalStyle, _wellKnownTypes, _reportDiagnostic, _containerDeclarationLocation);

        private void CreateVariables(
            ImmutableArray<Operation> operations,
            AutoIndenter methodSource,
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
                    methodSource.AppendLine(" = false;");

                    if (awaitResultType is not null)
                    {
                        methodSource.Append("var ");
                        methodSource.Append(awaitResultVariableName!);
                        methodSource.Append(" = default(");
                        methodSource.Append(awaitResultType.FullName());
                        methodSource.AppendLine(");");

                        if (operation.CanDisposeAwaitStatementResultLocally())
                        {
                            methodSource.Append("var ");
                            methodSource.Append(hasAwaitCompletedVariableName);
                            methodSource.AppendLine(" = false;");
                        }
                    }
                }
                else
                {
                    var (typeName, name) = operation.Statement switch
                    {
                        SingleInstanceReferenceStatement { VariableName: var variableName, Source: var source, IsAsync: var isAsync } => (
                            isAsync
                                ? WellKnownTypes.ConstructedValueTask1EmitName(source.OfType.FullName())
                                : source.OfType.FullName(),
                            variableName),
                        DelegateCreationStatement { VariableName: var variableName, Source: var source } => (source.OfType.FullName(), variableName),
                        OwnedCreationLocalFunctionStatement => (null, null),
                        OwnedCreationStatement { VariableName: var variableName, Source: var source } => (source.OfType.FullName(), variableName),
                        DependencyCreationStatement { VariableName: var variableName, Source: { OfType: var ofType } source } => (source.IsAsync
                            ? source switch
                            {
                                Registration or WrappedDecoratorInstanceSource { Decorator: DecoratorRegistration } => ofType.FullName(),
                                FactorySource => WellKnownTypes.ConstructedValueTask1EmitName(ofType.FullName()),
                                FactoryMethod { Method: { ReturnType: var returnType } } => returnType.FullName(),
                                WrappedDecoratorInstanceSource { Decorator: DecoratorFactoryMethod { Method: { ReturnType: var returnType } } } => returnType.FullName(),
                                _ => throw new NotImplementedException(source.GetType().ToString())
                            }
                            : ofType.FullName(), variableName),
                        DisposeActionsCreationStatement { VariableName: var variableName, TypeName: var disposeActionsType } => (disposeActionsType, variableName),
                        InitializationStatement { VariableName: var variableName } => (variableName is null ? null : WellKnownTypes.VALUE_TASK_EMIT_NAME, variableName),
                        _ => throw new NotImplementedException(operation.Statement.GetType().ToString()),
                    };
                    if (typeName is not null)
                    {
                        methodSource.Append(typeName);
                        methodSource.Append(" ");
                        methodSource.Append(name!);
                        methodSource.AppendLine(";");
                    }
                }
            }

            for (var i = 0; i < operations.Length; i++)
            {
                var operation = operations[i];
                EmitStatement(operation);

                if (i != operations.Length - 1 && (operation.CanDisposeLocally || operation.AwaitStatement is not null))
                {
                    methodSource.AppendLine("try");
                    methodSource.AppendLine("{");
                }
            }

            for (var i = operations.Length - 2; i >= 0; i--)
            {
                var operation = operations[i];
                if (operation.CanDisposeLocally || operation.AwaitStatement is not null)
                {
                    methodSource.AppendLine("}");
                    methodSource.AppendLine("catch");
                    methodSource.AppendLine("{");
                    if (operation.AwaitStatement is
                        {
                            HasAwaitStartedVariableName: var hasAwaitStartedVariableName,
                            VariableName: var variableName,
                            VariableToAwaitName: var variableToAwaitName,
                            HasAwaitCompletedVariableName: var hasAwaitCompletedVariableName
                        })
                    {
                        methodSource.Append("if (!");
                        methodSource.Append(hasAwaitStartedVariableName);
                        methodSource.AppendLine(")");
                        methodSource.AppendLine("{");
                        if (!operation.CanDisposeLocally)
                        {
                            methodSource.Append("_ = ");
                            methodSource.Append(variableToAwaitName);
                            var isValueTask = operation.Statement switch
                            {
                                SingleInstanceReferenceStatement => true,
                                InitializationStatement => true,
                                DependencyCreationStatement { Source: var source } => source switch
                                {
                                    FactorySource => true,
                                    FactoryMethod { Method: { ReturnType: var returnType } } => WellKnownTypes.IsValueTask1(returnType),
                                    WrappedDecoratorInstanceSource { Decorator: DecoratorFactoryMethod { Method: { ReturnType: var returnType } } }
                                        => WellKnownTypes.IsValueTask1(returnType),
                                    _ => throw new NotImplementedException(source.GetType().ToString())
                                },
                                _ => throw new NotImplementedException(operation.Statement.GetType().ToString()),
                            };
                            if (isValueTask)
                            {
                                methodSource.Append(".AsTask()");
                            }

                            methodSource.AppendLine(".ContinueWith(failedTask => _ = failedTask.Exception, global::System.Threading.Tasks.TaskContinuationOptions.OnlyOnFaulted);");
                            methodSource.AppendLine("}");
                        }
                        else
                        {
                            methodSource.Append(variableName!);
                            methodSource.Append(" = await ");
                            methodSource.Append(variableToAwaitName);
                            methodSource.AppendLine(";");
                            methodSource.AppendLine("}");
                            methodSource.Append("else if (!");
                            methodSource.Append(hasAwaitCompletedVariableName);
                            methodSource.AppendLine(")");
                            methodSource.AppendLine("{");
                            methodSource.AppendLine("throw;");
                            methodSource.AppendLine("}");
                        }
                    }

                    if (operation.CanDisposeLocally)
                    {
                        EmitDisposal(methodSource, operation);
                    }

                    methodSource.AppendLine("throw;");

                    methodSource.AppendLine("}");
                }
            }

            void EmitStatement(Operation operation)
            {
                _cancellationToken.ThrowIfCancellationRequested();
                switch (operation.Statement)
                {
                    case SingleInstanceReferenceStatement(var variableName, var source, var isAsync):
                        {
                            var name = GetSingleInstanceMethod(source, isAsync);
                            methodSource.Append(variableName);
                            methodSource.Append(" = ");
                            methodSource.Append(name);
                            methodSource.AppendLine("();");
                        }
                        break;
                    case DisposeActionsCreationStatement(var variableName, var type):
                        {
                            methodSource.Append(variableName);
                            methodSource.Append(" = new ");
                            methodSource.Append(type);
                            methodSource.AppendLine("();");
                            break;
                        }
                    case DelegateCreationStatement
                    {
                        VariableName: var variableName,
                        Source:
                        {
                            IsAsync: var isAsync,
                            Parameters: var parameters,
                        } source,
                        InternalOperations: var internalOps,
                        InternalTargetName: var internalTargetName,
                        DisposeActionsName: var disposeActionsName
                    }:
                        var isDisposed = operation.Disposal is not null;
                        var disposeAsynchronously = operation.Disposal is { IsAsync: true };
                        // definitely assign the delegate variable so it can be referenced inside the delegate 
                        methodSource.Append(variableName);
                        methodSource.AppendLine(" = null;");
                        methodSource.Append(variableName);
                        methodSource.Append(isAsync ? " = async(" : " = (");

                        var newScope = instanceSourcesScope.Enter(source);
                        foreach (var (parameter, index) in parameters.WithIndex())
                        {
                            if (index != 0)
                                methodSource.Append(", ");
                            methodSource.Append(((DelegateParameter)newScope[parameter.Type]).Name);
                        }

                        methodSource.AppendLine(") =>");
                        methodSource.AppendLine("{");
                        
                            CreateVariables(
                                internalOps,
                                methodSource,
                                newScope);

                            if (isDisposed)
                            {
                                methodSource.Append(disposeActionsName);
                                methodSource.Append(".Add(");
                                if (disposeAsynchronously)
                                    methodSource.Append("async ");
                                methodSource.AppendLine("() =>");
                                methodSource.AppendLine("{");
                                    EmitDisposals(methodSource, internalOps);
                                methodSource.AppendLine("});");
                            }

                            methodSource.Append("return ");
                            methodSource.Append(internalTargetName);
                            methodSource.AppendLine(";");
                        methodSource.AppendLine("};");

                        break;
                    case OwnedCreationLocalFunctionStatement(var source, var isAsyncLocalFunction, var localFunctionName, var internalOps, var internalTargetName):
                        if (isAsyncLocalFunction)
                            methodSource.Append("async ");

                        methodSource.Append((isAsyncLocalFunction
                            ? WellKnownTypes.ConstructedValueTask1EmitName(source.OwnedType.FullName())
                            : source.OwnedType.FullName()));

                        methodSource.Append(' ');
                        methodSource.Append(localFunctionName);
                        methodSource.AppendLine("()");
                        methodSource.AppendLine("{");
                            CreateVariables(
                                internalOps,
                                methodSource,
                                instanceSourcesScope);

                            methodSource.Append("return new ");
                            methodSource.Append(source.OwnedType.FullName());
                            methodSource.Append('(');
                            methodSource.Append(internalTargetName);
                            methodSource.Append(", ");
                            if (source.IsAsync)
                                methodSource.Append("async ");
                            methodSource.AppendLine("() =>");
                            methodSource.AppendLine("{");
                                EmitDisposals(methodSource, internalOps);
                            methodSource.AppendLine("});");
                        methodSource.AppendLine("}");
                        break;
                    case OwnedCreationStatement(var variableName, _, var isAsyncLocalFunction, var localFunctionName):
                        methodSource.Append(variableName);
                        methodSource.Append(" = ");
                        if (isAsyncLocalFunction)
                            methodSource.Append("await ");
                        methodSource.Append(localFunctionName);
                        methodSource.AppendLine("();");
                        break;
                    case DependencyCreationStatement(var variableName, var source, var dependencies):

                        switch (source)
                        {
                            case FactorySource { IsAsync: var isAsync }:
                                var factoryName = dependencies[0];
                                methodSource.Append(variableName);
                                methodSource.Append(" = ");
                                methodSource.Append(factoryName!);
                                methodSource.Append('.');
                                methodSource.Append(isAsync
                                    ? "CreateAsync"
                                    : "Create");
                                methodSource.AppendLine("();");
                                break;
                            case Registration { Constructor: var constructor }:
                                {
                                    GenerateMethodCall(variableName, constructor, dependencies);
                                    break;
                                }
                            case FactoryMethod { Method: var method }:
                                {
                                    GenerateMethodCall(variableName, method, dependencies);
                                    break;
                                }
                            case ArraySource { ArrayType: var arrayType }:
                                {
                                    methodSource.Append(variableName);
                                    methodSource.Append(" = new ");
                                    methodSource.AppendLine(arrayType.FullName());
                                    methodSource.AppendLine('{');
                                    var elementType = arrayType.ElementType.FullName();
                                    foreach (var dependency in dependencies)
                                    {
                                        methodSource.Append('(');
                                        methodSource.Append(elementType);
                                        methodSource.Append(')');
                                        methodSource.Append(dependency!);
                                        methodSource.AppendLine(',');
                                    }

                                    methodSource.AppendLine("};");
                                    break;
                                }
                            case WrappedDecoratorInstanceSource { Decorator: var decoratorSource }:
                                {
                                    switch (decoratorSource)
                                    {
                                        case DecoratorRegistration { Constructor: var constructor}:
                                            GenerateMethodCall(variableName, constructor, dependencies);
                                            break;
                                        case DecoratorFactoryMethod { Method: var method }:
                                            GenerateMethodCall(variableName, method, dependencies);
                                            break;
                                        default: throw new NotImplementedException(decoratorSource.GetType().ToString());
                                    }
                                    break;
                                }
                            case InstanceFieldOrProperty { FieldOrPropertySymbol: var fieldOrPropertySymbol }:
                                {
                                    methodSource.Append(variableName);
                                    methodSource.Append(" = ");
                                    GenerateMemberAccess(methodSource, fieldOrPropertySymbol);
                                    methodSource.AppendLine(";");
                                    break;
                                }
                            case ForwardedInstanceSource { AsType: var asType }:
                                {
                                    methodSource.Append(variableName);
                                    methodSource.Append(" = (");
                                    methodSource.Append(asType.FullName());
                                    methodSource.Append(")");
                                    methodSource.Append(dependencies[0]!);
                                    methodSource.AppendLine(";");
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
                        methodSource.AppendLine(" = true;");
                        if (variableName is not null)
                        {
                            methodSource.Append(variableName);
                            methodSource.Append(" = ");
                        }
                        methodSource.Append("await ");
                        methodSource.Append(variableToAwaitName);
                        methodSource.AppendLine(";");
                        if (operation.CanDisposeAwaitStatementResultLocally())
                        {
                            methodSource.Append(hasAwaitCompletedVariableName);
                            methodSource.AppendLine(" = true;");
                        }
                        break;
                    default:
                        throw new NotImplementedException(operation.Statement.GetType().ToString());
                }

                void GenerateMethodCall(string variableName, IMethodSymbol method, ImmutableArray<string?> dependencies)
                {
                    methodSource.Append(variableName);
                    methodSource.Append(" = ");
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
                                    methodSource.Append(", ");
                                methodSource.Append(typeArgument.FullName());
                            }
                            methodSource.Append('>');
                        }
                    }
                    methodSource.Append('(');
                    bool isFirst = true;
                    for (int i = 0; i < method.Parameters.Length; i++)
                    {
                        var parameter = method.Parameters[i];
                        var name = dependencies[i];
                        if (name is not null)
                        {
                            if (!isFirst)
                            {
                                methodSource.Append(", ");
                            }
                            isFirst = false;
                            methodSource.Append(parameter.Name);
                            methodSource.Append(": ");
                            methodSource.Append(name);
                        }
                    }
                    methodSource.AppendLine(");");
                }
            }
        }

        private void GenerateInitializeCall(AutoIndenter methodSource, string? variableName, string variableToInitializeName, bool isAsync)
        {
            if (isAsync)
            {
                methodSource.Append(variableName!);
                methodSource.Append(" = ((");
                methodSource.Append(WellKnownTypes.IREQUIRES_ASYNC_INITIALIZATION_EMIT_NAME);
            }
            else
            {
                methodSource.Append("((");
                methodSource.Append(WellKnownTypes.IREQUIRES_INITIALIZATION_EMIT_NAME);
            }
            methodSource.Append(')');
            methodSource.Append(variableToInitializeName);
            methodSource.Append(").");
            methodSource.Append(isAsync
                ? "InitializeAsync"
                : "Initialize");
            methodSource.AppendLine("();");
        }

        private static void GenerateMemberAccess(AutoIndenter methodSource, ISymbol member)
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

        private void EmitDisposals(AutoIndenter methodSource, ImmutableArray<Operation> operations)
        {
            for (int i = operations.Length - 1; i >= 0; i--)
            {
                EmitDisposal(methodSource, operations[i]);
            }
        }

        private void EmitDisposal(AutoIndenter methodSource, Operation operation)
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
                        methodSource.AppendLine(")");
                        methodSource.AppendLine(isAsync ? "await disposeAction();" : "disposeAction();");
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
                    methodSource.AppendLine(");");
                    break;
                case Disposal.DisposalHelpers { VariableName: var variableName, IsAsync: var isAsync }:
                    if (isAsync)
                    {
                        methodSource.Append("await ");
                    }
                    methodSource.Append(WellKnownTypes.HELPERS_EMIT_NAME);
                    methodSource.Append('.');
                    methodSource.Append(isAsync
                        ? "DisposeAsync"
                        : "Dispose");
                    methodSource.Append('(');
                    methodSource.Append(variableName);
                    methodSource.AppendLine(");");
                    break;
                case Disposal.IDisposable { VariableName: var variableName, IsAsync: var isAsync }:
                    if (isAsync)
                    {
                        methodSource.Append("await ((");
                        methodSource.Append(WellKnownTypes.IASYNC_DISPOSABLE_EMIT_NAME);
                        methodSource.Append(')');
                        methodSource.Append(variableName);
                        methodSource.Append(")." + nameof(IAsyncDisposable.DisposeAsync) + '(');
                        methodSource.AppendLine(");");
                    }
                    else
                    {
                        methodSource.Append("((");
                        methodSource.Append(WellKnownTypes.IDISPOSABLE_EMIT_NAME);
                        methodSource.Append(')');
                        methodSource.Append(variableName);
                        methodSource.Append(")." + nameof(IDisposable.Dispose) + '(');
                        methodSource.AppendLine(");");
                    }
                    break;
                case null:
                    break;
                case var disposal:
                    throw new NotImplementedException(disposal.GetType().ToString());
            }
        }

        private void GenerateDisposeMethods(bool anySync, bool anyAsync, AutoIndenter file)
        {
            file.AppendLine(@"private int _disposed = 0;");
            file.AppendLine(@"private bool Disposed => _disposed != 0;");
            var singleInstanceMethodsDisposalOrderings = _singleInstanceMethods.Count == 0
                ? Enumerable.Empty<(string name, string disposeFieldName, string lockName)>()
                : PartialOrderingOfSingleInstanceDependenciesVisitor.GetPartialOrdering(_containerScope, _singleInstanceMethods.Keys.ToHashSet(), _cancellationToken).Select(x => _singleInstanceMethods[x]);

            if (anyAsync)
            {
                file.AppendLine(@"public async global::System.Threading.Tasks.ValueTask DisposeAsync()");
                file.AppendLine(@"{");
                file.AppendLine(@"var disposed = global::System.Threading.Interlocked.Exchange(ref this._disposed, 1);");
                file.AppendLine(@"if (disposed != 0)");
                file.AppendLineIndented(@"return;");
                foreach (var (_, disposeFieldName, lockName) in singleInstanceMethodsDisposalOrderings)
                {
                    file.Append(@"await this.");
                    file.Append(lockName);
                    file.AppendLine(".WaitAsync();");
                    file.AppendLine("try");
                    file.AppendLine("{");
                    file.Append("await (this.");
                    file.Append(disposeFieldName);
                    file.AppendLine("?.Invoke() ?? default);");

                    file.AppendLine("}");
                    file.AppendLine("finally");
                    file.AppendLine("{");
                    file.Append("this.");
                    file.Append(lockName);
                    file.AppendLine(".Release();");
                    file.AppendLine("}");
                }

                file.AppendLine("}");
                if (anySync)
                {
                    file.AppendLine(@"void global::System.IDisposable.Dispose()");
                    file.AppendLine(@"{");
                    file.AppendLine(@"throw new global::StrongInject.StrongInjectException(""This container requires async disposal"");");
                    file.AppendLine(@"}");
                }
            }
            else
            {
                file.AppendLine("public void Dispose()");
                file.AppendLine(@"{");
                file.AppendLine("var disposed = global::System.Threading.Interlocked.Exchange(ref this._disposed, 1);");
                file.AppendLine("if (disposed != 0)");
                file.AppendLineIndented("return;");
                foreach (var (_, disposeFieldName, lockName) in singleInstanceMethodsDisposalOrderings)
                {
                    file.Append(@"this.");
                    file.Append(lockName);
                    file.AppendLine(".Wait();");
                    file.AppendLine("try");
                    file.AppendLine("{");
                    file.Append("this.");
                    file.Append(disposeFieldName);
                    file.AppendLine("?.Invoke();");
                    file.AppendLine("}");
                    file.AppendLine("finally");
                    file.AppendLine("{");
                    file.Append("this.");
                    file.Append(lockName);
                    file.AppendLine(".Release();");
                    file.AppendLine("}");
                }
                file.AppendLine('}');
            }
        }

        void ThrowObjectDisposedException(AutoIndenter methodSource)
        {
            methodSource.AppendIndented("throw new ");
            methodSource.Append(WellKnownTypes.OBJECT_DISPOSED_EXCEPTION_EMIT_NAME);
            methodSource.Append("(nameof(");
            methodSource.Append(_container.NameWithTypeParameters());
            methodSource.AppendLine("));");
        }
    }
}