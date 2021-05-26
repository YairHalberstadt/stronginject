using System.Collections;
using System.Collections.Generic;
using System.Collections.Immutable;

namespace StrongInject.Generator
{
    internal class ImmutableSetInInsertionOrder<T> : IImmutableSet<T> where T : notnull
    {
        private readonly ImmutableDictionary<T, int> _items;
        private readonly ImmutableSortedDictionary<int, T> _insertionOrder;
        private readonly int _totalInserted;

        public static ImmutableSetInInsertionOrder<T> Empty { get; } = new(ImmutableDictionary<T, int>.Empty, ImmutableSortedDictionary<int, T>.Empty, 0);

        public int Count => _items.Count;

        private ImmutableSetInInsertionOrder(ImmutableDictionary<T, int> items, ImmutableSortedDictionary<int, T> insertionOrder, int totalInserted)
        {
            _items = items;
            _insertionOrder = insertionOrder;
            _totalInserted = totalInserted;
        }

        public bool Contains(T value) => _items.ContainsKey(value);

        public ImmutableSetInInsertionOrder<T> Add(T value)
        {
            if (_items.ContainsKey(value))
            {
                return this;
            }
            return new(_items.Add(value, _totalInserted), _insertionOrder.Add(_totalInserted, value), _totalInserted + 1);
        }

        public ImmutableSetInInsertionOrder<T> Remove(T value)
        {
            if (_items.TryGetValue(value, out var insertionIndex))
            {
                return new(_items.Remove(value), _insertionOrder.Remove(insertionIndex), _totalInserted - 1);
            }
            return this;
        }

        public ImmutableSetInInsertionOrder<T> Intersect(IEnumerable<T> other)
        {
            var intersected = Empty;
            foreach (var item in other)
            {
                if (Contains(item))
                    intersected = intersected.Add(item);
            }
            return intersected;
        }

        public ImmutableSetInInsertionOrder<T> Except(IEnumerable<T> other)
        {
            var updated = this;
            foreach (var item in other)
            {
                updated = updated.Remove(item);
            }
            return updated;
        }

        public ImmutableSetInInsertionOrder<T> SymmetricExcept(IEnumerable<T> other)
        {
            var updated = this;
            foreach (var item in other)
            {
                if (Contains(item))
                {
                    updated = updated.Remove(item);
                }
                else
                {
                    updated = updated.Add(item);
                }
            }
            return updated;
        }

        public ImmutableSetInInsertionOrder<T> Union(IEnumerable<T> other)
        {
            var updated = this;
            foreach (var item in other)
            {
                updated = updated.Add(item);
            }
            return updated;
        }

        public bool TryGetValue(T equalValue, out T actualValue) => _items.TryGetKey(equalValue, out actualValue);

        public bool SetEquals(IEnumerable<T> other)
        {
            var otherSet = other.ToHashSet();
            if (Count != otherSet.Count)
            {
                return false;
            }

            foreach (var item in this)
            {
                if (!otherSet.Contains(item))
                    return false;
            }

            return true;
        }

        public bool IsProperSubsetOf(IEnumerable<T> other)
        {
            var otherSet = other.ToHashSet();
            if (Count >= otherSet.Count)
            {
                return false;
            }

            foreach (var item in this)
            {
                if (!otherSet.Contains(item))
                    return false;
            }

            return true;
        }

        public bool IsProperSupersetOf(IEnumerable<T> other)
        {
            var otherSet = other.ToHashSet();
            if (Count <= otherSet.Count)
            {
                return false;
            }

            foreach (var item in otherSet)
            {
                if (!Contains(item))
                    return false;
            }

            return true;
        }

        public bool IsSubsetOf(IEnumerable<T> other)
        {
            var otherSet = other.ToHashSet();
            if (Count > otherSet.Count)
            {
                return false;
            }

            foreach (var item in this)
            {
                if (!otherSet.Contains(item))
                    return false;
            }

            return true;
        }

        public bool IsSupersetOf(IEnumerable<T> other)
        {
            var otherSet = other.ToHashSet();
            if (Count < otherSet.Count)
            {
                return false;
            }

            foreach (var item in otherSet)
            {
                if (!Contains(item))
                    return false;
            }

            return true;
        }

        public bool Overlaps(IEnumerable<T> other)
        {
            foreach (var item in other)
            {
                if (Contains(item))
                {
                    return true;
                }
            }
            return false;
        }

        public ImmutableSetInInsertionOrder<T> Clear() => Empty;

        IImmutableSet<T> IImmutableSet<T>.Clear() => Clear();

        IImmutableSet<T> IImmutableSet<T>.Add(T value) => Add(value);

        IImmutableSet<T> IImmutableSet<T>.Remove(T value) => Remove(value);

        IImmutableSet<T> IImmutableSet<T>.Intersect(IEnumerable<T> other) => Intersect(other);

        IImmutableSet<T> IImmutableSet<T>.Except(IEnumerable<T> other) => Except(other);

        IImmutableSet<T> IImmutableSet<T>.SymmetricExcept(IEnumerable<T> other) => SymmetricExcept(other);

        IImmutableSet<T> IImmutableSet<T>.Union(IEnumerable<T> other) => Union(other);

        public IEnumerator<T> GetEnumerator()
        {
            return _insertionOrder.Values.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }
}
