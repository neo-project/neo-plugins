using System;
using System.Threading;
using Neo.FileStorage.API.Client;
using Neo.FileStorage.Storage.Services.Object.Util;
using Neo.FileStorage.Storage.Services.Reputaion.Local.Client;
using FSObject = Neo.FileStorage.API.Object.Object;

namespace Neo.FileStorage.Storage.Services.Object.Head
{
    public class RemoteHeader
    {
        public const int DefaultHeadTtl = 1;
        public KeyStorage KeyStorage { get; init; }
        public ReputationClientCache ClientCache { get; init; }

        public FSObject Head(RemoteHeadPrm prm, CancellationToken context)
        {
            var key = KeyStorage.GetKey(prm.SessionToken);
            if (key is null)
                throw new InvalidOperationException($"{nameof(RemoteHeader)} could not receive private key");
            var client = ClientCache.Get(prm.Node);
            if (client is null)
                throw new InvalidOperationException($"{nameof(RemoteHeader)} could not create SDK client {prm.Node}");
            var header = client.GetObjectHeader(prm.Address, prm.Short, prm.Raw, new CallOptions
            {
                Ttl = DefaultHeadTtl,
                Session = prm.SessionToken,
                Bearer = prm.BearerToken,
                Key = key,
            }, context).Result;
            if (header is null)
                throw new InvalidOperationException(nameof(RemoteHeader) + $" could not read object payload range from {prm.Node}");
            return header;
        }
    }
}
