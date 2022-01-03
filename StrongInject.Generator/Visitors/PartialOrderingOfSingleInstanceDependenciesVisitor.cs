using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;

namespace StrongInject.Generator.Visitors
{
    internal class PartialOrderingOfSingleInstanceDependenciesVisitor : SimpleVisitor
    {
        private List<InstanceSource>? _results;
        private readonly HashSet<InstanceSource> _alreadyAdded = new();

        private PartialOrderingOfSingleInstanceDependenciesVisitor(InstanceSourcesScope containerScope, CancellationToken cancellationToken) : base(containerScope, cancellationToken)
        {
        }

        public static IEnumerable<InstanceSource> GetPartialOrdering(InstanceSourcesScope containerScope, HashSet<InstanceSource> usedSingleInstanceSources, CancellationToken cancellationToken)
        {
            var visitor = new PartialOrderingOfSingleInstanceDependenciesVisitor(containerScope, cancellationToken);
            IEnumerable<InstanceSource> results = Array.Empty<InstanceSource>();
            foreach (var source in usedSingleInstanceSources)
            {
                visitor.VisitCore(source, new State(containerScope));
                if (visitor._results is { } visitResults)
                    results = visitResults.Concat(results);
                visitor._results = null;
            }
            return results;
        }

        protected override bool ShouldVisitBeforeUpdateState(InstanceSource? source, State state)
        {
            if (source is null)
                return false;
            if (source.Scope == Scope.SingleInstance && source is not (InstanceFieldOrProperty or ForwardedInstanceSource))
            {
                if (_alreadyAdded.Add(source))
                {
                    (_results ??= new()).Add(source);
                }
                else
                {
                    return false;
                }
            }
            return base.ShouldVisitBeforeUpdateState(source, state);
        }
    }
}
