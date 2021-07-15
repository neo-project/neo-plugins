using Grpc.Core;
using Google.Protobuf;
using Neo.FileStorage.API.Object;
using Neo.FileStorage.Storage.Services.Object.Util;
using System;
using FSObject = Neo.FileStorage.API.Object.Object;

namespace Neo.FileStorage.Storage.Services.Object.Get.Writer
{
    public class RangeStream : IObjectResponseWriter
    {
        public Action<GetRangeResponse> Handler { get; init; }

        public void WriteHeader(FSObject obj)
        {
            throw new NotImplementedException($"{nameof(WriteHeader)} shouldn't writer header in range stream");
        }

        public void WriteChunk(byte[] chunk)
        {
            Handler(GetRangeResponse(ByteString.CopyFrom(chunk)));
        }

        private GetRangeResponse GetRangeResponse(ByteString chunk)
        {
            var resp = new GetRangeResponse
            {
                Body = new GetRangeResponse.Types.Body
                {
                    Chunk = chunk,
                }
            };
            return resp;
        }
    }
}
