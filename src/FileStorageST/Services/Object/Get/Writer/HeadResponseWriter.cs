using Google.Protobuf;
using Neo.FileStorage.API.Object;
using Neo.FileStorage.Utils;
using FSObject = Neo.FileStorage.API.Object.Object;

namespace Neo.FileStorage.Storage.Services.Object.Get.Writer
{
    public class HeadResponseWriter : IObjectResponseWriter
    {
        public bool Short { get; init; }
        public HeadResponse Response;

        public void WriteHeader(FSObject obj)
        {
            if (Short)
                Response.Body = ToShortHeader(obj);
            else
                Response.Body = ToFullHeader(obj);
        }

        public void WriteChunk(byte[] chunk)
        {

        }

        private HeadResponse.Types.Body ToFullHeader(FSObject obj)
        {
            return new HeadResponse.Types.Body
            {
                Header = new HeaderWithSignature
                {
                    Header = obj.Header,
                    Signature = obj.Signature,
                }
            };
        }

        private HeadResponse.Types.Body ToShortHeader(FSObject obj)
        {
            return new HeadResponse.Types.Body
            {
                ShortHeader = new ShortHeader
                {
                    Version = obj.Header.Version,
                    OwnerId = obj.Header.OwnerId,
                    CreationEpoch = obj.Header.CreationEpoch,
                    ObjectType = obj.Header.ObjectType,
                    PayloadLength = obj.Header.PayloadLength,
                }
            };
        }
    }
}
