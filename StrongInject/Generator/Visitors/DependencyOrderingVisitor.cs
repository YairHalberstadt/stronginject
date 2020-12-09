using Microsoft.CodeAnalysis;
using System;
using System.Collections.Generic;

namespace StrongInject.Generator.Visitors
{
    internal class DependencyOrderingVisitor : BaseVisitor<DependencyOrderingVisitor.State>
    {
        private readonly Dictionary<InstanceSource, string> _existingVariables = new();
        private int _variableCount = 0;
        private readonly InstanceSource _target;
        private readonly InstanceSourcesScope _containerScope;
        private readonly bool _isSingleInstanceCreation;
        private readonly bool _isAsyncContext;
        private readonly static List<(string? name, InstanceSource? source)> _emptyList = new();
        private readonly List<(InstanceSource source, string assignedName, List<(string? name, InstanceSource? source)> dependencies)> _order = new();

        private DependencyOrderingVisitor(
            InstanceSource target,
            InstanceSourcesScope containerScope,
            bool isSingleInstanceCreation,
            bool isAsyncContext,
            List<(string, InstanceSource)>? singleInstanceVariablesInScope)
        {
            if (singleInstanceVariablesInScope != null)
            {
                foreach (var (name, source) in singleInstanceVariablesInScope)
                {
                    _existingVariables.Add(source, name);
                }
            }
            _target = target;
            _containerScope = containerScope;
            _isSingleInstanceCreation = isSingleInstanceCreation;
            _isAsyncContext = isAsyncContext;
        }

        public static List<(InstanceSource source, string assignedName, List<(string? name, InstanceSource? source)> dependencies)> OrderDependencies(
            InstanceSource target,
            InstanceSourcesScope currentScope,
            InstanceSourcesScope containerScope,
            bool isSingleInstanceCreation,
            bool isAsyncContext,
            List<(string, InstanceSource)>? singleInstanceVariablesInScope, out string targetName)
        {
            var visitor = new DependencyOrderingVisitor(target, containerScope, isSingleInstanceCreation, isAsyncContext, singleInstanceVariablesInScope);
            var state = new State { InstanceSourcesScope = currentScope, Dependencies = new() };
            visitor.VisitCore(target, state);
            targetName = state.Dependencies[0].name!;
            return visitor._order;
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
                _order.Add((source, name, _emptyList));
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
            _order.Add((source, state.Name, state.Dependencies));
            if (source.Scope != Scope.InstancePerDependency)
                _existingVariables.Add(source, state.Name);
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
