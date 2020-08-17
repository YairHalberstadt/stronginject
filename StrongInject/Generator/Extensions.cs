using Microsoft.CodeAnalysis;
using System;
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

        public static void CreateOrUpdate<TKey, TValue, TParam>(this Dictionary<TKey, TValue> dic, TKey key, TParam param, Func<TKey, TParam, TValue> create, Func<TKey, TParam, TValue, TValue> update)
        {
            dic[key] = dic.TryGetValue(key, out var existing)
                ? update(key, param, existing)
                : create(key, param);
        }

        public static void WithInstanceSource(this Dictionary<ITypeSymbol, InstanceSources> instanceSources, ITypeSymbol type, InstanceSource instanceSource)
        {
            instanceSources.CreateOrUpdate(
                type,
                instanceSource,
                (_, instanceSource) => InstanceSources.Create(instanceSource),
                (_, instanceSource, existing) => existing.Add(instanceSource));
        }
    }
}
