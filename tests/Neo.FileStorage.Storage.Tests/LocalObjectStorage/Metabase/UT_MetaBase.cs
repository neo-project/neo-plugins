using System;
using System.Collections.Generic;
using System.IO;
using Google.Protobuf;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Neo.FileStorage.API.Object;
using Neo.FileStorage.API.Refs;
using Neo.FileStorage.Storage.LocalObjectStorage;
using Neo.FileStorage.Storage.LocalObjectStorage.Blob;
using Neo.FileStorage.Storage.LocalObjectStorage.Metabase;
using static Neo.FileStorage.Storage.Tests.Helper;
using FSObject = Neo.FileStorage.API.Object.Object;

namespace Neo.FileStorage.Storage.Tests.LocalObjectStorage.Metabase
{
    [TestClass]
    public class UT_MetaBase
    {
        [TestMethod]
        public void TestContainers()
        {
            string path = "./test_mb_TestContainers";
            const int N = 10;
            try
            {
                Dictionary<ContainerID, int> cids = new();
                using MB mb = new(path);
                mb.Open();
                for (int i = 0; i < N; i++)
                {
                    var obj = RandomObject(1 << 10);
                    cids.Add(obj.ContainerId, 0);
                    mb.Put(obj);
                }
                var list = mb.Containers();
                Assert.AreEqual(cids.Count, list.Count);
                foreach (var cid in list)
                {
                    Assert.IsTrue(cids.TryGetValue(cid, out _));
                }
                //Inhume
                var o = RandomObject(1 << 10);
                mb.Put(o);
                list = mb.Containers();
                Assert.IsTrue(list.Contains(o.ContainerId));
                mb.Inhume(RandomAddress(), o.Address);
                list = mb.Containers();
                Assert.IsTrue(list.Contains(o.ContainerId));
                //ToMoveIt
                o = RandomObject(1 << 10);
                mb.Put(o);
                list = mb.Containers();
                Assert.IsTrue(list.Contains(o.ContainerId));
                mb.MoveIt(o.Address);
                list = mb.Containers();
                Assert.IsTrue(list.Contains(o.ContainerId));
            }
            finally
            {
                Directory.Delete(path, true);
            }
        }

        [TestMethod]
        public void TestContainerSize()
        {
            string path = "./test_mb_TestContainerSize";
            const int C = 3, N = 5;
            try
            {
                using MB mb = new(path);
                mb.Open();
                Dictionary<ContainerID, int> cids = new();
                Dictionary<ContainerID, List<FSObject>> objs = new();
                for (int i = 0; i < C; i++)
                {
                    var cid = RandomContainerID();
                    cids[cid] = 0;
                    var random = new Random();
                    for (int j = 0; j < N; j++)
                    {
                        int size = random.Next(1, 1024);
                        var parent = RandomObject(cid, size / 2);
                        var obj = RandomObject(cid, size);
                        obj.Parent = parent;
                        obj.SplitId = new SplitID();
                        cids[cid] += size;
                        if (!objs.TryGetValue(cid, out List<FSObject> value))
                        {
                            value = new();
                            objs[cid] = value;
                        }
                        objs[cid].Add(obj);
                        mb.Put(obj);
                    }
                }
                foreach (var (cid, size) in cids)
                {
                    Assert.AreEqual((ulong)size, mb.ContainerSize(cid));
                }
                foreach (var (cid, list) in objs)
                {
                    var val = (ulong)cids[cid];
                    foreach (var obj in list)
                    {
                        mb.Inhume(RandomAddress(), obj.Address);
                        val -= obj.PayloadSize;
                        var n = mb.ContainerSize(cid);
                        Assert.AreEqual(val, n);
                    }
                }
            }
            finally
            {
                Directory.Delete(path, true);
            }
        }

        [TestMethod]
        public void TestDelete()
        {
            string path = "./test_mb_TestDelete";
            try
            {
                using MB mb = new(path);
                mb.Open();
                var cid = RandomContainerID();
                var parent = RandomObject(cid);
                parent.Header.Attributes.Add(new Header.Types.Attribute()
                {
                    Key = "foo",
                    Value = "bar"
                });
                var child = RandomObject(cid);
                child.Parent = parent;
                child.SplitId = new SplitID();
                mb.Put(child);
                mb.MoveIt(child.Address);
                var list = mb.Moveable();
                Assert.AreEqual(1, list.Count);
                Assert.ThrowsException<Storage.LocalObjectStorage.SplitInfoException>(() => mb.Delete(parent.Address));
                var ts = RandomObject(cid);
                mb.Inhume(ts.Address, child.Address);
                mb.Inhume(ts.Address, child.Address);
                mb.Delete(child.Address);
                list = mb.Moveable();
                Assert.AreEqual(0, list.Count);
                Assert.IsFalse(mb.Exists(child.Address));
                Assert.IsFalse(mb.Exists(parent.Address));
            }
            finally
            {
                Directory.Delete(path, true);
            }
        }

        [TestMethod]
        public void TestDeleteAllChildren()
        {
            string path = "./test_mb_TestDeleteAllChildren";
            try
            {
                using MB mb = new(path);
                mb.Open();
                var cid = RandomContainerID();
                var parent = RandomObject(cid);
                var child1 = RandomObject(cid);
                var spi = new SplitID();
                child1.Parent = parent;
                child1.SplitId = spi;
                var child2 = RandomObject(cid);
                child2.Parent = parent;
                child2.SplitId = spi;
                mb.Put(child1);
                mb.Put(child2);
                Assert.ThrowsException<Storage.LocalObjectStorage.SplitInfoException>(() => mb.Exists(parent.Address));
                mb.Delete(child1.Address, child2.Address);
                Assert.IsFalse(mb.Exists(parent.Address));
            }
            finally
            {
                Directory.Delete(path, true);
            }
        }

