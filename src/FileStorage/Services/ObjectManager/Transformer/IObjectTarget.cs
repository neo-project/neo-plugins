using Google.Protobuf;
using FSObject = Neo.FileStorage.API.Object.Object;

namespace Neo.FileStorage.Services.ObjectManager.Transformer
{
    public interface IObjectTarget
    {
        void WriteHeader(FSObject obj);
        void WriteChunk(byte[] chunk);
        AccessIdentifiers Close();
    }
}
