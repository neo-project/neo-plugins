using Microsoft.VisualStudio.TestTools.UnitTesting;
using Neo.FileStorage.API.Acl;
using Neo.FileStorage.API.Refs;
using Neo.FileStorage.Storage.Services.Object.Acl.EAcl;
using static Neo.FileStorage.Storage.Tests.Helper;

namespace Neo.FileStorage.Tests.Services.Object.Acl
{
    [TestClass]
    public class UT_EAclValidator
    {
        private class TestEAclStorage : IEAclSource
        {
            public EACLTable GetEACL(ContainerID cid)
            {
                var target = new EACLRecord.Types.Target
                {
                    Role = Role.Others,
                };
                var record = new EACLRecord
                {
                    Operation = Operation.Delete,
                    Action = Action.Deny,
                };
                record.Targets.Add(target);
                var eacl = new EACLTable
                {
                    Version = Version.SDKVersion(),
                    ContainerId = cid,
                };
                eacl.Records.Add(record);
                return eacl;
            }
        }

        [TestMethod]
        public void TestEAclCheck()
        {
            var validator = new EAclValidator
            {
                EAclStorage = new TestEAclStorage(),
            };
            var unit = new ValidateUnit
            {
                ContainerId = RandomContainerID(),
                Role = Role.Others,
                Op = Operation.Delete,
                HeaderSource = null,
                Key = null,
                Bearer = null,
            };
            Assert.AreEqual(Action.Deny, validator.CalculateAction(unit));
        }
    }
}
