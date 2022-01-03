using System.Collections.Generic;
using System.Threading;

namespace StrongInject.Generator.Visitors
{
    internal class RequiresAsyncVisitor : SimpleVisitor
    {
        private bool _requiresAsync = false;

        private RequiresAsyncVisitor(InstanceSourcesScope containerScope, CancellationToken cancellationToken) : base(containerScope, cancellationToken)
        {
        }

        public static bool RequiresAsync(InstanceSource source, InstanceSourcesScope containerScope, CancellationToken cancellationToken)
        {
            var visitor = new RequiresAsyncVisitor(containerScope, cancellationToken);
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
