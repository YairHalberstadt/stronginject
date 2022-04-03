using System.Collections.Generic;
using System.Threading;

namespace StrongInject.Generator.Visitors
{
    /// <summary>
    /// Calculates which single instance variables require an async context to be resolved,
    /// but are used in a sync delegate.
    /// In order to allow them to be resolved, they must be resolved in a parent async scope,
    /// and then captured by the delegate.
    /// </summary>
    internal class SingleInstanceVariablesToCreateEarlyVisitor : SimpleVisitor
    {
        private readonly RequiresAsyncChecker _requiresAsyncChecker;
        private readonly List<InstanceSource> _singleInstanceVariablesToCreateEarly = new();

        private SingleInstanceVariablesToCreateEarlyVisitor(RequiresAsyncChecker requiresAsyncChecker, InstanceSourcesScope containerScope, CancellationToken cancellationToken) : base(containerScope, cancellationToken)
        {
            _requiresAsyncChecker = requiresAsyncChecker;
        }
        
        public static List<InstanceSource> CalculateVariables(RequiresAsyncChecker requiresAsyncChecker, InstanceSource source, InstanceSourcesScope currentScope, InstanceSourcesScope containerScope, CancellationToken cancellationToken)
        {
            var visitor = new SingleInstanceVariablesToCreateEarlyVisitor(requiresAsyncChecker, containerScope, cancellationToken);
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
                if (_requiresAsyncChecker.RequiresAsync(source))
                {
                    _singleInstanceVariablesToCreateEarly.Add(source);
                }
                return false;
            }
            return base.ShouldVisitBeforeUpdateState(source, state);
        }
    }
}
