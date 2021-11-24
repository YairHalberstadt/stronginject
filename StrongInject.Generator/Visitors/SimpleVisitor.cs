using System.Collections.Generic;

namespace StrongInject.Generator.Visitors
{
    internal abstract class SimpleVisitor : BaseVisitor<SimpleVisitor.State>
    {
        private readonly HashSet<InstanceSource> _visited = new();
        protected readonly InstanceSourcesScope _containerScope;

        protected SimpleVisitor(InstanceSourcesScope containerScope)
        {
            _containerScope = containerScope;
        }

        protected override bool ShouldVisitBeforeUpdateState(InstanceSource? source, State state)
        {
            if (source is null)
                return false;
            return true;
        }

        protected override void UpdateState(InstanceSource source, ref State state)
        {
            if (source.Scope == Scope.SingleInstance)
            {
                state.CurrentlyVisitingDelegates = new();
            }
            base.UpdateState(source, ref state);
        }

        protected override bool ShouldVisitAfterUpdateState(InstanceSource source, State state)
        {
            if ((ReferenceEquals(state.InstanceSourcesScope, _containerScope)
                 || ReferenceEquals(state.PreviousScope, _containerScope))
                && !_visited.Add(source))
                return false;
            if (source is DelegateSource ds && !state.CurrentlyVisitingDelegates.Add(ds))
                return false;
            return true;
        }

        protected override void AfterVisit(InstanceSource source, State state)
        {
            if (source is DelegateSource ds)
                state.CurrentlyVisitingDelegates.Remove(ds);
            base.AfterVisit(source, state);
        }

        public struct State : IState
        {
            public State(InstanceSourcesScope instanceSourcesScope)
            {
                _instanceSourcesScope = instanceSourcesScope;
                CurrentlyVisitingDelegates = new();
                PreviousScope = null;
            }
            
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

            public HashSet<DelegateSource> CurrentlyVisitingDelegates { get; set; }
        }
    }
}
