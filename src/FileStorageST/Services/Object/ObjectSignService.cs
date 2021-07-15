
using System;
using System.Threading;
using Neo.FileStorage.API.Object;
using Neo.FileStorage.API.Session;

namespace Neo.FileStorage.Storage.Services.Object
{
    public class ObjectSignService : SignService
    {
        public ObjectResponseService ResponseService { get; init; }

        public DeleteResponse Delete(DeleteRequest request, CancellationToken cancellation)
        {
            return (DeleteResponse)HandleUnaryRequest(request, resp =>
            {
                return ResponseService.Delete((DeleteRequest)resp, cancellation);
            });
        }

        public void Get(GetRequest request, Action<GetResponse> handler, CancellationToken cancellation)
        {
            ResponseService.Get(request, resp => HandleServerStreamRequest(request, resp =>
            {
                handler((GetResponse)resp);
            }), cancellation);
        }

        public void GetRange(GetRangeRequest request, Action<GetRangeResponse> handler, CancellationToken cancellation)
        {
            ResponseService.GetRange(request, resp => HandleServerStreamRequest(request, resp =>
            {
                handler((GetRangeResponse)resp);
            }), cancellation);
        }

        public GetRangeHashResponse GetRangeHash(GetRangeHashRequest request, CancellationToken cancellation)
        {
            return (GetRangeHashResponse)HandleUnaryRequest(request, resp =>
            {
                return ResponseService.GetRangeHash((GetRangeHashRequest)resp, cancellation);
            });
        }

        public HeadResponse Head(HeadRequest request, CancellationToken cancellation)
        {
            return (HeadResponse)HandleUnaryRequest(request, resp =>
            {
                return ResponseService.Head((HeadRequest)resp, cancellation);
            });
        }

        public IRequestStream Put(CancellationToken cancellation)
        {
            var next = ResponseService.Put(cancellation);
            return new PutSignStream
            {
                Stream = CreateRequestStreamer(request => next.Send((PutRequest)request), () => next.Close()),
            };
        }

        public void Search(SearchRequest request, Action<SearchResponse> handler, CancellationToken cancellation)
        {
            ResponseService.Search(request, resp => HandleServerStreamRequest(request, resp =>
            {
                handler((SearchResponse)resp);
            }), cancellation);
        }
    }

    public class PutSignStream : IRequestStream
    {
        public RequestSignStream Stream { get; init; }

        public void Send(IRequest request)
        {
            Stream.Send(request);
        }

        public IResponse Close()
        {
            return Stream.Close();
        }
    }
}