        [TestMethod]
        public void TestGraveOnlyDelete()
        {
            string path = "./test_mb_TestGraveOnlyDelete";
            try
            {
                using MB mb = new(path);
                mb.Open();
                var address = RandomAddress();
                mb.Inhume(address);
                mb.Delete(address);
            }
            finally
            {
                Directory.Delete(path, true);
            }
        }

        [TestMethod]
        public void TestExists()
        {
            string path = "./test_mb_TestExists";
            try
            {
                using MB mb = new(path);
                mb.Open();
                var obj = RandomObject();

                //no object
                Assert.IsFalse(mb.Exists(obj.Address));

                //regular object
                mb.Put(obj);
                Assert.IsTrue(mb.Exists(obj.Address));

                //tombstone object
                var ts = RandomObject();
                ts.Header.ObjectType = ObjectType.Tombstone;
                mb.Put(ts);
                Assert.IsTrue(mb.Exists(ts.Address));

                //storage group object
                var sg = RandomObject();
                sg.Header.ObjectType = ObjectType.StorageGroup;
                mb.Put(sg);
                Assert.IsTrue(mb.Exists(sg.Address));

                //virtual object
                var cid = RandomContainerID();
                var parent = RandomObject(cid);
                var child = RandomObject(cid);
                child.Parent = parent;
                child.SplitId = new SplitID();
                mb.Put(child);
                Assert.ThrowsException<Storage.LocalObjectStorage.SplitInfoException>(() => mb.Exists(parent.Address));

                #region merge split info
                {
                    //direct order
                    cid = RandomContainerID();
                    var spi1 = new SplitID();
                    parent = RandomObject(cid);
                    parent.Header.Attributes.Add(new Header.Types.Attribute()
                    {
                        Key = "foo",
                        Value = "bar"
                    });
                    child = RandomObject(cid);
                    child.Parent = parent;
                    child.SplitId = spi1;
                    var link = RandomObject(cid);
                    link.Parent = parent;
                    link.Children = new ObjectID[] { child.ObjectId };
                    link.SplitId = spi1;

                    mb.Put(child);
                    mb.Put(link);
                    try
                    {
                        mb.Exists(parent.Address);
                    }
                    catch (Exception e)
                    {
                        Assert.IsTrue(e is FileStorage.Storage.LocalObjectStorage.SplitInfoException);
                        var spie = e as FileStorage.Storage.LocalObjectStorage.SplitInfoException;
                        Assert.AreEqual(spi1.ToByteArray().ToHexString(), spie.SplitInfo.SplitId.ToByteArray().ToHexString());
                        Assert.AreEqual(child.ObjectId, spie.SplitInfo.LastPart);
                        Assert.AreEqual(link.ObjectId, spie.SplitInfo.Link);
                    }

                    //reverse order
                    var spi2 = new SplitID();
                    parent = RandomObject(cid);
                    parent.Header.Attributes.Add(new Header.Types.Attribute()
                    {
                        Key = "foo",
                        Value = "bar"
                    });
                    child = RandomObject(cid);
                    child.Parent = parent;
                    child.SplitId = spi2;
                    link = RandomObject(cid);
                    link.Parent = parent;
                    link.Children = new ObjectID[] { child.ObjectId };
                    link.SplitId = spi2;
                    mb.Put(link);
                    mb.Put(child);
                    try
                    {
                        mb.Exists(parent.Address);
                    }
                    catch (Exception e)
                    {
                        Assert.IsTrue(e is Storage.LocalObjectStorage.SplitInfoException);
                        var spie = e as Storage.LocalObjectStorage.SplitInfoException;
                        Assert.AreEqual(spi2.ToByteArray().ToHexString(), spie.SplitInfo.SplitId.ToByteArray().ToHexString());
                        Assert.AreEqual(child.ObjectId, spie.SplitInfo.LastPart);
                        Assert.AreEqual(link.ObjectId, spie.SplitInfo.Link);
                    }
                }
                #endregion
            }
            finally
            {
                Directory.Delete(path, true);
            }
        }

        [TestMethod]
        public void TestGet()
        {
            string path = "./test_mb_TestGet";
            try
            {
                using MB mb = new(path);
                mb.Open();
                var obj = RandomObject();

                //object not found
                Assert.ThrowsException<ObjectNotFoundException>(() => mb.Get(obj.Address));

                //regular object
                mb.Put(obj);
                Assert.AreEqual(obj.ObjectId, mb.Get(obj.Address).ObjectId);
                Assert.AreEqual(obj.CutPayload().ToByteArray().ToHexString(), mb.Get(obj.Address).ToByteArray().ToHexString());

                //tombstone object
                obj.Header.ObjectType = ObjectType.Tombstone;
                obj.ObjectId = RandomObjectID();
                mb.Put(obj);
                Assert.AreEqual(obj.CutPayload().ToByteArray().ToHexString(), mb.Get(obj.Address).ToByteArray().ToHexString());

                //storagegroup object
                obj.Header.ObjectType = ObjectType.StorageGroup;
                obj.ObjectId = RandomObjectID();
                mb.Put(obj);
                Assert.AreEqual(obj.CutPayload().ToByteArray().ToHexString(), mb.Get(obj.Address).ToByteArray().ToHexString());

                //virtual object
                var cid = RandomContainerID();
                var sid = new SplitID();
                var parent = RandomObject(cid);
                parent.Header.Attributes.Add(new Header.Types.Attribute()
                {
                    Key = "foo",
                    Value = "bar"
                });
                var child = RandomObject(cid);
                child.Parent = parent;
                child.SplitId = sid;
                mb.Put(child);
                try
                {
                    mb.Get(parent.Address, true);
                }
                catch (Exception e)
                {
                    Assert.IsTrue(e is Storage.LocalObjectStorage.SplitInfoException);
                    var sie = e as Storage.LocalObjectStorage.SplitInfoException;
                    Assert.AreEqual(sid.ToByteArray().ToHexString(), sie.SplitInfo.SplitId.ToByteArray().ToHexString());
                    Assert.AreEqual(child.ObjectId, sie.SplitInfo.LastPart);
                }
                var new_parent = mb.Get(parent.Address, false);
                Assert.AreEqual(parent.CutPayload().ToString(), new_parent.ToString());
                Assert.AreEqual(parent.CutPayload().ToByteArray().ToHexString(), new_parent.ToByteArray().ToHexString());
                var new_child = mb.Get(child.Address);
                Assert.AreEqual(child.CutPayload().ToByteArray().ToHexString(), new_child.ToByteArray().ToHexString());
            }
            finally
            {
                Directory.Delete(path, true);
            }
        }

