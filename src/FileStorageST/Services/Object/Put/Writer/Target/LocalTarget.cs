using Google.Protobuf;
using Neo.FileStorage.Storage.LocalObjectStorage.Engine;
using Neo.FileStorage.Storage.Services.ObjectManager.Transformer;
using FSObject = Neo.FileStorage.API.Object.Object;

namespace Neo.FileStorage.Storage.Services.Object.Put
{
    public class LocalTarget : IObjectTarget
    {
        public StorageEngine LocalStorage { get; init; }

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
            LocalStorage.Put(obj);
            return new()
            {
                Self = obj.ObjectId,
            };
        }
    }
}
