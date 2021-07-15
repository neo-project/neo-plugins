using System;
using System.Threading;
using Neo.FileStorage.API.Object;
using Neo.FileStorage.API.Session;

namespace Neo.FileStorage.Storage.Services.Object
{
    public class ObjectResponseService : ResponseService
    {
        public SplitService SplitService { get; init; }

        public DeleteResponse Delete(DeleteRequest request, CancellationToken cancellation)
        {
            return (DeleteResponse)HandleUnaryRequest(request, r =>
            {
                return SplitService.Delete((DeleteRequest)r, cancellation);
            });
        }

        public void Get(GetRequest request, Action<GetResponse> handler, CancellationToken cancellation)
        {
            SplitService.Get(request, resp => HandleServerStreamRequest(resp =>
            {
                handler((GetResponse)resp);
            }), cancellation);
        }

        public void GetRange(GetRangeRequest request, Action<GetRangeResponse> handler, CancellationToken cancellation)
        {
            SplitService.GetRange(request, resp => HandleServerStreamRequest(resp =>
            {
                handler((GetRangeResponse)resp);
            }), cancellation);
        }

        public GetRangeHashResponse GetRangeHash(GetRangeHashRequest request, CancellationToken cancellation)
        {
            return (GetRangeHashResponse)HandleUnaryRequest(request, r =>
            {
                return SplitService.GetRangeHash((GetRangeHashRequest)r, cancellation);
            });
        }

        public HeadResponse Head(HeadRequest request, CancellationToken cancellation)
        {
            return (HeadResponse)HandleUnaryRequest(request, r =>
            {
                return SplitService.Head((HeadRequest)r, cancellation);
            });
        }

        public IRequestStream Put(CancellationToken cancellation)
        {
            var next = SplitService.Put(cancellation);
            return CreateRequestStream(req => next.Send((PutRequest)req), () => next.Close());
        }

        public void Search(SearchRequest request, Action<SearchResponse> handler, CancellationToken cancellation)
        {
            SplitService.Search(request, resp => HandleServerStreamRequest(resp =>
            {
                handler((SearchResponse)resp);
            }), cancellation);
        }
    }

    public class PutResponseStream : IRequestStream
    {
        public RequestResponseStream Stream { get; init; }

        public void Send(IRequest request)
        {
            Stream.Send(request);
        }

        public IResponse Close()
        {
            return (PutResponse)Stream.Close();
        }
    }
}
