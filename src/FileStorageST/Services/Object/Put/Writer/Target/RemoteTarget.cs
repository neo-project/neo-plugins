using System.Threading;
using Google.Protobuf;
using Neo.FileStorage.API.Refs;
using Neo.FileStorage.Storage.Services.Object.Util;
using Neo.FileStorage.Storage.Services.ObjectManager.Transformer;
using Neo.FileStorage.Storage.Services.Reputaion.Local.Client;
using FSObject = Neo.FileStorage.API.Object.Object;

namespace Neo.FileStorage.Storage.Services.Object.Put
{
    public class RemoteTarget : IObjectTarget
    {

        public CancellationToken Cancellation { get; init; }
        public KeyStorage KeyStorage { get; init; }
        public PutInitPrm Prm { get; init; }
        public Network.Address Address { get; init; }
        public ReputationClientCache ClientCache { get; init; }

        private FSObject obj;
        private byte[] payload;
        private int offset;

        public void WriteHeader(FSObject header)
        {
            obj = header;
            payload = new byte[obj.PayloadSize];
            offset = 0;
        }

        public void WriteChunk(byte[] chunk)
        {
            chunk.CopyTo(payload, offset);
            offset += chunk.Length;
        }

        public AccessIdentifiers Close()
        {
            obj.Payload = ByteString.CopyFrom(payload);
            var key = KeyStorage.GetKey(Prm?.SessionToken);
            var client = ClientCache.Get(Address);
            var id = client.PutObject(obj, Prm.CallOptions.WithTTL(1).WithKey(key), Cancellation).Result;
            return new()
            {
                Self = id,
            };
        }
    }
}
