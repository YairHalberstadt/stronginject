using System;
using System.Collections.Concurrent;

namespace StrongInject.Samples.ConsoleApp
{
    public class Cache<TKey, TValue> where TKey : notnull
    {
        private readonly Func<TKey, TValue> _factory;

        public Cache(Func<TKey, TValue> factory)
        {
            _factory = factory;
        }

        private readonly ConcurrentDictionary<TKey, TValue> _cached = new();
        public TValue Get(TKey key) => _cached.GetOrAdd(key, _factory);
    }
}
