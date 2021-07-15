using System;
using System.Collections.Generic;
using System.Threading;
using Google.Protobuf;
using Neo.FileStorage.API.Client;
using Neo.FileStorage.API.Cryptography;
using Neo.FileStorage.API.Object;
using Neo.FileStorage.API.Session;
using Neo.FileStorage.Core.Object;
using Neo.FileStorage.Morph.Invoker;
using Neo.FileStorage.Placement;
using Neo.FileStorage.Storage.Services.Object.Util;
using Neo.FileStorage.Storage.Services.ObjectManager.Transformer;

namespace Neo.FileStorage.Storage.Services.Object.Put.Writer
{
    public class PutStream : IRequestStream
    {
        public PutService PutService { get; init; }
        public CancellationToken Cancellation { get; init; }

        private Traverser traverser;
        private IObjectTarget target;
        private PutRequest init;
        private readonly List<PutRequest> chunks = new();
        private ulong maxObjectSize;
        private bool saveChunks;
        private ulong writenPayloadSize;
        private ulong payloadSize;

        public PutInitPrm ToInitPrm(PutRequest request)
        {
            var prm = PutInitPrm.FromRequest(request);
            prm.Relay = RelayRequest;
            return prm;
        }

        public void Send(IRequest request)
        {
            if (request is not PutRequest putRequest)
                throw new InvalidOperationException($"{nameof(PutStream)} invalid object put request");
            switch (putRequest.Body.ObjectPartCase)
            {
                case PutRequest.Types.Body.ObjectPartOneofCase.Init:
                    var init_prm = ToInitPrm(init);
                    Init(init_prm);
                    saveChunks = init.Body.Init.Signature is not null;
                    if (saveChunks)
                    {
                        payloadSize = init.Body.Init.Header.PayloadLength;
                        if (maxObjectSize < payloadSize)
                            throw new InvalidOperationException("payload size is greater than the limit");
                        init = putRequest;
                    }
                    break;
                case PutRequest.Types.Body.ObjectPartOneofCase.Chunk:
                    if (saveChunks)
                    {
                        writenPayloadSize += (ulong)putRequest.Body.Chunk.Length;
                        if (payloadSize < writenPayloadSize)
                            throw new InvalidOperationException("wrong payload size");
                    }
                    Chunk(putRequest.Body.Chunk);
                    if (saveChunks)
                        chunks.Add(putRequest);
                    break;
                default:
                    throw new InvalidOperationException($"{nameof(PutStream)} invalid object put request");
            }
            if (!saveChunks) return;
            var metaHdr = new RequestMetaHeader();
            var meta = request.MetaHeader;
            metaHdr.Ttl = meta.Ttl - 1;
            metaHdr.Origin = meta;
            request.MetaHeader = metaHdr;
            var key = PutService.KeyStorage.GetKey(meta.SessionToken);
            key.SignRequest(request);
        }

        public IResponse Close()
        {
            if (target is null)
                throw new InvalidOperationException($"{nameof(PutStream)} target not initilized");
            var ids = target.Close();
            var id = ids.Parent ?? ids.Self;
            return new PutResponse
            {
                Body = new PutResponse.Types.Body
                {
                    ObjectId = id,
                }
            };
        }

        public void Init(PutInitPrm prm)
        {
            InitTarget(prm);
            target.WriteHeader(prm.Header);
        }

        private void InitTarget(PutInitPrm prm)
        {
            if (target is not null)
                throw new Exception($"{nameof(PutStream)} init recall");
            PrepareInitPrm(prm);
            if (prm.Header.Signature is not null)
            {
                target = new ValidationTarget
                {
                    ObjectValidator = new ObjectValidator(PutService.ObjectInhumer, PutService.MorphInvoker),
                    Next = NewCommonTarget(prm),
                };
                return;
            }
            var key = PutService.KeyStorage.GetKey(prm.SessionToken);
            maxObjectSize = PutService.MorphInvoker.MaxObjectSize();
            if (maxObjectSize == 0) throw new InvalidOperationException($"{nameof(PutStream)} could not obtain max object size parameter");
            target = new PayloadSizeLimiterTarget(maxObjectSize, new FormatTarget
            {
                Key = key,
                SessionToken = prm.SessionToken,
                MorphInvoker = PutService.MorphInvoker,
                Next = NewCommonTarget(prm)
            });
        }

        private void PrepareInitPrm(PutInitPrm prm)
        {
            var nm = PutService.MorphInvoker.Snapshot(0);
            var container = PutService.MorphInvoker.GetContainer(prm.Header.ContainerId)?.Container;
            var builder = new NetworkMapBuilder(nm);

            if (prm.Local)
            {
                traverser = new Traverser(new LocalPlacementBuilder(builder, PutService.LocalAddress), container.PlacementPolicy, prm.Header.Address, 1);
                return;
            }
            traverser = new Traverser(builder, container.PlacementPolicy, prm.Header.Address);
        }

        public void Chunk(ByteString chunk)
        {
            if (target is null)
                throw new InvalidOperationException($"{nameof(PutStream)} target not initilized");
            target.WriteChunk(chunk.ToByteArray());
        }

        public async void RelayRequest(IFSClient client)
        {
            if (init is null) return;
            using var stream = await client.Raw().PutObject(init, context: Cancellation);
            foreach (var chunk in chunks)
                stream.Write(chunk);
            await stream.Close();
        }

        public DistributeTarget NewCommonTarget(PutInitPrm prm)
        {
            Action<Network.Address> relay = null;
            if (prm.Relay is not null)
            {
                relay = address =>
                {
                    if (address.Equals(PutService.LocalAddress))
                        return;
                    var c = PutService.ClientCache.Get(address);
                    prm.Relay(c);
                };
            }
            return new DistributeTarget
            {
                LocalAddress = PutService.LocalAddress,
                Traverser = traverser,
                Relay = relay,
                ObjectValidator = new ObjectValidator(PutService.ObjectInhumer, PutService.MorphInvoker),
                NodeTargetInitializer = address =>
                {
                    if (address == PutService.LocalAddress)
                        return new LocalTarget
                        {
                            LocalStorage = PutService.LocalStorage,
                        };
                    return new RemoteTarget
                    {
                        Cancellation = Cancellation,
                        KeyStorage = PutService.KeyStorage,
                        Prm = prm,
                        Address = address,
                        ClientCache = PutService.ClientCache,
                    };
                }
            };
        }
    }
}
