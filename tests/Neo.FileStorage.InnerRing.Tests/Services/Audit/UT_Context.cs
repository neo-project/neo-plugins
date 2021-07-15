using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Akka.TestKit.Xunit2;
using Google.Protobuf;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Neo.FileStorage.API.Acl;
using Neo.FileStorage.API.Audit;
using Neo.FileStorage.API.Container;
using Neo.FileStorage.API.Cryptography;
using Neo.FileStorage.API.Cryptography.Tz;
using Neo.FileStorage.API.Netmap;
using Neo.FileStorage.API.Object;
using Neo.FileStorage.API.Refs;
using Neo.FileStorage.API.StorageGroup;
using Neo.FileStorage.InnerRing.Services.Audit;
using Neo.FileStorage.InnerRing.Services.Audit.Auditor;
using Neo.FileStorage.Utils;
using FSContainer = Neo.FileStorage.API.Container.Container;
using FSObject = Neo.FileStorage.API.Object.Object;
using FSRange = Neo.FileStorage.API.Object.Range;

namespace Neo.FileStorage.InnerRing.Tests.Services.Audit
{
    [TestClass]
    public class UT_Context : TestKit
    {
        public class Reporter : IReporter
        {
            public Action<Report> ExpectCheck { get; init; }

            public void WriteReport(Report r)
            {
                ExpectCheck(r);
            }
        }

        public class TestContainerCommunacator : IContainerCommunicator
        {
            public FSContainer Container { get; init; }
            public FSObject Object { get; init; }

            public StorageGroup GetStorageGroup(AuditTask task, ObjectID oid)
            {
                StorageGroup sg = new()
                {
                    ValidationDataSize = Object.PayloadSize,
                    ValidationHash = Object.PayloadHomomorphicHash,
                    ExpirationEpoch = 0,
                };
                sg.Members.Add(Object.ObjectId);
                return sg;
            }

            public FSObject GetHeader(AuditTask task, Node node, ObjectID oid, bool relay)
            {
                return Object.CutPayload();
            }

            public byte[] GetRangeHash(AuditTask task, Node node, ObjectID oid, FSRange range)
            {
                return new TzHash().ComputeHash(Object.Payload.ToByteArray()[(int)range.Offset..(int)(range.Offset + range.Length)]);
            }
        }

        [TestMethod]
        public void TestAuditManager()
        {
            var key = "L4kWTNckyaWn2QdUrACCJR1qJNgFFGhTCy63ERk7ZK3NvBoXap6t".LoadWif();
            int cap = 3;
            ulong max_pdp_internal = 3000;
            FSContainer container = new()
            {
                Version = API.Refs.Version.SDKVersion(),
                OwnerId = key.ToOwnerID(),
                NonceUUID = Guid.NewGuid(),
                BasicAcl = (uint)BasicAcl.PublicBasicRule,
                PlacementPolicy = new(2, new Replica[] { new Replica(2, "") }, null, null)
            };
            byte[] payload = Enumerable.Repeat((byte)0xff, 4 * TzHash.TzHashLength + 1).ToArray();
            FSObject obj = new()
            {

                Header = new Header
                {
                    OwnerId = key.ToOwnerID(),
                    ContainerId = container.CalCulateAndGetId,
                    PayloadLength = (ulong)payload.Length,
                    HomomorphicHash = new()
                    {
                        Type = ChecksumType.Tz,
                        Sum = ByteString.CopyFrom(new TzHash().ComputeHash(payload)),
                    },
                },
                Payload = ByteString.CopyFrom(payload),
            };
            obj.ObjectId = obj.CalculateID();
            obj.Signature = obj.CalculateIDSignature(key);
            IContainerCommunicator communicator = new TestContainerCommunacator
            {
                Container = container,
                Object = obj
            };
            Node node1 = new(0, new()
            {
                Address = "localhost",
                PublicKey = ByteString.CopyFrom(key.PublicKey()),
            });
            Node node2 = new(0, new()
            {
                Address = "localhost",
                PublicKey = ByteString.CopyFrom(key.PublicKey().Reverse().ToArray()),
            });
            NetMap nm = new(new List<Node> { node1, node2 });
            List<List<Node>> container_nodes = new() { new() { node1, node2 } };
            CancellationTokenSource source = new();
            AuditTask task = new()
            {
                Cancellation = source.Token,
                ContainerID = container.CalCulateAndGetId,
                Container = container,
                Netmap = nm,
                ContainerNodes = container_nodes,
                SGList = new List<ObjectID> { obj.ObjectId }
            };
            FileStorage.InnerRing.Services.Audit.Auditor.Context context = new()
            {
                ContainerCommunacator = communicator,
                AuditTask = task,
                MaxPDPInterval = max_pdp_internal,
                PorPool = ActorOf(WorkerPool.Props("AuditPOR", cap)),
                PdpPool = ActorOf(WorkerPool.Props("AuditPDP", cap))
            };
            task.Reporter = new Reporter
            {
                ExpectCheck = report =>
                {
                    var result = report.Result();
                    Assert.IsTrue(result.Complete);
                    Assert.AreEqual(1u, result.Requests);
                    Assert.AreEqual(0u, result.Retries);
                    Assert.AreEqual(1u, result.Hit);
                    Assert.AreEqual(0u, result.Miss);
                    Assert.AreEqual(0u, result.Fail);
                    Assert.AreEqual(1, result.PassSg.Count);
                    Assert.AreEqual(obj.ObjectId, result.PassSg[0]);
                    Assert.AreEqual(0, result.FailSg.Count);
                    Assert.AreEqual(2, result.PassNodes.Count);
                    Assert.IsTrue(key.PublicKey().SequenceEqual(result.PassNodes[0].ToByteArray()));
                }
            };
            context.Execute();
        }

        [TestMethod]
        public void AuditResultSerialize()
        {
            var r = DataAuditResult.Parser.ParseFrom(new byte[0]);
            Assert.IsNotNull(r);
            Assert.IsNull(r.ContainerId);
        }
    }
}
