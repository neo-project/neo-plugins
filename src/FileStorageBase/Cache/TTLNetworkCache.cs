using System;
using System.Runtime.CompilerServices;

namespace Neo.FileStorage.Cache
{
    public class TTLNetworkCache<K, V> where K : IEquatable<K>
    {
        private class ValueWithTime
        {
            public DateTime Expiration;
            public V Value;
        }

        private readonly TimeSpan ttl;
        private readonly Func<K, V> fetcher;
        private readonly LRUCache<K, ValueWithTime> cache;

        public TTLNetworkCache(int capactiy, TimeSpan ttl, Func<K, V> fetcher)
        {
            cache = new(capactiy, null);
            this.ttl = ttl;
            this.fetcher = fetcher;
        }

        [MethodImpl(MethodImplOptions.Synchronized)]
        public V Get(K key)
        {
            if (cache.TryPeek(key, out ValueWithTime vt))
            {
                if (vt.Expiration < DateTime.UtcNow)
                {
                    return vt.Value;
                }
                cache.Remove(key);
            }
            var value = fetcher(key);
            cache.Add(key, new ValueWithTime { Expiration = DateTime.UtcNow + ttl, Value = value });
            return value;
        }
    }
}
