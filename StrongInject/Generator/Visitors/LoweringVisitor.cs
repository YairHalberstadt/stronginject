using Microsoft.CodeAnalysis;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;

namespace StrongInject.Generator.Visitors
{
    internal class LoweringVisitor : BaseVisitor<LoweringVisitor.State>
    {
        private readonly Dictionary<InstanceSource, (Operation operation, string name)> _existingVariables = new();
        private int _variableCount = 0;
        private readonly InstanceSource _target;
        private readonly InstanceSourcesScope _containerScope;
        private readonly DisposalLowerer _disposalLowerer;
        private readonly bool _isSingleInstanceCreation;
        private readonly bool _isAsyncContext;
        private readonly List<Operation> _order = new();
        private static readonly List<Operation> _emptyList = new();

        private LoweringVisitor(
            InstanceSource target,
            InstanceSourcesScope containerScope,
            DisposalLowerer disposalLowerer,
            bool isSingleInstanceCreation,
            bool isAsyncContext,
            IEnumerable<KeyValuePair<InstanceSource, (Operation operation, string name)>>? singleInstanceVariablesInScope)
        {
            if (singleInstanceVariablesInScope != null)
            {
                foreach (var (source, value) in singleInstanceVariablesInScope)
                {
                    _existingVariables.Add(source, value);
                }
            }
            _target = target;
            _containerScope = containerScope;
            _disposalLowerer = disposalLowerer;
            _isSingleInstanceCreation = isSingleInstanceCreation;
            _isAsyncContext = isAsyncContext;
        }

        public static ImmutableArray<Operation> LowerResolution(
            InstanceSource target,
            InstanceSourcesScope containerScope,
            DisposalLowerer disposalLowerer,
            bool isSingleInstanceCreation,
            bool isAsyncContext,
            out string targetName) => LowerResolution(
                target,
                containerScope,
                containerScope,
                disposalLowerer,
                isSingleInstanceCreation,
                isAsyncContext,
                null,
                out targetName);

        private static ImmutableArray<Operation> LowerResolution(
            InstanceSource target,
            InstanceSourcesScope currentScope,
            InstanceSourcesScope containerScope,
            DisposalLowerer disposalLowerer,
            bool isSingleInstanceCreation,
            bool isAsyncContext,
            IEnumerable<KeyValuePair<InstanceSource, (Operation operation, string name)>>? singleInstanceVariablesInScope,
            out string targetName)
        {
            var visitor = new LoweringVisitor(target, containerScope, disposalLowerer, isSingleInstanceCreation, isAsyncContext, singleInstanceVariablesInScope);
            var state = new State { InstanceSourcesScope = currentScope, Dependencies = new(), OperationDependencies = new() };
            visitor.VisitCore(target, state);
            targetName = state.Dependencies[0].name!;
            return isAsyncContext ? Order(visitor._order) : visitor._order.ToImmutableArray();
        }

        protected override bool ShouldVisitBeforeUpdateState(InstanceSource? source, State state)
        {
            if (source is null)
            {
                state.Dependencies.Add((null, null));
                return false;
            }
            if (source.Scope is not Scope.InstancePerDependency && _existingVariables.TryGetValue(source, out var value))
            {
                state.Dependencies.Add((value.name, source));
                state.OperationDependencies.Add(value.operation);
                return false;
            }
            if (source is { Scope: Scope.SingleInstance } and not (InstanceFieldOrProperty or ForwardedInstanceSource) && !(ReferenceEquals(_target, source) && _isSingleInstanceCreation))
            {
                var name = GenerateName(source, state);
                var isAsync = RequiresAsyncVisitor.RequiresAsync(source, _containerScope);
                var statement = new SingleInstanceReferenceStatement(name, source, isAsync);
                Operation targetOperation;
                if (isAsync)
                {
                    var awaitVariableName = GenerateName(source, state);
                    var awaitStatement = new AwaitStatement(awaitVariableName, name, source.OfType);
                    var referenceOperation = CreateOperation(statement, state.Name, _emptyList, awaitStatement);
                    _order.Add(referenceOperation);
                    targetOperation = CreateOperation(awaitStatement, state.Name, new() { referenceOperation });
                    _order.Add(targetOperation);
                    name = awaitVariableName;
                }
                else
                {
                    targetOperation = CreateOperation(statement, state.Name, _emptyList);
                    _order.Add(targetOperation);
                }
                _existingVariables.Add(source, (targetOperation, name));
                state.Dependencies.Add((name, source));
                state.OperationDependencies.Add(targetOperation);
                return false;
            }
            if (source is DelegateParameter { Name: var variableName })
            {
                state.Dependencies.Add((variableName, source));
                return false;
            }
            return true;
        }

