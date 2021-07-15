using System.IO;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Neo.FileStorage.Storage.LocalObjectStorage;
using Neo.FileStorage.Storage.LocalObjectStorage.Blob;
using static Neo.FileStorage.Storage.Tests.Helper;
using FSObject = Neo.FileStorage.API.Object.Object;

namespace Neo.FileStorage.Storage.Tests.LocalObjectStorage.Blob
{
    [TestClass]
    public class UT_Blobovnica
    {
        [TestMethod]
        public void TestOpenBlob()
        {
            string path = "./test_blz1/0000000000000000/0000000000000000/0100000000000000";
            using Blobovnicza blz = new(path);
            blz.Open();
            try
            {
                var obj = RandomObject(100);
                blz.Put(obj);
                Assert.IsFalse(blz.Get(obj.Address) is null);
            }
            finally
            {
                Directory.Delete(path, true);
            }
        }

        [TestMethod]
        public void TestBlobovnicza()
        {
            string path = "./test_blz2";
            using Blobovnicza blz = new(path, null);
            blz.Open();
            try
            {
                Assert.ThrowsException<ObjectNotFoundException>(() => blz.Get(RandomAddress()));
                FSObject obj = RandomObject(15 * 1 << 10);
                blz.Put(obj);
                FSObject obj_get = blz.Get(obj.Address);
                Assert.AreEqual(obj.ObjectId, obj_get.ObjectId);
                blz.Delete(obj.Address);
                Assert.ThrowsException<ObjectNotFoundException>(() => blz.Get(obj.Address));
            }
            finally
            {
                Directory.Delete(path, true);
            }
        }
    }
}
