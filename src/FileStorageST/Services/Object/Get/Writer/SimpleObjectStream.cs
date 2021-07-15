using Google.Protobuf;
using Neo.FileStorage.Storage.Utils;
using FSObject = Neo.FileStorage.API.Object.Object;

namespace Neo.FileStorage.Storage.Services.Object.Get.Writer
{
    public class SimpleObjectWriter : IObjectResponseWriter
    {
        public FSObject Obj { get; private set; }

        public void WriteHeader(FSObject obj)
        {
            Obj = obj;
            Obj.Payload = ByteString.Empty;
        }

        public void WriteChunk(byte[] chunk)
        {
            Obj.Payload = Obj.Payload.Concat(ByteString.CopyFrom(chunk));
        }
    }
}
