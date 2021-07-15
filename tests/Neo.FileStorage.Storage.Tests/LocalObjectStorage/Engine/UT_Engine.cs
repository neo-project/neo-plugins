using System;
using System.IO;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Neo.FileStorage.API.Object;
using Neo.FileStorage.API.Refs;
using Neo.FileStorage.Storage.LocalObjectStorage.Engine;
using Neo.FileStorage.Storage.LocalObjectStorage.Shards;
using static Neo.FileStorage.Storage.Tests.Helper;
using FSObject = Neo.FileStorage.API.Object.Object;

namespace Neo.FileStorage.Storage.Tests.LocalObjectStorage.Engine
{
    [TestClass]
    public class UT_Engine
    {
        private Shard NewShard(string root, int num)
        {
            ShardSettings settings = ShardSettings.Default;
            settings.UseWriteCache = false;
            settings.BlobStorageSettings.Path = root + $"/Data_BlobStorage_{num}";
            settings.BlobStorageSettings.ShallowDepth = 2;
            settings.BlobStorageSettings.BlobovniczasSettings.ShallowDepth = 2;
            settings.BlobStorageSettings.BlobovniczasSettings.ShallowWidth = 2;
            settings.MetabaseSettings.Path = root + $"/Data_Metabase_{num}";
            return new(settings, null, null);
        }

        [TestMethod]
        public void TestHead()
        {
            ContainerID cid = RandomContainerID();
            SplitID sid = new();
            FSObject parent = RandomObject(cid);
            parent.Header.Attributes.Add(new Header.Types.Attribute { Key = "foo", Value = "bar" });
            FSObject child = RandomObject(cid);
            child.Parent = parent;
            child.SplitId = sid;
            FSObject link = RandomObject(cid);
            link.Parent = parent;
            link.Children = new ObjectID[] { child.ObjectId };
            link.SplitId = sid;
            string path = $"./engine_{nameof(TestHead)}";
            try
            {
                using StorageEngine engine = new();
                var s1 = NewShard(path, 1);
                var s2 = NewShard(path, 2);
                engine.AddShard(s1);
                engine.AddShard(s2);
                engine.Open();
                s1.Put(child);
                s2.Put(link);
                var o = engine.Head(parent.Address, false);
                Assert.AreEqual(parent.ObjectId, o.ObjectId);
                Assert.ThrowsException<FileStorage.Storage.LocalObjectStorage.SplitInfoException>(() => engine.Head(parent.Address, true));
                try
                {
                    engine.Head(parent.Address, true);
                }
                catch (FileStorage.Storage.LocalObjectStorage.SplitInfoException sie)
                {
                    Assert.IsTrue(sid.ToByteArray().SequenceEqual(sie.SplitInfo.SplitId.ToByteArray()));
                    Assert.AreEqual(child.ObjectId, sie.SplitInfo.LastPart);
                    Assert.AreEqual(link.ObjectId, sie.SplitInfo.Link);
                }
            }
            finally
            {
                Directory.Delete(path, true);
            }
        }

        [TestMethod]
        public void TestInhumeSmall()
        {
            ContainerID cid = RandomContainerID();
            SplitID sid = new();
            SearchFilters filters = new();
            filters.AddRootFilter();
            var ts = RandomObject(cid);
            FSObject parent = RandomObject(cid);
            FSObject child = RandomObject(cid);
            child.Parent = parent;
            child.SplitId = sid;
            FSObject link = RandomObject(cid);
            link.Parent = parent;
            link.Children = new ObjectID[] { child.ObjectId };
            link.SplitId = sid;
            string path = $"./engine_{nameof(TestInhumeSmall)}";
            try
            {
                //delete small object
                using StorageEngine engine = new();
                var s1 = NewShard(path, 1);
                engine.AddShard(s1);
                engine.Open();
                engine.Put(parent);
                engine.Inhume(ts.Address, parent.Address);
                var addrs = engine.Select(cid, filters);
                Assert.IsFalse(addrs.Any());
            }
            finally
            {
                Directory.Delete(path, true);
            }
        }

        [TestMethod]
        public void TestInhumeBig()
        {
            ContainerID cid = RandomContainerID();
            SplitID sid = new();
            SearchFilters filters = new();
            filters.AddRootFilter();
            var ts = RandomObject(cid);
            FSObject parent = RandomObject(cid);
            FSObject child = RandomObject(cid);
            child.Parent = parent;
            child.SplitId = sid;
            FSObject link = RandomObject(cid);
            link.Parent = parent;
            link.Children = new ObjectID[] { child.ObjectId };
            link.SplitId = sid;
            string path = $"./engine_{nameof(TestInhumeBig)}";
            try
            {
                //delete big object
                using StorageEngine engine = new();
                var s1 = NewShard(path, 1);
                var s2 = NewShard(path, 2);
                engine.AddShard(s1);
                engine.AddShard(s2);
                engine.Open();
                s1.Put(child);
                s2.Put(link);
                engine.Inhume(ts.Address, parent.Address);
                var addrs = engine.Select(cid, filters);
                Assert.IsFalse(addrs.Any());
            }
            finally
            {
                Directory.Delete(path, true);
            }
        }
    }
}
