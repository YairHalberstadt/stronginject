using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;

namespace StrongInject.Generator
{
    internal static class Extensions
    {
        public static HashSet<T> ToHashSet<T>(this IEnumerable<T> @this) => new HashSet<T>(@this);
        public static HashSet<T> ToHashSet<T>(this IEnumerable<T> @this, IEqualityComparer<T> equalityComparer) => new HashSet<T>(@this, equalityComparer);
        public static void Deconstruct<TKey, TValue>(this KeyValuePair<TKey, TValue> @this, out TKey key, out TValue value)
            => (key, value) = (@this.Key, @this.Value);
        public static IEnumerable<(T value, int index)> WithIndex<T>(this ImmutableArray<T> @this)
        {
            for (var i = 0; i < @this.Length; i++)
            {
                var x = @this[i];
                yield return (x, i);
            }
        }
    }
}
