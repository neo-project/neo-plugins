using System;
using System.Runtime.CompilerServices;

namespace Neo.FileStorage.Cache
{
    public class NetworkCache<K, V> where K : IEquatable<K>
    {
        private readonly Func<K, V> fetcher;
        private readonly LRUCache<K, V> cache;

        public NetworkCache(int capactiy, Func<K, V> fetcher)
        {
            cache = new(capactiy, null);
            this.fetcher = fetcher;
        }

        [MethodImpl(MethodImplOptions.Synchronized)]
        public V Get(K key)
        {
            if (cache.TryPeek(key, out V value))
            {
                return value;
            }
            value = fetcher(key);
            cache.Add(key, value);
            return value;
        }
    }
}
