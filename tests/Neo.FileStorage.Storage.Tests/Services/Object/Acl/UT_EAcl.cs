using System;
using System.Collections.Generic;
using System.Linq;
using Google.Protobuf;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Neo.FileStorage.API.Acl;
using Neo.FileStorage.API.Cryptography;
using Neo.FileStorage.API.Object;
using Neo.FileStorage.API.Refs;
using Neo.FileStorage.Storage.Services.Object.Acl;
using Neo.FileStorage.Storage.Services.Object.Acl.EAcl;
using static Neo.FileStorage.Storage.Tests.Helper;
using FSObject = Neo.FileStorage.API.Object.Object;

namespace Neo.FileStorage.Tests.Services.Object.Acl
{
    [TestClass]
    public class UT_EAcl
    {
        private class TestLocalStorage : ILocalHeadSource
        {
            public Address ExpectAddress;
            public FSObject Object;

            public FSObject Head(Address address)
            {
                return Object;
            }
        }

        private class TestLocalEAclStorage : IEAclSource
        {
            public ContainerID ExpectContainerId;
            public EACLTable Table;

            public EACLTable GetEACL(ContainerID cid)
            {
                Assert.AreEqual(ExpectContainerId, cid);
                return Table;
            }
        }

        [TestMethod]
        public void TestHeadRequest()
        {
            var address = RandomAddress();
            var req = new HeadRequest
            {
                MetaHeader = new(),
                Body = new()
                {
                    Address = address,
                },
            };
            string xKey = "x-key", xValue = "x-val";
            req.MetaHeader.XHeaders.Add(new API.Session.XHeader { Key = xKey, Value = xValue });
            string attrKey = "attr_key", attrValue = "attr_val";
            var obj = RandomObject();
            obj.Header.Attributes.Add(new Header.Types.Attribute { Key = attrKey, Value = attrValue });
            var key = RandomPrivatekey().LoadPrivateKey();
            var table = new EACLTable();
            var r = new EACLRecord
            {
                Operation = Operation.Head,
                Action = API.Acl.Action.Deny,
            };
            r.Filters.Add(new EACLRecord.Types.Filter
            {
                HeaderType = HeaderType.Object,
                MatchType = API.Acl.MatchType.StringEqual,
                Key = attrKey,
                Value = attrValue,
            });
            r.Filters.Add(new EACLRecord.Types.Filter
            {
                HeaderType = HeaderType.Request,
                MatchType = API.Acl.MatchType.StringEqual,
                Key = xKey,
                Value = xValue,
            });
            var target = new EACLRecord.Types.Target
            {
                Role = Role.Unspecified,
            };
            target.Keys.Add(ByteString.CopyFrom(key.PublicKey()));
            r.Targets.Add(target);
            table.Records.Add(r);

            var lStorage = new TestLocalStorage
            {
                ExpectAddress = address,
                Object = obj,
            };
            var cid = address.ContainerId;
            var unit = new ValidateUnit
            {
                ContainerId = cid,
                Op = Operation.Head,
                Key = key.PublicKey(),
                HeaderSource = new HeaderSource(lStorage, address, req, null),
            };
            var eStorage = new TestLocalEAclStorage
            {
                ExpectContainerId = cid,
                Table = table,
            };
            var validator = new EAclValidator
            {
                EAclStorage = eStorage,
            };

            Assert.AreEqual(API.Acl.Action.Deny, validator.CalculateAction(unit));
            req.MetaHeader.XHeaders.Clear();
            Assert.AreEqual(API.Acl.Action.Allow, validator.CalculateAction(unit));
            req.MetaHeader.XHeaders.Add(new API.Session.XHeader { Key = xKey, Value = xValue });
            obj.Header.Attributes.Clear();
            Assert.AreEqual(API.Acl.Action.Allow, validator.CalculateAction(unit));
        }
    }
}
