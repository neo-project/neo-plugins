using System;
using Google.Protobuf;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Neo.FileStorage.API.Netmap;
using H = Neo.FileStorage.Core.Container.Extension;
using V2Container = Neo.FileStorage.API.Container.Container;
using V2OwnerID = Neo.FileStorage.API.Refs.OwnerID;
using V2Version = Neo.FileStorage.API.Refs.Version;

namespace Neo.FileStorage.Tests.Core
{
    [TestClass]
    public class UT_Fmt
    {
        [TestMethod]
        public void TestCheckFormat()
        {
            var c = new V2Container();
            Assert.IsFalse(H.CheckFormat(c));

            var policy = new PlacementPolicy();
            c.PlacementPolicy = policy;
            Assert.IsFalse(H.CheckFormat(c));

            c.Version = V2Version.SDKVersion();
            Assert.IsFalse(H.CheckFormat(c));

            c.OwnerId = V2OwnerID.FromByteArray(new byte[25]);
            Assert.IsFalse(H.CheckFormat(c));

            c.Nonce = ByteString.CopyFrom(Guid.NewGuid().ToByteArray());
            Assert.IsTrue(H.CheckFormat(c));
        }
    }
}