        private string GenerateName(InstanceSource source, State state)
        {
            return source.OfType.ToLowerCaseIdentifier("") + "_" + state.InstanceSourcesScope.Depth + "_" + _variableCount++;
        }

        protected override void UpdateState(InstanceSource source, ref State state)
        {
            state.Name = GenerateName(source, state);
            state.Dependencies.Add((state.Name, source));
            state.Dependencies = new();
            state.OperationDependencies = new();
            base.UpdateState(source, ref state);
        }

        protected override bool ShouldVisitAfterUpdateState(InstanceSource source, State state)
        {
            return true;
        }

        protected override void AfterVisit(InstanceSource source, State state)
        {
            Operation targetOperation;
            if (source is DelegateSource delegateSource)
            {
                var order = LowerResolution(
                    target: GetInstanceSource(delegateSource.ReturnType, state, null)!,
                    currentScope: state.InstanceSourcesScope,
                    containerScope: _containerScope,
                    disposalLowerer: _disposalLowerer,
                    isSingleInstanceCreation: false,
                    isAsyncContext: delegateSource.IsAsync,
                    singleInstanceVariablesInScope: _existingVariables.Where(x => x.Key.Scope == Scope.SingleInstance),
                    targetName: out var targetName);
                var delegateCreationStatement = new DelegateCreationStatement(state.Name, delegateSource, order, targetName);
                targetOperation = CreateOperation(delegateCreationStatement, state.Name, state.OperationDependencies);
                if (targetOperation.Disposal is Disposal.DelegateDisposal { DisposeActionsType: var disposeActionsType })
                {
                    var disposeActionsOperation = CreateOperation(new DisposeActionsCreationStatement(delegateCreationStatement.DisposeActionsName, disposeActionsType), state.Name, _emptyList);
                    state.OperationDependencies.Add(disposeActionsOperation);
                    _order.Add(disposeActionsOperation);
                }
                _order.Add(targetOperation);
            }
            else
            {
                var requiresInitialization = source is
                    Registration { RequiresInitialization: true }
                    or WrappedDecoratorInstanceSource { Decorator: DecoratorRegistration { RequiresInitialization: true } };

                if (requiresInitialization)
                {
                    var operation = CreateOperation(
                        new DependencyCreationStatement(
                            state.Name,
                            source,
                            state.Dependencies.Select(x => x.name).ToImmutableArray()),
                        state.Name,
                        state.OperationDependencies);
                    _order.Add(operation);

                    var initializationTaskVariableName = source.IsAsync ? GenerateName(source, state) : null;

                    if (initializationTaskVariableName is not null)
                    {
                        var awaitStatement = new AwaitStatement(VariableName: null, VariableToAwaitName: initializationTaskVariableName, Type: null);
                        var initializationOperation = CreateOperation(new InitializationStatement(initializationTaskVariableName, state.Name, source.IsAsync), state.Name, new() { operation }, awaitStatement);
                        _order.Add(initializationOperation);
                        targetOperation = CreateOperation(awaitStatement, state.Name, new() { initializationOperation });
                        _order.Add(targetOperation);
                    }
                    else
                    {
                        targetOperation = CreateOperation(new InitializationStatement(initializationTaskVariableName, state.Name, source.IsAsync), state.Name, new() { operation });
                        _order.Add(targetOperation);
                    }
                }
                else if (source.IsAsync)
                {
                    var taskVariableName = GenerateName(source, state);
                    var awaitStatement = new AwaitStatement(VariableName: state.Name, VariableToAwaitName: taskVariableName, Type: source.OfType);
                    var operation = CreateOperation(
                        new DependencyCreationStatement(
                            taskVariableName,
                            source,
                            state.Dependencies.Select(x => x.name).ToImmutableArray()), state.Name, state.OperationDependencies, awaitStatement);
                    _order.Add(operation);
                    targetOperation = CreateOperation(awaitStatement, state.Name, new() { operation });
                    _order.Add(targetOperation);
                }
                else
                {
                    targetOperation = CreateOperation(
                        new DependencyCreationStatement(
                            state.Name,
                            source,
                            state.Dependencies.Select(x => x.name).ToImmutableArray()),
                        state.Name,
                        state.OperationDependencies);
                    _order.Add(targetOperation);
                }
            }

            if (source.Scope != Scope.InstancePerDependency)
            {
                _existingVariables.Add(source, (targetOperation, state.Name));
            }
            state.ParentOperationDependencies.Add(targetOperation);
        }

