using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Google.Protobuf;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Neo.FileStorage.API.Refs;
using Neo.FileStorage.Storage.LocalObjectStorage;
using Neo.FileStorage.Storage.LocalObjectStorage.Blobstor;
using Neo.IO.Data.LevelDB;
using static Neo.FileStorage.Storage.Tests.Helper;
using FSRange = Neo.FileStorage.API.Object.Range;

namespace Neo.FileStorage.Storage.Tests.LocalObjectStorage.Blobstor
{
    [TestClass]
    public class UT_BlobovnicaTree
    {
        [TestMethod]
        public void TestBlobovnicas()
        {
            string path = "./test_blzs";
            int size_limit = 2 << 10;
            BlobovniczaTree tree = new()
            {
                BlzRootPath = path,
                BlzShallowWidth = 2,
                BlzShallowDepth = 2,
                SmallSizeLimit = (ulong)size_limit,
            };
            tree.Open();
            try
            {
                var objSize = size_limit / 2;
                var minFitObjNum = 2 * 2 * size_limit / objSize;
                List<Address> addrs = new();
                for (int i = 0; i < minFitObjNum; i++)
                {
                    var obj = RandomObject(objSize);
                    addrs.Add(obj.Address);
                    var id = tree.Put(obj);
                    var res = tree.Get(obj.Address, id);
                    Assert.IsTrue(obj.ToByteArray().SequenceEqual(res.ToByteArray()));
                    res = tree.Get(obj.Address);
                    Assert.IsTrue(obj.ToByteArray().SequenceEqual(res.ToByteArray()));
                    var range = new FSRange
                    {
                        Offset = obj.PayloadSize / 3,
                        Length = obj.PayloadSize / 3
                    };
                    byte[] data = tree.GetRange(obj.Address, range, id);
                    Assert.AreEqual(obj.Payload.ToByteArray()[(int)range.Offset..(int)(range.Length + range.Offset)].ToHexString(), data.ToHexString());
                    data = tree.GetRange(obj.Address, range);
                    Assert.IsTrue(obj.Payload.ToByteArray()[(int)range.Offset..(int)(range.Length + range.Offset)].SequenceEqual(data));
                }
                foreach (var address in addrs)
                {
                    tree.Delete(address);
                    Assert.ThrowsException<ObjectNotFoundException>(() => tree.Get(address));
                    Assert.ThrowsException<ObjectNotFoundException>(() => tree.Delete(address));
                }
            }
            finally
            {
                Directory.Delete(path, true);
            }
        }
    }
}
