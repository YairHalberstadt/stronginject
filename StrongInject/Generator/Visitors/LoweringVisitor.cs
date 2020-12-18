using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;

namespace StrongInject.Generator.Visitors
{
    internal class LoweringVisitor : BaseVisitor<LoweringVisitor.State>
    {
        private readonly Dictionary<InstanceSource, string> _existingVariables = new();
        private int _variableCount = 0;
        private readonly InstanceSource _target;
        private readonly InstanceSourcesScope _containerScope;
        private readonly DisposalLowerer _disposalLowerer;
        private readonly bool _isSingleInstanceCreation;
        private readonly bool _isAsyncContext;
        private readonly List<Operation> _order = new();

        private LoweringVisitor(
            InstanceSource target,
            InstanceSourcesScope containerScope,
            DisposalLowerer disposalLowerer,
            bool isSingleInstanceCreation,
            bool isAsyncContext,
            IEnumerable<KeyValuePair<InstanceSource, string>>? singleInstanceVariablesInScope)
        {
            if (singleInstanceVariablesInScope != null)
            {
                foreach (var (source, name) in singleInstanceVariablesInScope)
                {
                    _existingVariables.Add(source, name);
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
            IEnumerable<KeyValuePair<InstanceSource, string>>? singleInstanceVariablesInScope,
            out string targetName)
        {
            var visitor = new LoweringVisitor(target, containerScope, disposalLowerer, isSingleInstanceCreation, isAsyncContext, singleInstanceVariablesInScope);
            var state = new State { InstanceSourcesScope = currentScope, Dependencies = new() };
            visitor.VisitCore(target, state);
            targetName = state.Dependencies[0].name!;
            return visitor._order.ToImmutableArray();
        }

        protected override bool ShouldVisitBeforeUpdateState(InstanceSource? source, State state)
        {
            if (source is null)
            {
                state.Dependencies.Add((null, null));
                return false;
            }
            if (source.Scope is not Scope.InstancePerDependency && _existingVariables.TryGetValue(source, out var name))
            {
                state.Dependencies.Add((name, source));
                return false;
            }
            if (source is { Scope: Scope.SingleInstance } and not (InstanceFieldOrProperty or ForwardedInstanceSource) && !(ReferenceEquals(_target, source) && _isSingleInstanceCreation))
            {
                name = GenerateName(state);
                var isAsync = RequiresAsyncVisitor.RequiresAsync(source, _containerScope);
                var statement = new SingleInstanceReferenceStatement(name, source, isAsync);
                if (isAsync)
                {
                    var awaitVariableName = GenerateName(state);
                    var awaitStatement = new AwaitStatement(awaitVariableName, name, source.OfType);
                    _order.Add(CreateOperation(statement, state.Name, awaitStatement));
                    _order.Add(CreateOperation(awaitStatement, state.Name));
                    name = awaitVariableName;
                }
                else
                {
                    _order.Add(CreateOperation(statement, state.Name));
                }
                _existingVariables.Add(source, name);
                state.Dependencies.Add((name, source));
                return false;
            }
            if (source is DelegateParameter { Name: var variableName })
            {
                state.Dependencies.Add((variableName, source));
                return false;
            }
            return true;
        }

        private string GenerateName(State state)
        {
            return "_" + state.InstanceSourcesScope.Depth + "_" + _variableCount++;
        }

        protected override void UpdateState(InstanceSource source, ref State state)
        {
            state.Name = GenerateName(state);
            state.Dependencies.Add((state.Name, source));
            state.Dependencies = new();
            base.UpdateState(source, ref state);
        }

        protected override bool ShouldVisitAfterUpdateState(InstanceSource source, State state)
        {
            return true;
        }

        protected override void AfterVisit(InstanceSource source, State state)
        {
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
                var operation = CreateOperation(delegateCreationStatement, state.Name);
                if (operation.Disposal is Disposal.DelegateDisposal { DisposeActionsType: var disposeActionsType })
                {
                    _order.Add(CreateOperation(new DisposeActionsCreationStatement(delegateCreationStatement.DisposeActionsName, disposeActionsType), state.Name));
                }
                _order.Add(operation);
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
                        state.Name);
                    _order.Add(operation);

                    var initializationTaskVariableName = source.IsAsync ? GenerateName(state) : null;

                    if (initializationTaskVariableName is not null)
                    {
                        var awaitStatement = new AwaitStatement(VariableName: null, VariableToAwaitName: initializationTaskVariableName, Type: null);
                        _order.Add(CreateOperation(new InitializationStatement(initializationTaskVariableName, state.Name, source.IsAsync), state.Name, awaitStatement));
                        _order.Add(CreateOperation(awaitStatement, state.Name));
                    }
                    else
                    {
                        _order.Add(CreateOperation(new InitializationStatement(initializationTaskVariableName, state.Name, source.IsAsync), state.Name));
                    }
                }
                else if (source.IsAsync)
                {
                    var taskVariableName = GenerateName(state);
                    var awaitStatement = new AwaitStatement(VariableName: state.Name, VariableToAwaitName: taskVariableName, Type: source.OfType);
                    _order.Add(CreateOperation(
                        new DependencyCreationStatement(
                            taskVariableName,
                            source,
                            state.Dependencies.Select(x => x.name).ToImmutableArray()), state.Name, awaitStatement));
                    _order.Add(CreateOperation(awaitStatement, state.Name));
                }
                else
                {
                    _order.Add(CreateOperation(
                        new DependencyCreationStatement(
                            state.Name,
                            source,
                            state.Dependencies.Select(x => x.name).ToImmutableArray()), state.Name));
                }
            }

            if (source.Scope != Scope.InstancePerDependency)
            {
                _existingVariables.Add(source, state.Name);
            }
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

        private Operation CreateOperation(Statement statement, string variableToDisposeName, AwaitStatement? awaitStatement = null)
        {
            return new Operation(statement, _disposalLowerer.CreateDisposal(statement, variableToDisposeName), awaitStatement);
        }

        public struct State : IState
        {
            private InstanceSourcesScope _instanceSourcesScope;

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
        }
    }
}
