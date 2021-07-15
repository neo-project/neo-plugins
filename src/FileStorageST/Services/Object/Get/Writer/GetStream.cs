using Google.Protobuf;
using Neo.FileStorage.API.Object;
using System;
using FSObject = Neo.FileStorage.API.Object.Object;

namespace Neo.FileStorage.Storage.Services.Object.Get.Writer
{
    public class GetStream : IObjectResponseWriter
    {
        public Action<GetResponse> Handler { get; init; }

        public void WriteHeader(FSObject obj)
        {
            var resp = GetInitResponse(obj);
            Handler(resp);
        }

        public void WriteChunk(byte[] chunk)
        {
            var resp = GetChunkResponse(ByteString.CopyFrom(chunk));
            Handler(resp);
        }

        private GetResponse GetInitResponse(FSObject obj)
        {
            var resp = new GetResponse
            {
                Body = new GetResponse.Types.Body
                {
                    Init = new GetResponse.Types.Body.Types.Init
                    {
                        Header = obj.Header,
                        ObjectId = obj.ObjectId,
                        Signature = obj.Signature,
                    }
                }
            };
            return resp;
        }

        private GetResponse GetChunkResponse(ByteString chunk)
        {
            var resp = new GetResponse
            {
                Body = new GetResponse.Types.Body
                {
                    Chunk = chunk,
                }
            };
            return resp;
        }
    }
}