        [TestMethod]
        public void TestIterateGraveYard()
        {
            string path = "./test_mb_TestIterateGraveYard";
            try
            {
                using MB mb = new(path);
                mb.Open();
                var obj1 = RandomObject();
                var obj2 = RandomObject();
                mb.Put(obj1);
                mb.Put(obj2);
                var tombstone = RandomAddress();
                mb.Inhume(tombstone, obj1.Address);
                mb.Inhume(null, obj2.Address);
                int counter = 0;
                List<Address> buriedTS = new(), buriedGC = new();
                mb.IterateGraveYard(g =>
                {
                    if (g.GCMark)
                        buriedGC.Add(g.Address);
                    else
                        buriedTS.Add(g.Address);
                    counter++;
                    return false;
                });
                Assert.AreEqual(2, counter);
                Assert.AreEqual(obj1.Address, buriedTS[0]);
                Assert.AreEqual(obj2.Address, buriedGC[0]);
            }
            finally
            {
                Directory.Delete(path, true);
            }
        }

        [TestMethod]
        public void TestInhume()
        {
            string path = "./test_mb_TestInhume";
            try
            {
                using MB mb = new(path);
                mb.Open();
                var obj = RandomObject();
                obj.Header.Attributes.Add(new Header.Types.Attribute()
                {
                    Key = "foo",
                    Value = "bar"
                });
                var tombstone = RandomAddress();
                mb.Put(obj);
                mb.Inhume(tombstone, obj.Address);
                Assert.ThrowsException<ObjectAlreadyRemovedException>(() => mb.Exists(obj.Address));
                Assert.ThrowsException<ObjectAlreadyRemovedException>(() => mb.Get(obj.Address));
            }
            finally
            {
                Directory.Delete(path, true);
            }
        }

        [TestMethod]
        public void TestInhumeTombOnTomb()
        {
            string path = "./test_mb_TestInhumeTombOnTomb";
            try
            {
                using MB mb = new(path);
                mb.Open();
                var address1 = RandomAddress();
                var address2 = RandomAddress();
                var address3 = RandomAddress();
                mb.Inhume(address2, address1);
                Assert.ThrowsException<ObjectAlreadyRemovedException>(() => mb.Exists(address1));
                mb.Inhume(address1, address3);
                Assert.IsFalse(mb.Exists(address1));
                Assert.ThrowsException<ObjectAlreadyRemovedException>(() => mb.Exists(address3));
                mb.Inhume(RandomAddress(), address1);
                Assert.IsFalse(mb.Exists(address1));
            }
            finally
            {
                Directory.Delete(path, true);
            }
        }

        private Address PutWithExpiration(MB mb, ObjectType type, ulong expire)
        {
            var obj = RandomObject();
            obj.Header.ObjectType = type;
            obj.Header.Attributes.Add(new Header.Types.Attribute()
            {
                Key = Header.Types.Attribute.SysAttributeExpEpoch,
                Value = expire.ToString(),
            });
            mb.Put(obj);
            return obj.Address;
        }

        [TestMethod]
        public void TestIterateExpired()
        {
            string path = "./test_mb_TestIterateExpired";
            try
            {
                using MB mb = new(path);
                mb.Open();
                ulong epoch = 13;
                Dictionary<ObjectType, Address> mAlive = new(), mExpired = new();
                foreach (var t in new ObjectType[] { ObjectType.Regular, ObjectType.Tombstone, ObjectType.StorageGroup })
                {
                    mAlive[t] = PutWithExpiration(mb, t, epoch);
                    mExpired[t] = PutWithExpiration(mb, t, epoch - 1);
                }
                mb.IterateExpired(epoch, (type, address) =>
                {
                    Assert.AreNotEqual(mAlive[type], address);
                    Assert.AreEqual(mExpired[type], address);
                    mExpired.Remove(type);
                });
            }
            finally
            {
                Directory.Delete(path, true);
            }
        }

        [TestMethod]
        public void TestIterateCoveredByTombstones()
        {
            string path = "./test_mb_TestIterateCoveredByTombstones";
            try
            {
                using MB mb = new(path);
                mb.Open();
                var ts = RandomAddress();
                var protected1 = RandomAddress();
                var protected2 = RandomAddress();
                var garbage = RandomAddress();
                mb.Inhume(ts, protected1, protected2);
                mb.Inhume(null, garbage);
                List<Address> handled = new();
                HashSet<Address> tss = new() { ts };
                mb.IterateCoveredByTombstones(tss, address =>
                {
                    handled.Add(address);
                });
                Assert.AreEqual(2, handled.Count);
                Assert.IsTrue(handled.Contains(protected1));
                Assert.IsTrue(handled.Contains(protected2));
            }
            finally
            {
                Directory.Delete(path, true);
            }
        }

