using System.Collections.Generic;
using System.Threading;

namespace StrongInject.Generator.Visitors
{
    internal class RequiresAsyncChecker
    {
        private readonly InstanceSourcesScope _containerScope;
        private readonly CancellationToken _cancellationToken;
        private readonly Dictionary<InstanceSource, bool> _cache = new();

        public RequiresAsyncChecker(InstanceSourcesScope containerScope, CancellationToken cancellationToken)
        {
            _containerScope = containerScope;
            _cancellationToken = cancellationToken;
        }

        public bool RequiresAsync(InstanceSource source)
        {
            return _cache.GetOrCreate(source, (_containerScope, _cancellationToken), static (i, s) => Visitor.RequiresAsync(i, s._containerScope, s._cancellationToken));
        }
        
        private class Visitor : SimpleVisitor
        {
            private bool _requiresAsync = false;

            private Visitor(InstanceSourcesScope containerScope, CancellationToken cancellationToken) : base(containerScope, cancellationToken)
            {
            }

            public static bool RequiresAsync(InstanceSource source, InstanceSourcesScope containerScope, CancellationToken cancellationToken)
            {
                var visitor = new Visitor(containerScope, cancellationToken);
                visitor.VisitCore(source, new State(containerScope));
                return visitor._requiresAsync;
            }

            protected override bool ShouldVisitBeforeUpdateState(InstanceSource? source, State state)
            {
                if (source is null)
                    return false;
                if (source is DelegateSource { IsAsync: true })
                    return false;
                if (source.IsAsync)
                {
                    _requiresAsync = true;
                    ExitFast();
                    return false;
                }

                return base.ShouldVisitBeforeUpdateState(source, state);
            }
        }
    }
}
