using System;
using System.Threading;
using Neo.FileStorage.API.Object;
using Neo.FileStorage.Storage.Services.Object.Delete;
using Neo.FileStorage.Storage.Services.Object.Get;
using Neo.FileStorage.Storage.Services.Object.Put;
using Neo.FileStorage.Storage.Services.Object.Search;

namespace Neo.FileStorage.Storage.Services.Object
{
    public class ObjectService
    {
        public GetService GetService { get; init; }
        public PutService PutService { get; init; }
        public DeleteService DeleteService { get; init; }
        public SearchService SearchService { get; init; }

        public DeleteResponse Delete(DeleteRequest request, CancellationToken cancellation)
        {
            var resp = new DeleteResponse();
            var prm = DeleteService.ToDeletePrm(request, resp);
            DeleteService.Delete(prm, cancellation);
            return resp;
        }

        public void Get(GetRequest request, Action<GetResponse> handler, CancellationToken cancellation)
        {
            var prm = GetService.ToGetPrm(request, handler, cancellation);
            GetService.Get(prm, cancellation);
            //TODO: split exception
        }

        public void GetRange(GetRangeRequest request, Action<GetRangeResponse> handler, CancellationToken cancellation)
        {
            var prm = GetService.ToRangePrm(request, handler, cancellation);
            GetService.GetRange(prm, cancellation);
        }

        public GetRangeHashResponse GetRangeHash(GetRangeHashRequest request, CancellationToken cancellation)
        {
            var prm = GetService.ToRangeHashPrm(request);
            return GetService.GetRangeHash(prm, cancellation);
        }

        public HeadResponse Head(HeadRequest request, CancellationToken cancellation)
        {
            var resp = new HeadResponse();
            var prm = GetService.ToHeadPrm(request, resp, cancellation);
            GetService.Head(prm, cancellation);
            //TODO: split exception
            return resp;
        }

        public IRequestStream Put(CancellationToken cancellation)
        {
            return PutService.Put(cancellation);
        }

        public void Search(SearchRequest request, Action<SearchResponse> handler, CancellationToken cancellation)
        {
            var prm = SearchService.ToSearchPrm(request, handler, cancellation);
            SearchService.Search(prm, cancellation);
        }
    }
}