        [TestMethod]
        public void TestMovable()
        {
            string path = "./test_mb_TestMovable";
            try
            {
                using MB mb = new(path);
                mb.Open();
                var raw1 = RandomObject();
                var raw2 = RandomObject();
                mb.Put(raw1);
                mb.Put(raw2);
                var list = mb.Moveable();
                Assert.AreEqual(0, list.Count);
                mb.MoveIt(raw2.Address);
                list = mb.Moveable();
                Assert.AreEqual(1, list.Count);
                Assert.IsTrue(list.Contains(raw2.Address));
                mb.DoNotMove(raw2.Address);
                list = mb.Moveable();
                Assert.AreEqual(0, list.Count);
            }
            finally
            {
                Directory.Delete(path, true);
            }
        }

        [TestMethod]
        public void TestPutBlobovnicaUpdate()
        {
            string path = "./test_mb_TestPutBlobovnicaUpdate";
            try
            {
                using MB mb = new(path);
                mb.Open();
                var obj1 = RandomObject();
                BlobovniczaID bid = new byte[] { 1, 2, 3, 4 };
                mb.Put(obj1, bid);
                var fbid = mb.IsSmall(obj1.Address);
                Assert.AreEqual(((byte[])bid).ToHexString(), ((byte[])fbid).ToHexString());
                var obj2 = RandomObject();
                mb.Put(obj2);
                fbid = mb.IsSmall(obj2.Address);
                Assert.IsNull(fbid);
            }
            finally
            {
                Directory.Delete(path, true);
            }
        }

        [TestMethod]
        public void TestSelectUserAttribute()
        {
            string path = "./test_mb_TestSelectUserAttribute";
            try
            {
                using MB mb = new(path);
                mb.Open();
                var cid = RandomContainerID();
                var obj1 = RandomObject(cid);
                obj1.Header.Attributes.Add(new Header.Types.Attribute
                {
                    Key = "foo",
                    Value = "bar"
                });
                obj1.Header.Attributes.Add(new Header.Types.Attribute
                {
                    Key = "x",
                    Value = "y"
                });
                mb.Put(obj1);
                var obj2 = RandomObject(cid);
                obj2.Header.Attributes.Add(new Header.Types.Attribute
                {
                    Key = "foo",
                    Value = "bar"
                });
                obj2.Header.Attributes.Add(new Header.Types.Attribute
                {
                    Key = "x",
                    Value = "z"
                });
                mb.Put(obj2);
                var obj3 = RandomObject(cid);
                obj3.Header.Attributes.Add(new Header.Types.Attribute
                {
                    Key = "a",
                    Value = "b"
                });
                mb.Put(obj3);

                var fs = new SearchFilters();
                fs.AddFilter("foo", "bar", API.Object.MatchType.StringEqual);
                var list = mb.Select(cid, fs);
                Assert.IsTrue(list.Contains(obj1.Address));
                Assert.IsTrue(list.Contains(obj2.Address));
                Assert.IsFalse(list.Contains(obj3.Address));

                fs = new();
                fs.AddFilter("x", "y", API.Object.MatchType.StringEqual);
                list = mb.Select(cid, fs);
                Assert.IsTrue(list.Contains(obj1.Address));
                Assert.IsFalse(list.Contains(obj2.Address));
                Assert.IsFalse(list.Contains(obj3.Address));

                fs = new();
                fs.AddFilter("x", "y", API.Object.MatchType.StringNotEqual);
                list = mb.Select(cid, fs);
                Assert.IsFalse(list.Contains(obj1.Address));
                Assert.IsTrue(list.Contains(obj2.Address));
                Assert.IsFalse(list.Contains(obj3.Address));

                fs = new();
                fs.AddFilter("a", "b", API.Object.MatchType.StringEqual);
                list = mb.Select(cid, fs);
                Assert.IsFalse(list.Contains(obj1.Address));
                Assert.IsFalse(list.Contains(obj2.Address));
                Assert.IsTrue(list.Contains(obj3.Address));

                fs = new();
                fs.AddFilter("c", "d", API.Object.MatchType.StringEqual);
                list = mb.Select(cid, fs);
                Assert.IsFalse(list.Contains(obj1.Address));
                Assert.IsFalse(list.Contains(obj2.Address));
                Assert.IsFalse(list.Contains(obj3.Address));

                fs = new();
                fs.AddFilter("foo", "", API.Object.MatchType.NotPresent);
                list = mb.Select(cid, fs);
                Assert.IsFalse(list.Contains(obj1.Address));
                Assert.IsFalse(list.Contains(obj2.Address));
                Assert.IsTrue(list.Contains(obj3.Address));

                fs = new();
                fs.AddFilter("a", "", API.Object.MatchType.NotPresent);
                list = mb.Select(cid, fs);
                Assert.IsTrue(list.Contains(obj1.Address));
                Assert.IsTrue(list.Contains(obj2.Address));
                Assert.IsFalse(list.Contains(obj3.Address));

                fs = new();
                list = mb.Select(cid, fs);
                Assert.IsTrue(list.Contains(obj1.Address));
                Assert.IsTrue(list.Contains(obj2.Address));
                Assert.IsTrue(list.Contains(obj3.Address));
            }
            finally
            {
                Directory.Delete(path, true);
            }
        }

