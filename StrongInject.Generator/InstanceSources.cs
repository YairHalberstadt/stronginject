using System.Collections;
using System.Collections.Generic;

namespace StrongInject.Generator
{
    internal class InstanceSources : IReadOnlyCollection<InstanceSource>
    {
        public InstanceSources(InstanceSource? best, ImmutableSetInInsertionOrder<InstanceSource> others)
        {
            Best = best;
            _others = others;
        }

        private readonly ImmutableSetInInsertionOrder<InstanceSource> _others;
        public InstanceSource? Best { get; }

        public int Count => Best is null ? _others.Count : _others.Count + 1;

        public static InstanceSources Create(InstanceSource best) => new InstanceSources(best, ImmutableSetInInsertionOrder<InstanceSource>.Empty);

        public InstanceSources Add(InstanceSource instanceSource)
        {
            if (Best is null)
            {
                return new InstanceSources(null, _others.Add(instanceSource));
            }
            else
            {
                return new InstanceSources(null, _others.Add(instanceSource).Add(Best));
            }
        }

        public InstanceSources Merge(InstanceSources instanceSources)
        {
            if (Best?.Equals(instanceSources.Best) ?? false)
            {
                return new InstanceSources(Best, _others.Union(instanceSources._others));
            }

            var others = _others.Union(instanceSources._others);
            if (Best is not null)
            {
                others = others.Add(Best);
            }
            if (instanceSources.Best is not null)
            {
                others = others.Add(instanceSources.Best);
            }
            return new InstanceSources(null, others);
        }

        public InstanceSources MergeWithPreferred(InstanceSources instanceSources)
        {
            var others = _others.Union(instanceSources._others);
            if (instanceSources.Best is not null)
            {
                others = others.Remove(instanceSources.Best);
            }
            if (Best is not null && !Best.Equals(instanceSources.Best))
            {
                others = others.Add(Best);
            }
            return new InstanceSources(instanceSources.Best, others);
        }

        public IEnumerator<InstanceSource> GetEnumerator()
        {
            if (Best is not null)
            {
                yield return Best;
            }

            foreach (var other in _others)
            {
                yield return other;
            }
        }

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }
}
