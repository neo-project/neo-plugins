using System;
using System.Collections.Generic;
using System.Threading;

namespace Neo.FileStorage.Cache
{
    public class LRUCache<K, V> where K : IEquatable<K>
    {
        private class Element
        {
            public K Key;
            public V Value;

            public Element(K k, V v)
            {
                Key = k;
                Value = v;
            }
        }

        private readonly ReaderWriterLockSlim cacheLock = new();
        private readonly Dictionary<K, Element> cache = new();
        private readonly LinkedList<Element> list = new();
        private readonly int capacity;
        private readonly Action<K, V> onEvict;
        public int Count => cache.Count;

        public LRUCache(int cap, Action<K, V> evict = null)
        {
            capacity = cap;
            onEvict = evict;
        }

        public bool TryGet(K key, out V value)
        {
            cacheLock.EnterUpgradeableReadLock();
            try
            {
                if (cache.TryGetValue(key, out Element el))
                {
                    cacheLock.EnterWriteLock();
                    list.Remove(el);
                    list.AddFirst(el);
                    cacheLock.ExitWriteLock();
                    value = el.Value;
                    return true;
                }
            }
            finally
            {
                cacheLock.ExitUpgradeableReadLock();
            }
            value = default;
            return false;
        }

        public bool TryPeek(K key, out V value)
        {
            cacheLock.EnterReadLock();
            try
            {
                if (cache.TryGetValue(key, out Element el))
                {
                    value = el.Value;
                    return true;
                }
            }
            finally
            {
                cacheLock.ExitReadLock();
            }
            value = default;
            return false;
        }

        public void Add(K key, V value)
        {
            Element evicted_el = null;
            cacheLock.EnterWriteLock();
            try
            {
                if (cache.TryGetValue(key, out Element el))
                {
                    list.Remove(el);
                    list.AddFirst(el);
                    return;
                }
                el = new(key, value);
                cache.Add(key, el);
                list.AddFirst(el);
                if (capacity < cache.Count)
                {
                    evicted_el = list.Last.Value;
                    cache.Remove(evicted_el.Key);
                    list.RemoveLast();
                }
            }
            finally
            {
                cacheLock.ExitWriteLock();
            }
            if (onEvict is not null && evicted_el is not null)
                onEvict(evicted_el.Key, evicted_el.Value);
        }

        public bool Remove(K key)
        {
            bool evicted;
            Element evicted_e = null;
            cacheLock.EnterWriteLock();
            try
            {
                evicted = cache.Remove(key, out evicted_e) && list.Remove(evicted_e);
            }
            finally
            {
                cacheLock.ExitWriteLock();
            }
            if (onEvict is not null && evicted)
                onEvict(evicted_e.Key, evicted_e.Value);
            return evicted;
        }

        public bool RemoveOldest(out (K, V) result)
        {
            cacheLock.EnterUpgradeableReadLock();
            Element to_rm;
            try
            {
                to_rm = list.Last?.Value;
                if (to_rm is null)
                {
                    result.Item1 = default;
                    result.Item2 = default;
                    return false;
                }
                cacheLock.EnterWriteLock();
                try
                {
                    list.RemoveLast();
                    cache.Remove(to_rm.Key);
                }
                finally
                {
                    cacheLock.ExitWriteLock();
                }
            }
            finally
            {
                cacheLock.ExitUpgradeableReadLock();
            }
            result.Item1 = to_rm.Key;
            result.Item2 = to_rm.Value;
            return true;
        }


        public bool Contains(K key)
        {
            cacheLock.EnterReadLock();
            try
            {
                return cache.ContainsKey(key);
            }
            finally
            {
                cacheLock.ExitReadLock();
            }
        }

        public IEnumerable<K> Keys()
        {
            cacheLock.EnterReadLock();
            try
            {
                foreach (var n in list)
                    yield return n.Key;
            }
            finally
            {
                cacheLock.ExitReadLock();
            }
        }

        public void Purge()
        {
            cacheLock.EnterWriteLock();
            try
            {

                foreach (var item in cache.Values)
                {
                    if (onEvict is not null)
                        onEvict(item.Key, item.Value);
                }
                cache.Clear();
                list.Clear();
            }
            finally
            {
                cacheLock.ExitWriteLock();
            }
        }
    }
}