        [TestMethod]
        public void TestSelectRootPhyParent()
        {
            string path = "./test_mb_TestSelectRootPhyParent";
            try
            {
                using MB mb = new(path);
                mb.Open();
                var cid = RandomContainerID();
                var sid = new SplitID();
                var small = RandomObject(cid);
                mb.Put(small);
                var ts = RandomObject(cid);
                ts.Header.ObjectType = ObjectType.Tombstone;
                mb.Put(ts);
                var sg = RandomObject(cid);
                sg.Header.ObjectType = ObjectType.StorageGroup;
                mb.Put(sg);
                var left = RandomObject(cid);
                left.Header.Split = new()
                {
                    SplitId = sid
                };
                mb.Put(left);
                var parent = RandomObject(cid);
                var right = RandomObject(cid);
                right.Parent = parent;
                right.SplitId = sid;
                mb.Put(right);
                var link = RandomObject(cid);
                link.Parent = parent;
                link.Header.Split.Children.Add(left.ObjectId);
                link.Header.Split.Children.Add(right.ObjectId);
                link.SplitId = sid;
                mb.Put(link);
                //root objects
                var fs = new SearchFilters();
                fs.AddRootFilter();
                var list = mb.Select(cid, fs);
                Assert.IsTrue(list.Contains(small.Address));
                Assert.IsTrue(list.Contains(parent.Address));
                fs = new();
                fs.AddFilter(SearchRequest.Types.Body.Types.Filter.FilterPropertyRoot, "", API.Object.MatchType.NotPresent);
                Assert.AreEqual(0, mb.Select(cid, fs).Count);
                //phy objects
                fs = new();
                fs.AddPhyFilter();
                list = mb.Select(cid, fs);
                Assert.IsTrue(list.Contains(small.Address));
                Assert.IsTrue(list.Contains(ts.Address));
                Assert.IsTrue(list.Contains(sg.Address));
                Assert.IsTrue(list.Contains(left.Address));
                Assert.IsTrue(list.Contains(right.Address));
                Assert.IsTrue(list.Contains(link.Address));
                fs = new();
                fs.AddFilter(SearchRequest.Types.Body.Types.Filter.FilterPropertyPhy, "", API.Object.MatchType.NotPresent);
                Assert.AreEqual(0, mb.Select(cid, fs).Count);
                //regular objects
                fs = new();
                fs.AddFilter(SearchRequest.Types.Body.Types.Filter.FilterHeaderObjectType, ObjectType.Regular.ToString(), API.Object.MatchType.StringEqual);
                list = mb.Select(cid, fs);
                Assert.AreEqual(5, list.Count);
                Assert.IsTrue(list.Contains(small.Address));
                Assert.IsTrue(list.Contains(left.Address));
                Assert.IsTrue(list.Contains(right.Address));
                Assert.IsTrue(list.Contains(link.Address));
                Assert.IsTrue(list.Contains(parent.Address));
                fs = new();
                fs.AddFilter(SearchRequest.Types.Body.Types.Filter.FilterHeaderObjectType, ObjectType.Regular.ToString(), API.Object.MatchType.StringNotEqual);
                list = mb.Select(cid, fs);
                Assert.AreEqual(2, list.Count);
                Assert.IsTrue(list.Contains(ts.Address));
                Assert.IsTrue(list.Contains(sg.Address));
                fs = new();
                fs.AddFilter(SearchRequest.Types.Body.Types.Filter.FilterHeaderObjectType, "", API.Object.MatchType.NotPresent);
                Assert.AreEqual(0, mb.Select(cid, fs).Count);
                //tombstone objects
                fs = new();
                fs.AddFilter(SearchRequest.Types.Body.Types.Filter.FilterHeaderObjectType, ObjectType.Tombstone.ToString(), API.Object.MatchType.StringEqual);
                list = mb.Select(cid, fs);
                Assert.AreEqual(1, list.Count);
                Assert.IsTrue(list.Contains(ts.Address));
                fs = new();
                fs.AddFilter(SearchRequest.Types.Body.Types.Filter.FilterHeaderObjectType, ObjectType.Tombstone.ToString(), API.Object.MatchType.StringNotEqual);
                list = mb.Select(cid, fs);
                Assert.AreEqual(6, list.Count);
                Assert.IsTrue(list.Contains(small.Address));
                Assert.IsTrue(list.Contains(left.Address));
                Assert.IsTrue(list.Contains(right.Address));
                Assert.IsTrue(list.Contains(link.Address));
                Assert.IsTrue(list.Contains(parent.Address));
                Assert.IsTrue(list.Contains(sg.Address));
                fs = new();
                fs.AddFilter(SearchRequest.Types.Body.Types.Filter.FilterHeaderObjectType, "", API.Object.MatchType.NotPresent);
                Assert.AreEqual(0, mb.Select(cid, fs).Count);
                //storage group objects
                fs = new();
                fs.AddFilter(SearchRequest.Types.Body.Types.Filter.FilterHeaderObjectType, ObjectType.StorageGroup.ToString(), API.Object.MatchType.StringEqual);
                list = mb.Select(cid, fs);
                Assert.AreEqual(1, list.Count);
                Assert.IsTrue(list.Contains(sg.Address));
                fs = new();
                fs.AddFilter(SearchRequest.Types.Body.Types.Filter.FilterHeaderObjectType, ObjectType.StorageGroup.ToString(), API.Object.MatchType.StringNotEqual);
                list = mb.Select(cid, fs);
                Assert.AreEqual(6, list.Count);
                Assert.IsTrue(list.Contains(small.Address));
                Assert.IsTrue(list.Contains(left.Address));
                Assert.IsTrue(list.Contains(right.Address));
                Assert.IsTrue(list.Contains(link.Address));
                Assert.IsTrue(list.Contains(parent.Address));
                Assert.IsTrue(list.Contains(ts.Address));
                fs = new();
                fs.AddFilter(SearchRequest.Types.Body.Types.Filter.FilterHeaderObjectType, "", API.Object.MatchType.NotPresent);
                Assert.AreEqual(0, mb.Select(cid, fs).Count);
                //objects with parents
                fs = new();
                fs.AddFilter(SearchRequest.Types.Body.Types.Filter.FilterHeaderParent, parent.ObjectId.ToBase58String(), API.Object.MatchType.StringEqual);
                list = mb.Select(cid, fs);
                Assert.AreEqual(2, list.Count);
                Assert.IsTrue(list.Contains(right.Address));
                Assert.IsTrue(list.Contains(link.Address));
                fs = new();
                fs.AddFilter(SearchRequest.Types.Body.Types.Filter.FilterHeaderParent, "", API.Object.MatchType.NotPresent);
                Assert.AreEqual(0, mb.Select(cid, fs).Count);
                //all objects
                fs = new();
                Assert.AreEqual(7, mb.Select(cid, fs).Count);
            }
            finally
            {
                Directory.Delete(path, true);
            }
        }

