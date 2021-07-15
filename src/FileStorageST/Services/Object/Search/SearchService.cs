using System;
using System.Threading;
using Neo.FileStorage.API.Cryptography;
using Neo.FileStorage.API.Object;
using Neo.FileStorage.API.Session;
using Neo.FileStorage.Cache;
using Neo.FileStorage.Morph.Invoker;
using Neo.FileStorage.Storage.Services.Object.Search.Execute;
using Neo.FileStorage.Storage.Services.Object.Search.Remote;
using Neo.FileStorage.Storage.Services.Object.Search.Writer;
using Neo.FileStorage.Storage.Services.Object.Util;

namespace Neo.FileStorage.Storage.Services.Object.Search
{
    public class SearchService
    {
        public KeyStorage KeyStorage { get; init; }
        public ILocalSearchSource LocalStorage { get; init; }
        public IEpochSource MorphClient { get; init; }
        public ISearchClientCache ClientCache { get; init; }
        public ITraverserGenerator TraverserGenerator { get; init; }

        public SearchPrm ToSearchPrm(SearchRequest request, Action<SearchResponse> handler, CancellationToken cancellation)
        {
            var key = KeyStorage.GetKey(request.MetaHeader.SessionToken);
            var prm = SearchPrm.FromRequest(request);
            prm.Key = key;
            prm.Writer = new SearchStream
            {
                Handler = handler,
            };
            if (!prm.Local)
            {
                RequestMetaHeader meta = new();
                meta.Ttl = request.MetaHeader.Ttl - 1;
                meta.Origin = request.MetaHeader;
                request.MetaHeader = meta;
                key.SignRequest(request);
                prm.Forwarder = client =>
                {
                    return client.SearchObject(request, context: cancellation).Result;
                };
            }
            return prm;
        }

        public void Search(SearchPrm prm, CancellationToken cancellation)
        {
            ExecuteContext executor = new()
            {
                Cancellation = cancellation,
                Prm = prm,
                SearchService = this,
            };
            executor.Execute();
        }
    }
}
