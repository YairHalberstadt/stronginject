using StrongInject.Plugins;
using System;
using System.Collections.Generic;
using System.Linq;

namespace StrongInject.Generator.Visitors
{
    internal class PartialOrderingOfSingleInstanceDependenciesVisitor : SimpleVisitor
    {
        private List<InstanceSource>? _results;
        private readonly HashSet<InstanceSource> _alreadyAdded = new();

        private PartialOrderingOfSingleInstanceDependenciesVisitor(InstanceSourcesScope containerScope) : base(containerScope)
        {
        }

        public static IEnumerable<InstanceSource> GetPartialOrdering(InstanceSourcesScope containerScope, HashSet<InstanceSource> usedSingleInstanceSources)
        {
            var visitor = new PartialOrderingOfSingleInstanceDependenciesVisitor(containerScope);
            IEnumerable<InstanceSource> results = Array.Empty<InstanceSource>();
            foreach (var source in usedSingleInstanceSources)
            {
                visitor.VisitCore(source, new State { InstanceSourcesScope = containerScope });
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