        [TestMethod]
        public void TestSelectInhume()
        {
            string path = "./test_mb_TestSelectInhume";
            try
            {
                using MB mb = new(path);
                mb.Open();
                var cid = RandomContainerID();
                var obj1 = RandomObject(cid);
                mb.Put(obj1);
                var obj2 = RandomObject(cid);
                mb.Put(obj2);
                var fs = new SearchFilters();
                var list = mb.Select(cid, fs);
                Assert.AreEqual(2, list.Count);
                Assert.IsTrue(list.Contains(obj1.Address));
                Assert.IsTrue(list.Contains(obj2.Address));
                var ts = RandomAddress(cid);
                mb.Inhume(ts, obj2.Address);
                list = mb.Select(cid, fs);
                Assert.AreEqual(1, list.Count);
                Assert.IsTrue(list.Contains(obj1.Address));
            }
            finally
            {
                Directory.Delete(path, true);
            }
        }

        [TestMethod]
        public void TestSelectPayloadHash()
        {
            string path = "./test_mb_TestSelectPayloadHash";
            try
            {
                using MB mb = new(path);
                mb.Open();
                var cid = RandomContainerID();
                var obj1 = RandomObject(cid);
                mb.Put(obj1);
                var obj2 = RandomObject(cid);
                mb.Put(obj2);
                SearchFilters fs = new();
                fs.AddFilter(SearchRequest.Types.Body.Types.Filter.FilterHeaderPayloadHash, obj1.PayloadChecksum.Sum.ToByteArray().ToHexString(), API.Object.MatchType.StringEqual);
                var list = mb.Select(cid, fs);
                Assert.AreEqual(1, list.Count);
                Assert.IsTrue(list.Contains(obj1.Address));
                fs = new();
                fs.AddFilter(SearchRequest.Types.Body.Types.Filter.FilterHeaderPayloadHash, obj1.PayloadChecksum.Sum.ToByteArray().ToHexString(), API.Object.MatchType.StringNotEqual);
                list = mb.Select(cid, fs);
                Assert.AreEqual(1, list.Count);
                Assert.IsTrue(list.Contains(obj2.Address));
                fs = new();
                fs.AddFilter(SearchRequest.Types.Body.Types.Filter.FilterHeaderPayloadHash, "", API.Object.MatchType.NotPresent);
                list = mb.Select(cid, fs);
                Assert.AreEqual(0, list.Count);
            }
            finally
            {
                Directory.Delete(path, true);
            }
        }

        [TestMethod]
        public void TestSelectWithSlowFilters()
        {
            string path = "./test_mb_TestSelectWithSlowFilters";
            try
            {
                using MB mb = new(path);
                mb.Open();
                var cid = RandomContainerID();
                var v20 = new API.Refs.Version()
                {
                    Major = 2,
                    Minor = 0
                };
                var v21 = new API.Refs.Version()
                {
                    Major = 2,
                    Minor = 1
                };
                var obj1 = RandomObject(cid);
                obj1.Header.PayloadLength = 10;
                obj1.Header.CreationEpoch = 11;
                obj1.Header.Version = v20;
                mb.Put(obj1);
                var obj2 = RandomObject(cid);
                obj2.Header.PayloadLength = 20;
                obj2.Header.CreationEpoch = 21;
                obj2.Header.Version = v21;
                mb.Put(obj2);
                //object with tzhash
                SearchFilters fs = new();
                fs.AddFilter(SearchRequest.Types.Body.Types.Filter.FilterHeaderHomomorphicHash, obj1.PayloadHomomorphicHash.Sum.ToByteArray().ToHexString(), API.Object.MatchType.StringEqual);
                var list = mb.Select(cid, fs);
                Assert.AreEqual(1, list.Count);
                Assert.IsTrue(list.Contains(obj1.Address));
                fs = new();
                fs.AddFilter(SearchRequest.Types.Body.Types.Filter.FilterHeaderHomomorphicHash, obj1.PayloadHomomorphicHash.Sum.ToByteArray().ToHexString(), API.Object.MatchType.StringNotEqual);
                list = mb.Select(cid, fs);
                Assert.AreEqual(1, list.Count);
                Assert.IsTrue(list.Contains(obj2.Address));
                fs = new();
                fs.AddFilter(SearchRequest.Types.Body.Types.Filter.FilterHeaderHomomorphicHash, "", API.Object.MatchType.NotPresent);
                list = mb.Select(cid, fs);
                Assert.AreEqual(0, list.Count);
                //object with payload length
                fs = new();
                fs.AddFilter(SearchRequest.Types.Body.Types.Filter.FilterHeaderPayloadLength, "20", API.Object.MatchType.StringEqual);
                list = mb.Select(cid, fs);
                Assert.AreEqual(1, list.Count);
                Assert.IsTrue(list.Contains(obj2.Address));
                fs = new();
                fs.AddFilter(SearchRequest.Types.Body.Types.Filter.FilterHeaderPayloadLength, "20", API.Object.MatchType.StringNotEqual);
                list = mb.Select(cid, fs);
                Assert.AreEqual(1, list.Count);
                Assert.IsTrue(list.Contains(obj1.Address));
                fs = new();
                fs.AddFilter(SearchRequest.Types.Body.Types.Filter.FilterHeaderPayloadLength, "", API.Object.MatchType.NotPresent);
                list = mb.Select(cid, fs);
                Assert.AreEqual(0, list.Count);
                //object with creation epoch
                fs = new();
                fs.AddFilter(SearchRequest.Types.Body.Types.Filter.FilterHeaderCreationEpoch, "11", API.Object.MatchType.StringEqual);
                list = mb.Select(cid, fs);
                Assert.AreEqual(1, list.Count);
                Assert.IsTrue(list.Contains(obj1.Address));
                fs = new();
                fs.AddFilter(SearchRequest.Types.Body.Types.Filter.FilterHeaderCreationEpoch, "11", API.Object.MatchType.StringNotEqual);
                list = mb.Select(cid, fs);
                Assert.AreEqual(1, list.Count);
                Assert.IsTrue(list.Contains(obj2.Address));
                fs = new();
                fs.AddFilter(SearchRequest.Types.Body.Types.Filter.FilterHeaderCreationEpoch, "", API.Object.MatchType.NotPresent);
                list = mb.Select(cid, fs);
                Assert.AreEqual(0, list.Count);
                //object with version
                fs = new();
                fs.AddObjectVersionFilter(API.Object.MatchType.StringEqual, v21);
                list = mb.Select(cid, fs);
                Assert.AreEqual(1, list.Count);
                Assert.IsTrue(list.Contains(obj2.Address));
                fs = new();
                fs.AddObjectVersionFilter(API.Object.MatchType.StringNotEqual, v21);
                list = mb.Select(cid, fs);
                Assert.AreEqual(1, list.Count);
                Assert.IsTrue(list.Contains(obj1.Address));
                fs = new();
                fs.AddObjectVersionFilter(API.Object.MatchType.NotPresent, new());
                list = mb.Select(cid, fs);
                Assert.AreEqual(0, list.Count);
            }
            finally
            {
                Directory.Delete(path, true);
            }
        }

