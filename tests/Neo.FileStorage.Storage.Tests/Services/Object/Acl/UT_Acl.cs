using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Neo.FileStorage.API.Acl;
using Neo.FileStorage.Storage.Services.Object.Acl;
using static Neo.FileStorage.Storage.Services.Object.Acl.BasicAclHelper;

namespace Neo.FileStorage.Tests.Services.Object.Acl
{
    [TestClass]
    public class UT_BasicAclHelper
    {
        private const uint PrivateContainer = 0x1C8C8CCC;
        private const uint PublicContainerWithSticky = 0x3FFFFFFF;
        private const uint ReadonlyContainer = 0x1FFFCCFF;
        private readonly IEnumerable<Operation> allOperations = ((Operation[])Enum.GetValues(typeof(Operation))).Where(p => p != Operation.Unspecified);

        [TestMethod]
        public void TestPrivate()
        {
            var r = PrivateContainer;
            Assert.IsFalse(r.Sticky());
            foreach (var op in allOperations)
            {
                Assert.IsTrue(r.UserAllowed(op));
                Assert.IsFalse(r.OthersAllowed(op));
                if (op == Operation.Delete || op == Operation.Getrange)
                {
                    Assert.IsFalse(r.SystemAllowed(op));
                }
                else
                {
                    Assert.IsTrue(r.SystemAllowed(op));
                }
            }
        }

        [TestMethod]
        public void TestPublicWithSticky()
        {
            var r = PublicContainerWithSticky;
            Assert.IsTrue(r.Sticky());
            foreach (var op in allOperations)
            {
                Assert.IsTrue(r.UserAllowed(op));
                Assert.IsTrue(r.OthersAllowed(op));
                Assert.IsTrue(r.SystemAllowed(op));
                Assert.IsTrue(r.SystemAllowed(op));
            }
        }

        [TestMethod]
        public void TestReadOnly()
        {
            var r = ReadonlyContainer;
            Assert.IsFalse(r.Sticky());
            foreach (var op in allOperations)
            {
                Assert.IsTrue(r.UserAllowed(op));
                Assert.IsTrue(r.SystemAllowed(op));
                if (op == Operation.Delete || op == Operation.Put)
                {
                    Assert.IsFalse(r.OthersAllowed(op));
                }
                else
                {
                    Assert.IsTrue(r.OthersAllowed(op));
                }
            }
        }

        [TestMethod]
        public void TestPublicBasicAcl()
        {
            var b = (uint)BasicAcl.PublicBasicRule;
            Console.WriteLine(b);
            Assert.IsTrue(b.Final());
            b.ForbidOthers(Operation.Delete);
            Console.WriteLine(b);
        }
    }
}
