using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Neo.FileStorage.Cache;

namespace Neo.FileStorage.Tests.Cache
{
    [TestClass]
    public class UT_LRUCache
    {
        [TestMethod]
        public void TestLRU()
        {
            int evicted_counter = 0;
            LRUCache<int, int> lru = new(128, (int k, int v) => evicted_counter++);
            for (int i = 0; i < 256; i++)
                lru.Add(i, i);
            Assert.AreEqual(128, lru.Count);
            Assert.AreEqual(128, evicted_counter);
            var keys = lru.Keys().ToArray();
            for (int i = 0; i < keys.Length; i++)
            {
                Assert.IsTrue(lru.TryGet(keys[i], out int value));
                Assert.IsTrue(value == keys[i]);
                Assert.IsTrue(value == 255 - i);
            }
            for (int i = 0; i < 128; i++)
                Assert.IsFalse(lru.TryGet(i, out _));
            for (int i = 128; i < 256; i++)
                Assert.IsTrue(lru.TryGet(i, out _));
            for (int i = 128; i < 192; i++)
            {
                Assert.IsTrue(lru.Remove(i));
                Assert.IsFalse(lru.Remove(i));
                Assert.IsFalse(lru.TryGet(i, out _));
            }
            lru.TryGet(192, out _);
            keys = lru.Keys().ToArray();
            for (int i = 0; i < keys.Length; i++)
            {
                Assert.IsTrue(!(0 < i && keys[i] != 256 - i) || (i == 0 && keys[i] != 192));
            }
            lru.Purge();
            Assert.AreEqual(0, lru.Count);
        }

        [TestMethod]
        public void TestAddGet()
        {
            LRUCache<int, string> lru = new(3);
            lru.Add(0, "hello");
            Assert.AreEqual(1, lru.Count);
            Assert.IsTrue(lru.TryGet(0, out string value));
            Assert.AreEqual("hello", value);
        }

        [TestMethod]
        public void TestOrderAdd()
        {
            LRUCache<int, string> lru = new(3);
            lru.Add(0, "hello");
            lru.Add(1, "neo");
            lru.Add(2, "world");
            lru.Add(3, "neofs");
            Assert.IsFalse(lru.TryGet(0, out _));
            Assert.IsTrue(lru.TryGet(1, out _));
            Assert.IsTrue(lru.TryGet(2, out _));
            Assert.IsTrue(lru.TryGet(3, out _));
        }

        [TestMethod]
        public void TestOrderGet()
        {
            LRUCache<int, string> lru = new(3);
            lru.Add(0, "hello");
            lru.Add(1, "neo");
            lru.Add(2, "world");
            lru.TryGet(0, out _);
            lru.Add(3, "neofs");
            Assert.IsFalse(lru.TryGet(1, out _));
            Assert.IsTrue(lru.TryGet(0, out _));
            Assert.IsTrue(lru.TryGet(2, out _));
            Assert.IsTrue(lru.TryGet(3, out _));
        }
    }
}
