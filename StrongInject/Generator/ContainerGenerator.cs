using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using StrongInject.Generator.Visitors;
using System;
using System.Collections.Generic;
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

        private readonly Dictionary<InstanceSource, (string name, bool isAsync, string disposeFieldName, string lockName)> _singleInstanceMethods = new();
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
            _containerDeclarationLocation = ((ClassDeclarationSyntax)_container.DeclaringSyntaxReferences[0].GetSyntax()).Identifier.GetLocation();
        }

        private string GenerateContainerImplementations()
        {
            Debug.Assert(_containerMembersSource.Length == 0);

            foreach (var (constructedContainerInterface, isAsync) in _containerInterfaces)
            {
                _cancellationToken.ThrowIfCancellationRequested();

                var target = constructedContainerInterface.TypeArguments[0];

                var runMethodSource = new StringBuilder();
                var runMethodSymbol = (IMethodSymbol)constructedContainerInterface.GetMembers(isAsync
                    ? nameof(IAsyncContainer<object>.RunAsync)
                    : nameof(IContainer<object>.Run))[0];
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
                    ? nameof(IAsyncContainer<object>.ResolveAsync)
                    : nameof(IContainer<object>.Resolve))[0];
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
                    var variableCreationSource = new StringBuilder();
                    variableCreationSource.Append("if(Disposed)");
                    ThrowObjectDisposedException(variableCreationSource);
                    var resultVariableName = CreateVariables(
                        _containerScope[target],
                        variableCreationSource,
                        _containerScope,
                        isSingleInstanceCreation: false,
                        disposeAsynchronously: isAsync,
                        isAsyncContext: isAsync,
                        orderOfCreation: out var orderOfCreation);

                    var disposalSource = new StringBuilder();
                    GenerateDisposeCode(disposalSource, orderOfCreation, isAsync, singleInstanceTarget: null);

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

        private (string name, bool isAsync) GetSingleInstanceMethod(InstanceSource instanceSource)
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
                var singleInstanceFieldName = "_singleInstanceField" + index;
                var name = "GetSingleInstanceField" + index;
                var isAsync = RequiresAsyncVisitor.RequiresAsync(instanceSource, _containerScope);
                var disposeFieldName = "_disposeAction" + index;
                var lockName = "_lock" + index;
                singleInstanceMethod = (name, isAsync, disposeFieldName, lockName);
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
                methodSource.Append("(){");
                methodSource.Append("if (!object." + nameof(ReferenceEquals) + '(');
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
                var variableName = CreateVariables(
                    instanceSource,
                    methodSource,
                    _containerScope,
                    isSingleInstanceCreation: true,
                    disposeAsynchronously: _implementsAsyncContainer,
                    isAsyncContext: isAsync,
                    orderOfCreation: out var orderOfCreation);
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
                GenerateDisposeCode(methodSource, orderOfCreation, _implementsAsyncContainer, instanceSource);
                methodSource.Append("};");
                methodSource.Append("}finally{this.");
                methodSource.Append(lockName);
                methodSource.Append(".Release();}return ");
                methodSource.Append(singleInstanceFieldName);
                methodSource.Append(";}");
                _containerMembersSource.Append(methodSource);
            }
            return (singleInstanceMethod.name, singleInstanceMethod.isAsync);
        }

        private string CreateVariables(
            InstanceSource target,
            StringBuilder methodSource,
            InstanceSourcesScope instanceSourcesScope,
            bool isSingleInstanceCreation,
            bool disposeAsynchronously,
            bool isAsyncContext,
            out List<(string variableName, string? disposeActionsName, string? factoryVariable, InstanceSource source)> orderOfCreation,
            List<(string, InstanceSource)>? singleInstanceVariablesInScope = null)
        {
            _cancellationToken.ThrowIfCancellationRequested();
            var order = DependencyOrderingVisitor.OrderDependencies(
                target: target,
                currentScope: instanceSourcesScope,
                containerScope: _containerScope,
                isSingleInstanceCreation: isSingleInstanceCreation,
                isAsyncContext: isAsyncContext,
                singleInstanceVariablesInScope: singleInstanceVariablesInScope,
                out var targetName);

            orderOfCreation = new();
            foreach (var (source, assignedName, dependencies) in order)
            {
                _cancellationToken.ThrowIfCancellationRequested();
                CreateVariable(source, assignedName, dependencies, out var disposeActionsName, out var factoryName);
                orderOfCreation.Add((assignedName, disposeActionsName, factoryName, source));
            }
            return targetName;

            void CreateVariable(InstanceSource source, string assignedName, List<(string? name, InstanceSource? source)> dependencies, out string? disposeActionsName, out string? factoryName)
            {
                factoryName = null;
                disposeActionsName = null;
                switch (source)
                {
                    case { Scope: Scope.SingleInstance } and not (InstanceFieldOrProperty or ForwardedInstanceSource)
                        when !(isSingleInstanceCreation && ReferenceEquals(target, source)):
                        {
                            var (name, isAsync) = GetSingleInstanceMethod(source);
                            methodSource.Append("var ");
                            methodSource.Append(assignedName);
                            methodSource.Append(isAsync ? "=await " : '=');
                            methodSource.Append(name);
                            methodSource.Append("();");
                        }
                        break;
                    case FactorySource(var factoryOf, var underlying, var scope, var isAsync) registration:
                        factoryName = dependencies[0].name;
                        methodSource.Append("var ");
                        methodSource.Append(assignedName);
                        methodSource.Append(isAsync ? "=await " : "= ");
                        methodSource.Append(factoryName);
                        methodSource.Append('.');
                        methodSource.Append(isAsync
                            ? nameof(IAsyncFactory<object>.CreateAsync)
                            : nameof(IFactory<object>.Create));
                        methodSource.Append("();");
                        break;
                    case Registration(var _, var _, var requiresInitialization, var constructor, var isAsync) registration:
                        {
                            GenerateMethodCall(constructor, isAsync, decoratedParameter: null);

                            if (requiresInitialization)
                            {
                                GenerateInitializeCall(methodSource, assignedName, isAsync);
                            }

                            break;
                        }
                    case DelegateSource(var delegateType, var returnType, var parameters, var isAsync) delegateSource:
                        {
                            var disposeActionsIndex = methodSource.Length;

                            var disposeActionsSection = methodSource.BeginRevertableSection();

                            disposeActionsName = "disposeActions_" + assignedName;
                            methodSource.Append("var ");
                            methodSource.Append(disposeActionsName);
                            methodSource.Append(disposeAsynchronously
                                ? "=new global::System.Collections.Concurrent.ConcurrentBag<global::System.Func<global::System.Threading.Tasks.ValueTask>>();"
                                : "=new global::System.Collections.Concurrent.ConcurrentBag<global::System.Action>();");

                            disposeActionsSection.EndSection();

                            methodSource.Append(delegateType.FullName());
                            methodSource.Append(' ');
                            methodSource.Append(assignedName);
                            methodSource.Append(isAsync ? "=async(" : "=(");

                            var newScope = instanceSourcesScope.Enter(delegateSource);
                            foreach (var (parameter, index) in parameters.WithIndex())
                            {
                                if (index != 0)
                                    methodSource.Append(',');
                                methodSource.Append(((DelegateParameter)newScope[parameter.Type]).Name);
                            }

                            methodSource.Append(")=>{");

                            var newSingleInstanceVariablesInScope = singleInstanceVariablesInScope is { Count: > 0 }
                                    ? dependencies.Count > 0
                                        ? singleInstanceVariablesInScope.Concat(dependencies!).ToList()
                                        : singleInstanceVariablesInScope
                                     : dependencies!;

                            var variable = CreateVariables(
                                newScope[returnType],
                                methodSource,
                                newScope,
                                isSingleInstanceCreation: false,
                                disposeAsynchronously: disposeAsynchronously,
                                isAsyncContext: isAsync,
                                orderOfCreation: out var delegateOrderOfCreation,
                                newSingleInstanceVariablesInScope);

                            var disposeSection = methodSource.BeginRevertableSection();

                            methodSource.Append("disposeActions_" + assignedName);
                            methodSource.Append(".Add(");
                            if (disposeAsynchronously)
                                methodSource.Append("async");
                            methodSource.Append("() => {");

                            var beforeGenerateDisposeCodeLength = methodSource.Length;
                            GenerateDisposeCode(methodSource, delegateOrderOfCreation, disposeAsynchronously, null);
                            if (beforeGenerateDisposeCodeLength == methodSource.Length)
                            {
                                disposeSection.EndSection();
                                disposeSection.Revert();
                                disposeActionsSection.Revert();
                                disposeActionsName = null;
                            }
                            else
                            {
                                methodSource.Append("});");
                            }

                            methodSource.Append("return ");
                            methodSource.Append(variable);
                            methodSource.Append(";};");
                            break;
                        }
                    case FactoryMethod(var method, var _, var _, var _, var isAsync) registration:
                        {
                            GenerateMethodCall(method, isAsync, decoratedParameter: null);

                            break;
                        }
                    case ArraySource(var arrayType, _, var sources):
                        {
                            methodSource.Append("var ");
                            methodSource.Append(assignedName);
                            methodSource.Append("=new ");
                            methodSource.Append(arrayType.FullName());
                            methodSource.Append('{');

                            var elementType = arrayType.ElementType.FullName();
                            foreach (var dependency in dependencies)
                            {
                                methodSource.Append('(');
                                methodSource.Append(elementType);
                                methodSource.Append(')');
                                methodSource.Append(dependency.name);
                                methodSource.Append(',');
                            }
                            methodSource.Append("};");
                            break;
                        }
                    case WrappedDecoratorInstanceSource(var decoratorSource, var instanceSource):
                        {
                            switch (decoratorSource)
                            {
                                case DecoratorRegistration(var _, _, var requiresInitialization, var constructor, var decoratedParameter, _, var isAsync):

                                    GenerateMethodCall(constructor, isAsync, (decoratedParameter, instanceSource));

                                    if (requiresInitialization)
                                    {
                                        GenerateInitializeCall(methodSource, assignedName, isAsync);
                                    }

                                    break;
                                case DecoratorFactoryMethod(var method, var returnType, var _, var decoratedParameter, _, var isAsync) registration:
                                    GenerateMethodCall(method, isAsync, (decoratedParameter, instanceSource));
                                    break;
                                default: throw new NotImplementedException(decoratorSource.GetType().ToString());
                            }
                            break;
                        }
                    case InstanceFieldOrProperty { FieldOrPropertySymbol: var fieldOrPropertySymbol }:
                        {
                            methodSource.Append("var ");
                            methodSource.Append(assignedName);
                            methodSource.Append("=");
                            GenerateMemberAccess(methodSource, fieldOrPropertySymbol);
                            methodSource.Append(";");
                            break;
                        }
                    case ForwardedInstanceSource { AsType: var asType }:
                        {
                            methodSource.Append("var ");
                            methodSource.Append(assignedName);
                            methodSource.Append("=(");
                            methodSource.Append(asType.FullName());
                            methodSource.Append(")");
                            methodSource.Append(dependencies[0].name);
                            methodSource.Append(";");
                            break;
                        }
                    default:
                        throw new NotImplementedException(target.GetType().ToString());
                }
                void GenerateMethodCall(IMethodSymbol method, bool isAsync, (int ordinal, InstanceSource parameterInstanceSource)? decoratedParameter)
                {
                    methodSource.Append("var ");
                    methodSource.Append(assignedName);
                    bool isConstructor = method.MethodKind == MethodKind.Constructor;
                    methodSource.Append(isAsync && !isConstructor ? "=await " : '=');
                    if (isConstructor)
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
                        var (name, source) = dependencies[i];
                        if (source is not null)
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

        private void GenerateInitializeCall(StringBuilder methodSource, string? variableName, bool isAsync)
        {
            methodSource.Append(isAsync ? "await ((" : "((");
            methodSource.Append(isAsync
                ? _wellKnownTypes.IRequiresAsyncInitialization.FullName()
                : _wellKnownTypes.IRequiresInitialization.FullName());
            methodSource.Append(')');
            methodSource.Append(variableName);
            methodSource.Append(").");
            methodSource.Append(isAsync
                ? nameof(IRequiresAsyncInitialization.InitializeAsync)
                : nameof(IRequiresInitialization.Initialize));
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

        private void GenerateDisposeCode(
            StringBuilder methodSource,
            List<(string variableName, string? disposeActionsName, string? factoryName, InstanceSource source)> orderOfCreation,
            bool isAsync,
            InstanceSource? singleInstanceTarget)
        {
            for (int i = orderOfCreation.Count - 1; i >= 0; i--)
            {
                var (variableName, disposeActionName, factoryVariable, source) = orderOfCreation[i];
                switch (source)
                {
                    case { Scope: Scope.SingleInstance } when !source.Equals(singleInstanceTarget):
                        break;
                    case FactorySource { IsAsync: var isAsyncFactory }:
                        if (isAsyncFactory)
                        {
                            methodSource.Append("await ");
                        }
                        methodSource.Append(factoryVariable);
                        methodSource.Append('.');
                        methodSource.Append(isAsyncFactory
                            ? nameof(IAsyncFactory<object>.ReleaseAsync)
                            : nameof(IFactory<object>.Release));
                        methodSource.Append('(');
                        methodSource.Append(variableName);
                        methodSource.Append(");");
                        break;
                    case FactoryMethod { FactoryOfType: var type }:
                        DisposeExactTypeNotKnown(methodSource, isAsync, variableName, type);
                        break;
                    case Registration { Type: var type }:
                        DisposeExactTypeKnown(methodSource, isAsync, variableName, type);
                        break;
                    case DelegateSource:
                        if (disposeActionName is not null)
                        {
                            methodSource.Append("foreach (var disposeAction in ");
                            methodSource.Append(disposeActionName);
                            methodSource.Append(isAsync ? ")await disposeAction();" : ")disposeAction();");
                        }
                        break;
                    case WrappedDecoratorInstanceSource { Decorator: { dispose: var dispose } decorator }:
                        if (dispose)
                        {
                            switch (decorator)
                            {
                                case DecoratorRegistration { Type: var type }:
                                    DisposeExactTypeKnown(methodSource, isAsync, variableName, type);
                                    break;
                                case DecoratorFactoryMethod { DecoratedType: var type }:
                                    DisposeExactTypeNotKnown(methodSource, isAsync, variableName, type);
                                    break;
                                default: throw new NotImplementedException(decorator.GetType().ToString());
                            }
                        }
                        break;

                    case DelegateParameter:
                    case InstanceFieldOrProperty:
                    case ArraySource:
                    case ForwardedInstanceSource:
                        break;
                    default: throw new NotImplementedException(source.GetType().ToString());
                }
            }
        }

        private void DisposeExactTypeNotKnown(StringBuilder methodSource, bool isAsync, string? variableName, ITypeSymbol subTypeOf)
        {
            if ((subTypeOf.IsSealed || subTypeOf.IsValueType) && subTypeOf.TypeKind != TypeKind.TypeParameter)
            {
                DisposeExactTypeKnown(methodSource, isAsync, variableName, subTypeOf);
                return;
            }

            if (isAsync)
            {
                methodSource.Append("await ");
            }
            methodSource.Append(_wellKnownTypes.Helpers.FullName());
            methodSource.Append('.');
            methodSource.Append(isAsync
                ? nameof(Helpers.DisposeAsync)
                : nameof(Helpers.Dispose));
            methodSource.Append('(');
            methodSource.Append(variableName);
            methodSource.Append(");");
        }

        private void DisposeExactTypeKnown(StringBuilder methodSource, bool isAsync, string? variableName, ITypeSymbol type)
        {
            var isAsyncDisposable = type.AllInterfaces.Contains(_wellKnownTypes.IAsyncDisposable);
            if (isAsync && isAsyncDisposable)
            {
                methodSource.Append("await ((");
                methodSource.Append(_wellKnownTypes.IAsyncDisposable.FullName());
                methodSource.Append(')');
                methodSource.Append(variableName);
                methodSource.Append(")." + nameof(IAsyncDisposable.DisposeAsync) + '(');
                methodSource.Append(");");
            }
            else if (type.AllInterfaces.Contains(_wellKnownTypes.IDisposable))
            {
                methodSource.Append("((");
                methodSource.Append(_wellKnownTypes.IDisposable.FullName());
                methodSource.Append(')');
                methodSource.Append(variableName);
                methodSource.Append(")." + nameof(IDisposable.Dispose) + '(');
                methodSource.Append(");");
            }
            else if (isAsyncDisposable)
            {
                _reportDiagnostic(WarnIAsyncDisposableInSynchronousResolution(
                    type,
                    _containerDeclarationLocation));
            }
        }

        private void GenerateDisposeMethods(bool anySync, bool anyAsync, StringBuilder file)
        {
            file.Append(@"private int _disposed = 0; private bool Disposed => _disposed != 0;");
            var singleInstanceMethodsDisposalOrderings = _singleInstanceMethods is null
                ? Enumerable.Empty<(string name, bool isAsync, string disposeFieldName, string lockName)>()
                : PartialOrderingOfSingleInstanceDependenciesVisitor.GetPartialOrdering(_containerScope, _singleInstanceMethods.Keys.ToHashSet()).Select(x => _singleInstanceMethods[x]);

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

        private static Diagnostic WarnIAsyncDisposableInSynchronousResolution(ITypeSymbol type, Location location)
        {
            return Diagnostic.Create(
                new DiagnosticDescriptor(
                    "SI1301",
                    "Cannot call asynchronous dispose for Type in implementation of synchronous container",
                    "Cannot call asynchronous dispose for '{0}' in implementation of synchronous container",
                    "StrongInject",
                    DiagnosticSeverity.Warning,
                    isEnabledByDefault: true),
                location,
                type);
        }
    }
}