        public override void Visit(DelegateSource delegateSource, State state)
        {
            if (_isAsyncContext && !delegateSource.IsAsync)
            {
                state.InstanceSourcesScope = state.PreviousScope!;
                foreach (var dependency in SingleInstanceVariablesToCreateEarlyVisitor.CalculateVariables(delegateSource, state.InstanceSourcesScope, _containerScope))
                {
                    VisitCore(dependency, state);
                }
            }
        }

        private Operation CreateOperation(Statement statement, string variableToDisposeName, List<Operation> dependencies, AwaitStatement? awaitStatement = null)
        {
            var disposal = _disposalLowerer.CreateDisposal(statement, variableToDisposeName);
            var canDisposeLocally = disposal is not null && (!disposal.IsAsync || _isAsyncContext);
            return new Operation(statement, disposal, dependencies, canDisposeLocally, awaitStatement);
        }

        public struct State : IState
        {
            private InstanceSourcesScope _instanceSourcesScope;
            private List<Operation> _operationDependencies;

            public InstanceSourcesScope? PreviousScope { get; private set; }
            public InstanceSourcesScope InstanceSourcesScope
            {
                get => _instanceSourcesScope;
                set
                {
                    PreviousScope = _instanceSourcesScope;
                    _instanceSourcesScope = value;
                }
            }
            public string Name { get; set; }
            public List<(string? name, InstanceSource? source)> Dependencies { get; set; }
            public List<Operation> ParentOperationDependencies { get; private set; }
            public List<Operation> OperationDependencies
            {
                get => _operationDependencies;
                set
                {
                    ParentOperationDependencies = _operationDependencies;
                    _operationDependencies = value;
                }
            }
        }

        private static ImmutableArray<Operation> Order(List<Operation> order)
        {
            var builder = ImmutableArray.CreateBuilder<Operation>(order.Count);
            var ordered = new HashSet<Operation>();
            while (order.Count > 0)
            {
                for (var i = 0; i < order.Count; i++)
                {
                    var operation = order[i];

                    if (operation.Statement is InitializationStatement { IsAsync: true }
                        or SingleInstanceReferenceStatement { IsAsync: true }
                        or DependencyCreationStatement
                        {
                            Source:
                            FactoryMethod { IsAsync: true }
                            or WrappedDecoratorInstanceSource { Decorator: DecoratorFactoryMethod { IsAsync: true } }
                            or FactorySource { IsAsync: true }
                        })
                    {
                        if (operation.Dependencies.All(x => ordered.Contains(x)))
                        {
                            builder.Add(operation);
                            ordered.Add(operation);
                            order.RemoveAt(i);
                            i--;
                        }
                    }
                }

                bool found = false;
                for (var i = 0; i < order.Count; i++)
                {
                    var operation = order[i];

                    if (operation.Statement is not AwaitStatement && operation.Dependencies.All(x => ordered.Contains(x)))
                    {
                        builder.Add(operation);
                        ordered.Add(operation);
                        order.RemoveAt(i);
                        found = true;
                        break;
                    }
                }

                if (found)
                    continue;

                Operation longestPathSoFar = default!;
                int longestPathLengthSoFar = 0;
                FindLongestPath(order[order.Count - 1], 0);
                builder.Add(longestPathSoFar);
                ordered.Add(longestPathSoFar);
                order.Remove(longestPathSoFar);

                void FindLongestPath(Operation operation, int pathLength)
                {
                    if (operation.Statement is AwaitStatement)
                    {
                        pathLength++;
                        if (pathLength > longestPathLengthSoFar)
                        {
                            longestPathSoFar = operation;
                            longestPathLengthSoFar = pathLength;
                        }
                    }

                    foreach (var dependency in operation.Dependencies)
                    {
                        if (!ordered.Contains(dependency))
                        {
                            FindLongestPath(dependency, pathLength);
                        }
                    }
                }
            }

            return builder.MoveToImmutable();
        }
    }
}