        [TestMethod]
        public void TestSelectObjectID()
        {
            string path = "./test_mb_TestSelectObjectID";
            try
            {
                using MB mb = new(path);
                mb.Open();
                var cid = RandomContainerID();
                var sid = new SplitID();
                var parent = RandomObject(cid);
                var regular = RandomObject(cid);
                regular.Parent = parent;
                regular.SplitId = sid;
                mb.Put(regular);
                var ts = RandomObject(cid);
                ts.ObjectType = ObjectType.Tombstone;
                mb.Put(ts);
                var sg = RandomObject(cid);
                sg.ObjectType = ObjectType.StorageGroup;
                mb.Put(sg);
                //not present
                SearchFilters fs = new();
                fs.AddObjectIDFilter(API.Object.MatchType.NotPresent, new());
                var list = mb.Select(cid, fs);
                Assert.AreEqual(0, list.Count);
                //not found objects
                fs = new();
                var raw = RandomObject(cid);
                fs.AddObjectIDFilter(API.Object.MatchType.StringEqual, raw.ObjectId);
                list = mb.Select(cid, fs);
                Assert.AreEqual(0, list.Count);
                fs = new();
                fs.AddObjectIDFilter(API.Object.MatchType.StringNotEqual, raw.ObjectId);
                list = mb.Select(cid, fs);
                Assert.AreEqual(4, list.Count);
                Assert.IsTrue(list.Contains(regular.Address));
                Assert.IsTrue(list.Contains(parent.Address));
                Assert.IsTrue(list.Contains(sg.Address));
                Assert.IsTrue(list.Contains(ts.Address));
                //regular objects
                fs = new();
                fs.AddObjectIDFilter(API.Object.MatchType.StringEqual, regular.ObjectId);
                list = mb.Select(cid, fs);
                Assert.AreEqual(1, list.Count);
                Assert.IsTrue(list.Contains(regular.Address));
                fs = new();
                fs.AddObjectIDFilter(API.Object.MatchType.StringNotEqual, regular.ObjectId);
                list = mb.Select(cid, fs);
                Assert.AreEqual(3, list.Count);
                Assert.IsTrue(list.Contains(parent.Address));
                Assert.IsTrue(list.Contains(sg.Address));
                Assert.IsTrue(list.Contains(ts.Address));
                //tombstone objects
                fs = new();
                fs.AddObjectIDFilter(API.Object.MatchType.StringEqual, ts.ObjectId);
                list = mb.Select(cid, fs);
                Assert.AreEqual(1, list.Count);
                Assert.IsTrue(list.Contains(ts.Address));
                fs = new();
                fs.AddObjectIDFilter(API.Object.MatchType.StringNotEqual, ts.ObjectId);
                list = mb.Select(cid, fs);
                Assert.AreEqual(3, list.Count);
                Assert.IsTrue(list.Contains(regular.Address));
                Assert.IsTrue(list.Contains(parent.Address));
                Assert.IsTrue(list.Contains(sg.Address));
                //storage group objects
                fs = new();
                fs.AddObjectIDFilter(API.Object.MatchType.StringEqual, sg.ObjectId);
                list = mb.Select(cid, fs);
                Assert.AreEqual(1, list.Count);
                Assert.IsTrue(list.Contains(sg.Address));
                fs = new();
                fs.AddObjectIDFilter(API.Object.MatchType.StringNotEqual, sg.ObjectId);
                list = mb.Select(cid, fs);
                Assert.AreEqual(3, list.Count);
                Assert.IsTrue(list.Contains(regular.Address));
                Assert.IsTrue(list.Contains(parent.Address));
                Assert.IsTrue(list.Contains(ts.Address));
                //parent objects
                fs = new();
                fs.AddObjectIDFilter(API.Object.MatchType.StringEqual, parent.ObjectId);
                list = mb.Select(cid, fs);
                Assert.AreEqual(1, list.Count);
                Assert.IsTrue(list.Contains(parent.Address));
                fs = new();
                fs.AddObjectIDFilter(API.Object.MatchType.StringNotEqual, parent.ObjectId);
                list = mb.Select(cid, fs);
                Assert.AreEqual(3, list.Count);
                Assert.IsTrue(list.Contains(regular.Address));
                Assert.IsTrue(list.Contains(sg.Address));
                Assert.IsTrue(list.Contains(ts.Address));
            }
            finally
            {
                Directory.Delete(path, true);
            }
        }

