using Microsoft.VisualStudio.TestTools.UnitTesting;
using Neo.FileStorage.Morph.Event;

namespace Neo.FileStorage.Tests.Morph.Event
{
    [TestClass]
    public class UT_ScriptHashWithType
    {
        [TestMethod]
        public void TestEquals()
        {
            var t1 = new ScriptHashWithType()
            {
                Type = "test",
                ScriptHashValue = UInt160.Zero
            };
            var t2 = new ScriptHashWithType()
            {
                Type = "parser is null",
                ScriptHashValue = UInt160.Zero
            };
            var t3 = new ScriptHashWithType()
            {
                Type = "test with no handler",
                ScriptHashValue = UInt160.Zero
            };
            var t4 = new ScriptHashWithType()
            {
                Type = "test",
                ScriptHashValue = UInt160.Zero
            };
            var t5 = new ScriptHashWithType()
            {
                Type = null,
                ScriptHashValue = null
            };
            var t6 = new ScriptHashWithType()
            {
                Type = null,
                ScriptHashValue = UInt160.Zero
            };
            var t7 = new ScriptHashWithType()
            {
                Type = null,
                ScriptHashValue = UInt160.Zero
            };
            var t8 = new ScriptHashWithType()
            {
                Type = "test",
                ScriptHashValue = null
            };
            var t9 = new ScriptHashWithType()
            {
                Type = "test",
                ScriptHashValue = null
            };
            Assert.AreEqual(t1, t1);
            Assert.AreEqual(t1, t4);
            Assert.AreEqual(t6, t7);
            Assert.AreEqual(t8, t9);
            Assert.AreNotEqual(t1, t2);
            Assert.AreNotEqual(t1, null);
            Assert.AreNotEqual(t1, t3);
            Assert.AreNotEqual(t1, t5);
            Assert.AreNotEqual(t2, t3);
        }
    }
}
