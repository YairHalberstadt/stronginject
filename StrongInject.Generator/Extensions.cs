using Microsoft.CodeAnalysis;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;

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

        public static TValue GetOrCreate<TKey, TValue>(this Dictionary<TKey, TValue> dic, TKey key, Func<TKey, TValue> create)
        {
            if (!dic.TryGetValue(key, out var value))
            {
                dic[key] = value = create(key);
            }
            return value;
        }
        
        public static bool TryAdd<TKey, TValue>(this Dictionary<TKey, TValue> dic, TKey key, TValue value, out TValue? existingValue)
        {
            if (!dic.TryGetValue(key, out existingValue))
            {
                dic[key] = value;
                return true;
            }
            return false;
        }
        
        public static void AddOrUpdate<TKey, TValue>(this Dictionary<TKey, TValue> dic, TKey key, TValue value, out TValue existingValue)
        {
            dic.TryGetValue(key, out existingValue);
            dic[key] = value;
        }

        public static TValue? GetValueOrDefault<TKey, TValue>(this IReadOnlyDictionary<TKey, TValue> dic, TKey key, TValue? defaultValue = default)
        {
            if (dic.TryGetValue(key, out var value))
            {
                return value;
            }
            return defaultValue;
        }

        public static void WithInstanceSource(this Dictionary<ITypeSymbol, InstanceSources> instanceSources, InstanceSource instanceSource)
        {
            instanceSources.CreateOrUpdate(
                instanceSource.OfType,
                instanceSource,
                static (_, instanceSource) => InstanceSources.Create(instanceSource),
                static (_, instanceSource, existing) => existing.Add(instanceSource));
        }

        public static IEnumerable<TResult> SelectWhere<TSource, TResult>(this IEnumerable<TSource> source, Func<TSource, (bool, TResult)> whereSelector)
        {
            foreach (var item in source)
            {
                if (whereSelector(item) is (true, var result))
                {
                    yield return result;
                }
            }
        }
    }
}
