using System;
using System.Security.Cryptography;
using System.Threading;
using Google.Protobuf;
using Neo.FileStorage.API.Client;
using Neo.FileStorage.API.Cryptography;
using Neo.FileStorage.API.Object;
using Neo.FileStorage.API.Session;
using FSObject = Neo.FileStorage.API.Object.Object;

namespace Neo.FileStorage.Storage.Services.Object.Get
{
    public class Forwarder
    {
        private readonly CancellationToken cancellation;
        private readonly ECDsa key;
        private readonly IRequest request;

        public Forwarder(ECDsa key, IRequest req, CancellationToken cancellation)
        {
            this.key = key;
            request = req;
            RequestMetaHeader meta = new();
            meta.Ttl = req.MetaHeader.Ttl - 1;
            meta.Origin = req.MetaHeader;
            request.MetaHeader = meta;
            key.SignRequest(request);
            this.cancellation = cancellation;
        }

        public FSObject Forward(IFSRawClient client)
        {
            switch (request)
            {
                case GetRequest getRequest:
                    return client.GetObject(getRequest).Result;
                case HeadRequest headRequest:
                    return client.GetObjectHeader(headRequest).Result;
                case GetRangeRequest getRangeRequest:
                    var data = client.GetObjectPayloadRangeData(getRangeRequest).Result;
                    return new()
                    {
                        Payload = ByteString.CopyFrom(data)
                    };
                default:
                    throw new InvalidOperationException($"{nameof(Forwarder)} request type not supported");
            }
        }
    }
}