        [TestMethod]
        public void TestSelectSplitID()
        {
            string path = "./test_mb_TestSelectSplitID";
            try
            {
                using MB mb = new(path);
                mb.Open();
                var cid = RandomContainerID();
                var child1 = RandomObject(cid);
                var child2 = RandomObject(cid);
                var child3 = RandomObject(cid);
                var sid1 = new SplitID();
                var sid2 = new SplitID();
                child1.SplitId = sid1;
                child2.SplitId = sid1;
                child3.SplitId = sid2;
                mb.Put(child1);
                mb.Put(child2);
                mb.Put(child3);
                //not present
                SearchFilters fs = new();
                fs.AddFilter(SearchRequest.Types.Body.Types.Filter.FilterHeaderSplitID, "", API.Object.MatchType.NotPresent);
                Assert.AreEqual(0, mb.Select(cid, fs).Count);
                //split id
                fs = new();
                fs.AddFilter(SearchRequest.Types.Body.Types.Filter.FilterHeaderSplitID, sid1.ToString(), API.Object.MatchType.StringEqual);
                var list = mb.Select(cid, fs);
                Assert.AreEqual(2, list.Count);
                Assert.IsTrue(list.Contains(child1.Address));
                Assert.IsTrue(list.Contains(child2.Address));
                fs = new();
                fs.AddFilter(SearchRequest.Types.Body.Types.Filter.FilterHeaderSplitID, sid2.ToString(), API.Object.MatchType.StringEqual);
                list = mb.Select(cid, fs);
                Assert.AreEqual(1, list.Count);
                Assert.IsTrue(list.Contains(child3.Address));
                //empty split
                fs = new();
                fs.AddFilter(SearchRequest.Types.Body.Types.Filter.FilterHeaderSplitID, "", API.Object.MatchType.StringEqual);
                list = mb.Select(cid, fs);
                Assert.AreEqual(0, list.Count);
                //unkown split id
                fs = new();
                fs.AddFilter(SearchRequest.Types.Body.Types.Filter.FilterHeaderSplitID, new SplitID().ToString(), API.Object.MatchType.StringEqual);
                list = mb.Select(cid, fs);
                Assert.AreEqual(0, list.Count);
            }
            finally
            {
                Directory.Delete(path, true);
            }
        }

        [TestMethod]
        public void TestSelectContainerID()
        {
            string path = "./test_mb_TestSelectContainerID";
            try
            {
                using MB mb = new(path);
                mb.Open();
                var cid = RandomContainerID();
                var obj1 = RandomObject(cid);
                mb.Put(obj1);
                var obj2 = RandomObject(cid);
                mb.Put(obj2);
                //same cid
                SearchFilters fs = new();
                fs.AddObjectContainerIDFilter(API.Object.MatchType.StringEqual, cid);
                var list = mb.Select(cid, fs);
                Assert.AreEqual(2, list.Count);
                Assert.IsTrue(list.Contains(obj1.Address));
                Assert.IsTrue(list.Contains(obj2.Address));
                fs = new();
                fs.AddObjectContainerIDFilter(API.Object.MatchType.StringNotEqual, cid);
                list = mb.Select(cid, fs);
                Assert.AreEqual(2, list.Count);
                Assert.IsTrue(list.Contains(obj1.Address));
                Assert.IsTrue(list.Contains(obj2.Address));
                fs = new();
                fs.AddObjectContainerIDFilter(API.Object.MatchType.NotPresent, cid);
                list = mb.Select(cid, fs);
                Assert.AreEqual(0, list.Count);
                fs = new();
                fs.AddObjectContainerIDFilter(API.Object.MatchType.StringEqual, RandomContainerID());
                list = mb.Select(cid, fs);
                Assert.AreEqual(0, list.Count);
            }
            finally
            {
                Directory.Delete(path, true);
            }
        }

        [TestMethod]
        public void TestIsSmall()
        {
            string path = "./test_mb_TestIsSmall";
            try
            {
                using MB mb = new(path);
                mb.Open();
                var obj1 = RandomObject();
                var obj2 = RandomObject();
                BlobovniczaID bid = new byte[] { 1, 2, 3, 4 };
                Assert.IsNull(mb.IsSmall(obj1.Address));
                mb.Put(obj1, bid);
                mb.Put(obj2);
                Assert.IsNull(mb.IsSmall(obj2.Address));
                Assert.AreEqual(((byte[])bid).ToHexString(), ((byte[])mb.IsSmall(obj1.Address)).ToHexString());
            }
            finally
            {
                Directory.Delete(path, true);
            }
        }

        [TestMethod]
        public void TestAddressHashSet()
        {
            var set = new HashSet<Address>();
            var address = RandomAddress();
            set.Add(address);
            Assert.IsTrue(set.Contains(address));
            Assert.IsFalse(set.Add(address));
        }
    }
}
