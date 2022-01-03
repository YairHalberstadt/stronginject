using System.Collections.Generic;
using System.Threading;

namespace StrongInject.Generator.Visitors
{
    internal class SingleInstanceVariablesToCreateEarlyVisitor : SimpleVisitor
    {
        private readonly List<InstanceSource> _singleInstanceVariablesToCreateEarly = new();

        private SingleInstanceVariablesToCreateEarlyVisitor(InstanceSourcesScope containerScope, CancellationToken cancellationToken) : base(containerScope, cancellationToken)
        {
        }

        public static List<InstanceSource> CalculateVariables(InstanceSource source, InstanceSourcesScope currentScope, InstanceSourcesScope containerScope, CancellationToken cancellationToken)
        {
            var visitor = new SingleInstanceVariablesToCreateEarlyVisitor(containerScope, cancellationToken);
            visitor.VisitCore(source, new State(currentScope));
            return visitor._singleInstanceVariablesToCreateEarly;
        }

        protected override bool ShouldVisitBeforeUpdateState(InstanceSource? source, State state)
        {
            if (source is null)
                return false;
            if (source is DelegateSource { IsAsync: true })
                return false;
            if (source.Scope == Scope.SingleInstance)
            {
                if (RequiresAsyncVisitor.RequiresAsync(source, _containerScope, _cancellationToken))
                {
                    _singleInstanceVariablesToCreateEarly.Add(source);
                }
                return false;
            }
            return base.ShouldVisitBeforeUpdateState(source, state);
        }
    }
}
