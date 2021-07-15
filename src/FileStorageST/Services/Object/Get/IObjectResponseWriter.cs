using FSObject = Neo.FileStorage.API.Object.Object;

namespace Neo.FileStorage.Storage.Services.Object.Get
{
    public interface IObjectResponseWriter
    {
        void WriteHeader(FSObject obj);
        void WriteChunk(byte[] chunk);
    }
}
