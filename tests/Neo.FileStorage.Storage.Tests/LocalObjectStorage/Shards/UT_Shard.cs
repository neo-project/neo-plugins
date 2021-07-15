using System;
using System.IO;
using System.Linq;
using Google.Protobuf;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Neo.FileStorage.API.Object;
using Neo.FileStorage.Storage.LocalObjectStorage;
using Neo.FileStorage.Storage.LocalObjectStorage.Shards;
using static Neo.FileStorage.Storage.Tests.Helper;

namespace Neo.FileStorage.Storage.Tests.LocalObjectStorage.Shards
{
    [TestClass]
    public class UT_Shard
    {
        private Shard NewShard(string testName, bool useCache)
        {
            ShardSettings settings = ShardSettings.Default;
            settings.UseWriteCache = useCache;
            settings.BlobStorageSettings.Path = testName + "/" + settings.BlobStorageSettings.Path;
            settings.BlobStorageSettings.BlobovniczasSettings.ShallowWidth = 4;
            settings.MetabaseSettings.Path = testName + "/" + settings.MetabaseSettings.Path;
            if (useCache)
            {
                settings.WriteCacheSettings = WriteCacheSettings.Default;
                settings.WriteCacheSettings.Path = testName + "/" + settings.WriteCacheSettings.Path;
                settings.WriteCacheSettings.MaxMemorySize = 0;
            }
            Shard shard = new(settings, null, null);
            shard.Open();
            return shard;
        }

        private void TestShardGet(Shard shard)
        {
            //small object
            var obj = RandomObject(1 << 5);
            obj.Header.Attributes.Add(new Header.Types.Attribute { Key = "foo", Value = "bar" });
            shard.Put(obj);
            var o = shard.Get(obj.Address);
            Assert.IsNotNull(o);
            Assert.AreEqual(obj.Address, o.Address);
            //big object
            obj = RandomObject(1 << 20);
            obj.Header.Attributes.Add(new Header.Types.Attribute { Key = "foo", Value = "bar" });
            shard.Put(obj);
            o = shard.Get(obj.Address);
            Assert.IsNotNull(o);
            Assert.AreEqual(obj.Address, o.Address);
            //parent object
            var cid = RandomContainerID();
            obj = RandomObject(cid);
            obj.Header.Attributes.Add(new Header.Types.Attribute { Key = "foo", Value = "bar" });
            var sid = new SplitID();
            var parent = RandomObject(cid);
            parent.Header.Attributes.Add(new Header.Types.Attribute { Key = "foo", Value = "bar" });
            var child = RandomObject(cid);
            child.Parent = parent;
            child.SplitId = sid;
            shard.Put(child);
            o = shard.Get(child.Address);
            Assert.IsNotNull(o);
            Assert.IsTrue(child.ToByteArray().SequenceEqual(o.ToByteArray()));
            Assert.ThrowsException<Storage.LocalObjectStorage.SplitInfoException>(() => shard.Get(parent.Address));
            try
            {
                shard.Get(parent.Address);
            }
            catch (Storage.LocalObjectStorage.SplitInfoException sie)
            {
                Assert.IsNull(sie.SplitInfo.Link);
                Assert.AreEqual(child.ObjectId, sie.SplitInfo.LastPart);
                Assert.IsTrue(sid.ToByteArray().SequenceEqual(sie.SplitInfo.SplitId.ToByteArray()));
            }
        }

        [TestMethod]
        public void TestShardGetWithoutCache()
        {
            try
            {
                using var shard = NewShard(nameof(TestShardGetWithoutCache), false);
                TestShardGet(shard);
            }
            finally
            {
                Directory.Delete(nameof(TestShardGetWithoutCache), true);
            }
        }

        [TestMethod]
        public void TestShardGetWithCache()
        {
            try
            {
                using var shard = NewShard(nameof(TestShardGetWithCache), true);
                TestShardGet(shard);
            }
            finally
            {
                Directory.Delete(nameof(TestShardGetWithCache), true);
            }
        }

        private void TestShardHead(Shard shard)
        {
            //regular object
            var obj = RandomObject(1 << 5);
            obj.Header.Attributes.Add(new Header.Types.Attribute { Key = "foo", Value = "bar" });
            shard.Put(obj);
            var o = shard.Head(obj.Address, false);
            Assert.IsNotNull(o);
            Assert.AreEqual(obj.Address, o.Address);
            Assert.IsTrue(obj.CutPayload().Equals(o));
            //virtual object
            var cid = RandomContainerID();
            var sid = new SplitID();
            var parent = RandomObject(cid);
            obj.Header.Attributes.Add(new Header.Types.Attribute { Key = "foo", Value = "bar" });
            var child = RandomObject(cid);
            child.Parent = parent;
            child.SplitId = sid;
            shard.Put(child);
            o = shard.Head(parent.Address, false);
            Assert.IsNotNull(o);
            Assert.AreEqual(parent.Address, o.Address);
            Assert.IsTrue(parent.CutPayload().ToByteArray().SequenceEqual(o.ToByteArray()));
        }

        [TestMethod]
        public void TestShardHeadWithoutCache()
        {
            try
            {
                using var shard = NewShard(nameof(TestShardHeadWithoutCache), false);
                TestShardHead(shard);
            }
            finally
            {
                Directory.Delete(nameof(TestShardHeadWithoutCache), true);
            }
        }

        [TestMethod]
        public void TestShardHeadWithCache()
        {
            try
            {
                using var shard = NewShard(nameof(TestShardHeadWithCache), true);
                TestShardHead(shard);
            }
            finally
            {
                Directory.Delete(nameof(TestShardHeadWithCache), true);
            }
        }

        private void TestShardInhume(Shard shard)
        {
            var cid = RandomContainerID();
            var obj = RandomObject(cid);
            obj.Header.Attributes.Add(new Header.Types.Attribute { Key = "foo", Value = "bar" });
            var ts = RandomObject(cid);
            shard.Put(obj);
            var o = shard.Get(obj.Address);
            Assert.IsNotNull(o);
            Assert.AreEqual(o.Address, obj.Address);
            Assert.IsTrue(obj.ToByteArray().SequenceEqual(o.ToByteArray()));
            shard.Inhume(ts.Address, obj.Address);
            Assert.ThrowsException<ObjectAlreadyRemovedException>(() => shard.Get(obj.Address));
        }

        [TestMethod]
        public void TestShardInhumeWithoutCache()
        {
            try
            {
                using var shard = NewShard(nameof(TestShardInhumeWithoutCache), false);
                TestShardInhume(shard);
            }
            finally
            {
                Directory.Delete(nameof(TestShardInhumeWithoutCache), true);
            }
        }

        [TestMethod]
        public void TestShardInhumeWithCache()
        {
            try
            {
                using var shard = NewShard(nameof(TestShardInhumeWithCache), true);
                TestShardInhume(shard);
            }
            finally
            {
                Directory.Delete(nameof(TestShardInhumeWithCache), true);
            }
        }
    }
}
