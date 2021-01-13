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

        protected override void AfterVisit(InstanceSource source, State state)
        {
            if (ReferenceEquals(state.InstanceSourcesScope, _containerScope) || ReferenceEquals(state.PreviousScope, _containerScope))
                _visited.Add(source);
        }

        protected override bool ShouldVisitBeforeUpdateState(InstanceSource? source, State state)
        {
            if (source is null)
                return false;
            if (ReferenceEquals(state.InstanceSourcesScope, _containerScope) && _visited.Contains(source))
                return false;
            return true;
        }

        protected override bool ShouldVisitAfterUpdateState(InstanceSource source, State state)
        {
            if (ReferenceEquals(state.InstanceSourcesScope, _containerScope) && !ReferenceEquals(state.PreviousScope, _containerScope) && _visited.Contains(source))
                return false;
            return true;
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
        }
    }
}
