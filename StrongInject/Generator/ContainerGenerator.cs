using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
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
            IReadOnlyDictionary<ITypeSymbol, InstanceSource> registrations,
            WellKnownTypes wellKnownTypes,
            Action<Diagnostic> reportDiagnostic,
            CancellationToken cancellationToken) => new ContainerGenerator(
                container,
                registrations,
                wellKnownTypes,
                reportDiagnostic,
                cancellationToken).GenerateContainerImplementations();

        private readonly INamedTypeSymbol _container;
        private readonly WellKnownTypes _wellKnownTypes;
        private readonly Action<Diagnostic> _reportDiagnostic;
        private readonly CancellationToken _cancellationToken;
        private readonly InstanceSourcesScope _containerScope;

        private readonly Dictionary<InstanceSource, (string name, bool isAsync, string disposeFieldName, string lockName)> _singleInstanceMethods = new(InstanceSourceComparer.Instance);
        private readonly StringBuilder _containerMembersSource = new();
        private readonly List<(INamedTypeSymbol containerInterface, bool isAsync)> _containerInterfaces;
        private readonly bool _implementsSyncContainer;
        private readonly bool _implementsAsyncContainer;

        private ContainerGenerator(
            INamedTypeSymbol container,
            IReadOnlyDictionary<ITypeSymbol, InstanceSource> registrations,
            WellKnownTypes wellKnownTypes,
            Action<Diagnostic> reportDiagnostic,
            CancellationToken cancellationToken)
        {
            _container = container;
            _wellKnownTypes = wellKnownTypes;
            _reportDiagnostic = reportDiagnostic;
            _cancellationToken = cancellationToken;
            _containerScope ??= new(registrations, wellKnownTypes);

            _containerInterfaces = _container.AllInterfaces
                .Where(x
                    => x.OriginalDefinition.Equals(_wellKnownTypes.iContainer, SymbolEqualityComparer.Default)
                    || x.OriginalDefinition.Equals(_wellKnownTypes.iAsyncContainer, SymbolEqualityComparer.Default))
                .Select(x => (containerInterface: x, isAsync: x.OriginalDefinition.Equals(_wellKnownTypes.iAsyncContainer, SymbolEqualityComparer.Default)))
                .ToList();

            foreach (var (_, isAsync) in _containerInterfaces)
            {
                (isAsync ? ref _implementsAsyncContainer : ref _implementsSyncContainer) = true;
            }
        }

        private string GenerateContainerImplementations()
        {
            Debug.Assert(_containerMembersSource.Length == 0);

            foreach (var (constructedContainerInterface, isAsync) in _containerInterfaces)
            {
                _cancellationToken.ThrowIfCancellationRequested();

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
                        _containerScope,
                        _reportDiagnostic,
                        // Ideally we would use the location of the interface in the base list, however getting that location is complex and not critical for now.
                        // See http://sourceroslyn.io/#Microsoft.CodeAnalysis.CSharp/Symbols/Source/SourceMemberContainerSymbol_ImplementationChecks.cs,333
                        ((ClassDeclarationSyntax)_container.DeclaringSyntaxReferences[0].GetSyntax()).Identifier.GetLocation()))
                {
                    // error reported. Implement with throwing implementation to remove NotImplemented error CS0535
                    methodSource.Append("throw new global::System.NotImplementedException();}");
                }
                else
                {
                    methodSource.Append("if(Disposed)");
                    ThrowObjectDisposedException(methodSource);
                    var resultVariableName = CreateVariable(
                        _containerScope[target],
                        methodSource,
                        _containerScope,
                        isSingleInstanceCreation: false,
                        disposeAsynchronously: isAsync,
                        orderOfCreation: out var orderOfCreation);
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

                _containerMembersSource.Append(methodSource);
            }

            var file = new StringBuilder("#pragma warning disable CS1998\n");
            var closingBraceCount = 0;
            if (_container.ContainingNamespace is { IsGlobalNamespace: false })
            {
                closingBraceCount++;
                file.Append("namespace ");
                file.Append(_container.ContainingNamespace.FullName());
                file.Append("{");
            }

            foreach (var type in _container.GetContainingTypesAndThis().Reverse())
            {
                closingBraceCount++;
                file.Append("partial class ");
                file.Append(type.NameWithGenerics());
                file.Append(@"{");
            }

            GenerateDisposeMethods(_implementsSyncContainer, _implementsAsyncContainer, file);

            file.Append(_containerMembersSource);

            for (int i = 0; i < closingBraceCount; i++)
            {
                file.Append("}");
            }

            return file.ToString();
        }

        private (string name, bool isAsync) GetSingleInstanceMethod(InstanceSource instanceSource)
        {
            if (!_singleInstanceMethods.TryGetValue(instanceSource, out var singleInstanceMethod))
            {
                var type = instanceSource switch
                {
                    Registration { type: var t } => t,
                    FactoryRegistration { factoryOf: var t } => t,
                    FactoryMethod { returnType: var t } => t,
                    _ => throw new InvalidOperationException(),
                };

                var index = _singleInstanceMethods.Count;
                var singleInstanceFieldName = "_singleInstanceField" + index;
                var name = "GetSingleInstanceField" + index;
                var isAsync = DependencyChecker.RequiresAsync(instanceSource, _containerScope);
                var disposeFieldName = "_disposeAction" + index;
                var lockName = "_lock" + index;
                singleInstanceMethod = (name, isAsync, disposeFieldName, lockName);
                _singleInstanceMethods.Add(instanceSource, singleInstanceMethod);
                _containerMembersSource.Append("private " + type.FullName() + " " + singleInstanceFieldName + ";");
                _containerMembersSource.Append("private global::System.Threading.SemaphoreSlim _lock" + index + "=new global::System.Threading.SemaphoreSlim(1);");
                _containerMembersSource.Append((_implementsAsyncContainer
                    ? "private global::System.Func<global::System.Threading.Tasks.ValueTask>"
                    : "private global::System.Action") + " _disposeAction" + index + ";");
                var methodSource = new StringBuilder();
                methodSource.Append(isAsync ? "private async " : "private ");
                methodSource.Append(isAsync ? _wellKnownTypes.valueTask1.Construct(type).FullName() : type.FullName());
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
                var variableName = CreateVariable(
                    instanceSource,
                    methodSource,
                    _containerScope,
                    isSingleInstanceCreation: true,
                    disposeAsynchronously: _implementsAsyncContainer,
                    orderOfCreation: out var orderOfCreation);
                methodSource.Append("this.");
                methodSource.Append(singleInstanceFieldName);
                methodSource.Append("=");
                methodSource.Append(variableName);
                methodSource.Append(";");
                methodSource.Append("this.");
                methodSource.Append(disposeFieldName);
                methodSource.Append("=");
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

        private string CreateVariable(
            InstanceSource target,
            StringBuilder methodSource,
            InstanceSourcesScope instanceSourcesScope,
            bool isSingleInstanceCreation,
            bool disposeAsynchronously,
            out List<(string variableName, string? disposeActionsName, InstanceSource source)> orderOfCreation)
        {
            _cancellationToken.ThrowIfCancellationRequested();
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
                            methodSource.Append(")");
                            GenerateMemberAccess(methodSource, field);
                            methodSource.Append(").");
                            methodSource.Append(isAsync
                                ? nameof(IAsyncInstanceProvider<object>.GetAsync)
                                : nameof(IInstanceProvider<object>.Get));
                            methodSource.Append("();");
                            break;
                        case { scope: Scope.SingleInstance } registration
                            when !(isSingleInstanceCreation && ReferenceEquals(outerTarget, target)):
                            {
                                var (name, isAsync) = GetSingleInstanceMethod(registration);
                                methodSource.Append("var ");
                                methodSource.Append(variableName);
                                methodSource.Append(isAsync ? "=await " : "=");
                                methodSource.Append(name);
                                methodSource.Append("();");
                            }
                            break;
                        case FactoryRegistration(var factoryType, var factoryOf, var scope, var isAsync) registration:
                            var factory = CreateVariableInternal(_containerScope[factoryType], instanceSourcesScope);
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
                            {
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
                                        ? _wellKnownTypes.iRequiresAsyncInitialization.FullName()
                                        : _wellKnownTypes.iRequiresInitialization.FullName());
                                    methodSource.Append(")");
                                    methodSource.Append(variableName);
                                    methodSource.Append(").");
                                    methodSource.Append(isAsync
                                        ? nameof(IRequiresAsyncInitialization.InitializeAsync)
                                        : nameof(IRequiresInitialization.Initialize));
                                    methodSource.Append("();");
                                }

                                break;
                            }
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
                                var variable = CreateVariable(
                                    instanceSourcesScope[returnType],
                                    methodSource,
                                    instanceSourcesScope,
                                    isSingleInstanceCreation: false,
                                    disposeAsynchronously: disposeAsynchronously,
                                    orderOfCreation: out var delegateOrderOfCreation);
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
                        case FactoryMethod(var method, var returnType, var _, var isAsync) registration:
                            {
                                var variableSource = new StringBuilder();
                                variableSource.Append("var ");
                                variableSource.Append(variableName);
                                variableSource.Append(isAsync ? "=await " : "=");
                                GenerateMemberAccess(variableSource, method);
                                variableSource.Append("(");
                                for (int i = 0; i < method.Parameters.Length; i++)
                                {
                                    if (i != 0)
                                    {
                                        variableSource.Append(",");
                                    }
                                    IParameterSymbol? parameter = method.Parameters[i];
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

                                break;
                            }
                    }

                }
                orderOfCreationTemp.Add((variableName, disposeActionsName, target));
                return variableName;
            }
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

            methodSource.Append(".");
            methodSource.Append(member.Name);
        }

        private void GenerateDisposeCode(
            StringBuilder methodSource,
            List<(string variableName, string? disposeActionsName, InstanceSource source)> orderOfCreation,
            bool isAsync,
            InstanceSource? singleInstanceTarget)
        {
            for (int i = orderOfCreation.Count - 1; i >= 0; i--)
            {
                var (variableName, disposeActionName, source) = orderOfCreation[i];
                switch (source)
                {
                    case { scope: Scope.SingleInstance } when !source.Equals(singleInstanceTarget):
                        break;
                    case FactoryRegistration:
                    case FactoryMethod:
                        if (isAsync)
                        {
                            methodSource.Append("await ");
                        }
                        methodSource.Append(_wellKnownTypes.helpers.FullName());
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
                        methodSource.Append(")");
                        GenerateMemberAccess(methodSource, field);
                        methodSource.Append(").");
                        methodSource.Append(isAsyncInstanceProvider
                            ? nameof(IAsyncInstanceProvider<object>.ReleaseAsync)
                            : nameof(IInstanceProvider<object>.Release));
                        methodSource.Append("(");
                        methodSource.Append(variableName);
                        methodSource.Append(");");
                        break;
                    case Registration { type: var type }:
                        var isAsyncDisposable = type.AllInterfaces.Contains(_wellKnownTypes.iAsyncDisposable);
                        if (isAsync && isAsyncDisposable)
                        {
                            methodSource.Append("await ((");
                            methodSource.Append(_wellKnownTypes.iAsyncDisposable.FullName());
                            methodSource.Append(")");
                            methodSource.Append(variableName);
                            methodSource.Append(")." + nameof(IAsyncDisposable.DisposeAsync) + "(");
                            methodSource.Append(");");
                        }
                        else if (type.AllInterfaces.Contains(_wellKnownTypes.iDisposable))
                        {
                            methodSource.Append("((");
                            methodSource.Append(_wellKnownTypes.iDisposable.FullName());
                            methodSource.Append(")");
                            methodSource.Append(variableName);
                            methodSource.Append(")." + nameof(IDisposable.Dispose) + "(");
                            methodSource.Append(");");
                        }
                        else if (isAsyncDisposable)
                        {
                            _reportDiagnostic(WarnIAsyncDisposableInSynchronousResolution(
                                type,
                                _container,
                                _cancellationToken));
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

        private void GenerateDisposeMethods(bool anySync, bool anyAsync, StringBuilder file)
        {
            file.Append(@"private int _disposed = 0; private bool Disposed => _disposed != 0;");
            var singleInstanceMethodsDisposalOrderings = _singleInstanceMethods is null
                ? Enumerable.Empty<(string name, bool isAsync, string disposeFieldName, string lockName)>()
                : DependencyChecker.GetPartialOrderingOfSingleInstanceDependencies(_containerScope, _singleInstanceMethods.Keys.ToHashSet()).Select(x => _singleInstanceMethods[x]);

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

        void ThrowObjectDisposedException(StringBuilder methodSource)
        {
            methodSource.Append("throw new ");
            methodSource.Append(_wellKnownTypes.objectDisposedException.FullName());
            methodSource.Append("(nameof(");
            methodSource.Append(_container.Name);
            methodSource.Append("));");
        }

        private static Diagnostic WarnIAsyncDisposableInSynchronousResolution(ITypeSymbol type, INamedTypeSymbol container, CancellationToken cancellationToken)
        {
            return Diagnostic.Create(
                new DiagnosticDescriptor(
                    "SI1301",
                    "Cannot call asynchronous dispose for Type in implementation of synchronous container",
                    "Cannot call asynchronous dispose for '{0}' in implementation of synchronous container",
                    "StrongInject",
                    DiagnosticSeverity.Warning,
                    isEnabledByDefault: true),
                container.DeclaringSyntaxReferences.FirstOrDefault()?.GetSyntax(cancellationToken).GetLocation() ?? Location.None,
                type);
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
                    (FactoryMethod mX, FactoryMethod mY) => mX.method.Equals(mY.method, SymbolEqualityComparer.Default),
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
                    FactoryMethod m => 23 + m.method.GetHashCode(),
                    _ => throw new InvalidOperationException("This location is thought to be unreachable"),
                };
            }
        }
    }
}
