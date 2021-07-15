using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Threading.Tasks;
using Grpc.Core;
using Neo.FileStorage.API.Refs;
using Neo.FileStorage.Morph.Invoker;
using Neo.FileStorage.Storage.LocalObjectStorage.Engine;
using Neo.FileStorage.Storage.Services.Control.Service;
using APINodeInfo = Neo.FileStorage.API.Netmap.NodeInfo;

namespace Neo.FileStorage.Storage.Services.Control
{
    public class ControlServiceImpl : ControlService.ControlServiceBase
    {
        public ECDsa Key { get; init; }
        public StorageEngine LocalStorage { get; init; }
        public MorphInvoker MorphInvoker { get; init; }
        public StorageService StorageNode { get; init; }
        private readonly HashSet<byte[]> allowKeys = new();

        public override Task<DropObjectsResponse> DropObjects(DropObjectsRequest request, ServerCallContext context)
        {
            return Task.Run(() =>
            {
                if (!IsValidRequest(request)) throw new RpcException(new Status(StatusCode.PermissionDenied, ""));
                Address[] addresses = new Address[request.Body.AddressList.Count];
                for (int i = 0; i < request.Body.AddressList.Count; i++)
                {
                    addresses[i] = Address.Parser.ParseFrom(request.Body.AddressList[i]);
                }
                LocalStorage.Delete(addresses);
                var resp = new DropObjectsResponse
                {
                    Body = new DropObjectsResponse.Types.Body(),
                };
                Key.SignMessage(resp);
                return resp;
            }, context.CancellationToken);
        }

        public override Task<HealthCheckResponse> HealthCheck(HealthCheckRequest request, ServerCallContext context)
        {
            return Task.Run(() =>
            {
                if (!IsValidRequest(request)) throw new RpcException(new Status(StatusCode.PermissionDenied, ""));
                var resp = new HealthCheckResponse
                {
                    Body = new HealthCheckResponse.Types.Body
                    {
                        NetmapStatus = StorageNode.NetmapStatus,
                        HealthStatus = StorageNode.HealthStatus,
                    }
                };
                Key.SignMessage(resp);
                return resp;
            }, context.CancellationToken);
        }

        public override Task<NetmapSnapshotResponse> NetmapSnapshot(NetmapSnapshotRequest request, ServerCallContext context)
        {
            return Task.Run(() =>
            {
                if (!IsValidRequest(request)) throw new RpcException(new Status(StatusCode.PermissionDenied, ""));
                ulong epoch = MorphInvoker.Epoch();
                var nm = MorphInvoker.EpochSnapshot(epoch);
                var netmap = new Service.Netmap
                {
                    Epoch = epoch,
                };
                netmap.Nodes.AddRange(nm.Nodes.Select(p => NodeInfoFromAPI(p.Info)));
                var resp = new NetmapSnapshotResponse
                {
                    Body = new NetmapSnapshotResponse.Types.Body
                    {
                        Netmap = netmap,
                    },
                };
                Key.SignMessage(resp);
                return resp;
            }, context.CancellationToken);
        }

        public override Task<SetNetmapStatusResponse> SetNetmapStatus(SetNetmapStatusRequest request, ServerCallContext context)
        {
            return Task.Run(() =>
            {
                if (!IsValidRequest(request)) throw new RpcException(new Status(StatusCode.PermissionDenied, ""));
                StorageNode.SetStatus(request.Body.Status);
                var resp = new SetNetmapStatusResponse
                {
                    Body = new SetNetmapStatusResponse.Types.Body { }
                };
                Key.SignMessage(resp);
                return resp;
            }, context.CancellationToken);
        }

        private bool IsValidRequest(ISignedMessage message)
        {
            var key = message.Signature.Key.ToByteArray();
            if (!allowKeys.Contains(key)) return false;
            return message.VerifyMessage();
        }

        private NodeInfo NodeInfoFromAPI(APINodeInfo n)
        {
            NodeInfo ni = new()
            {
                PublicKey = n.PublicKey,
                Address = n.Address,
                State = (NetmapStatus)n.State,
            };
            ni.Attributes.AddRange(n.Attributes.Select(p => AttributeFromAPI(p)));
            return ni;
        }

        private NodeInfo.Types.Attribute AttributeFromAPI(APINodeInfo.Types.Attribute a)
        {
            return new()
            {
                Key = a.Key,
                Value = a.Value,
            };
        }
    }
}
