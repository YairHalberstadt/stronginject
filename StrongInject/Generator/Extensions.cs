using System.Collections.Generic;

namespace StrongInject.Generator
{
    internal static class Extensions
    {
        public static HashSet<T> ToHashSet<T>(this IEnumerable<T> @this) => new HashSet<T>(@this);
        public static HashSet<T> ToHashSet<T>(this IEnumerable<T> @this, IEqualityComparer<T> equalityComparer) => new HashSet<T>(@this, equalityComparer);
        public static void Deconstruct<TKey, TValue>(this KeyValuePair<TKey, TValue> @this, out TKey key, out TValue value)
            => (key, value) = (@this.Key, @this.Value);
    }
}
