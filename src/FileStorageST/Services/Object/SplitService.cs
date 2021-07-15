using System;
using System.Linq;
using System.Threading;
using Google.Protobuf;
using Neo.FileStorage.API.Object;

namespace Neo.FileStorage.Storage.Services.Object
{
    public class SplitService
    {
        public const int MaxMsgSize = 4 << 20;
        public const int AddressesSize = 72;
        public const int MaxChunkSize = MaxMsgSize * 3 / 4;
        public const int MaxAddrAmount = MaxChunkSize / AddressesSize;

        public int ChunkSize { get; init; } = MaxChunkSize;
        public int AddressAmount { get; init; } = MaxAddrAmount;
        public ObjectService ObjectService { get; init; }

        public DeleteResponse Delete(DeleteRequest request, CancellationToken cancellation)
        {
            return ObjectService.Delete(request, cancellation);
        }

        public void Get(GetRequest request, Action<GetResponse> handler, CancellationToken cancellation)
        {
            ObjectService.Get(request, resp =>
            {
                switch (resp.Body.ObjectPartCase)
                {
                    case GetResponse.Types.Body.ObjectPartOneofCase.Init:
                    case GetResponse.Types.Body.ObjectPartOneofCase.SplitInfo:
                        handler(resp);
                        break;
                    case GetResponse.Types.Body.ObjectPartOneofCase.Chunk:
                        var buffer = resp.Body.Chunk.ToByteArray().AsEnumerable();
                        while (buffer.Any())
                        {
                            var chunk = buffer.Take(ChunkSize);
                            resp.Body.Chunk = ByteString.CopyFrom(chunk.ToArray());
                            handler(resp);
                            buffer = buffer.Skip(chunk.Count());
                        }
                        break;
                    default:
                        throw new InvalidOperationException($"{nameof(SplitService)} invalid {resp.GetType()}");
                }
            }, cancellation);
        }

        public void GetRange(GetRangeRequest request, Action<GetRangeResponse> handler, CancellationToken cancellation)
        {
            ObjectService.GetRange(request, resp =>
            {
                switch (resp.Body.RangePartCase)
                {
                    case GetRangeResponse.Types.Body.RangePartOneofCase.None:
                    case GetRangeResponse.Types.Body.RangePartOneofCase.SplitInfo:
                        handler(resp);
                        break;
                    case GetRangeResponse.Types.Body.RangePartOneofCase.Chunk:
                        var buffer = resp.Body.Chunk.ToByteArray().AsEnumerable();
                        while (buffer.Any())
                        {
                            var chunk = buffer.Take(ChunkSize);
                            resp.Body.Chunk = ByteString.CopyFrom(chunk.ToArray());
                            buffer = buffer.Skip(chunk.Count());
                            handler(resp);
                        }
                        break;
                    default:
                        throw new InvalidOperationException($"{nameof(SplitService)} invalid {resp.GetType()}");
                }
            }, cancellation);
        }

        public GetRangeHashResponse GetRangeHash(GetRangeHashRequest request, CancellationToken cancellation)
        {
            return ObjectService.GetRangeHash(request, cancellation);
        }

        public HeadResponse Head(HeadRequest request, CancellationToken cancellation)
        {
            return ObjectService.Head(request, cancellation);
        }

        public IRequestStream Put(CancellationToken cancellation)
        {
            return ObjectService.Put(cancellation);
        }

        public void Search(SearchRequest request, Action<SearchResponse> handler, CancellationToken cancellation)
        {
            ObjectService.Search(request, resp =>
            {
                var ids = resp.Body.IdList.AsEnumerable();
                while (ids.Any())
                {
                    SearchResponse r = new();
                    r.MetaHeader = resp.MetaHeader;
                    r.VerifyHeader = resp.VerifyHeader;
                    r.Body = new SearchResponse.Types.Body();
                    r.Body.IdList.AddRange(ids.Take(AddressAmount));
                    ids = ids.Skip(r.Body.IdList.Count);
                    handler(r);
                }
            }, cancellation);
        }
    }
}
