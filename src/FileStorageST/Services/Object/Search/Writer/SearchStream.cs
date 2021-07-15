using Neo.FileStorage.API.Object;
using Neo.FileStorage.API.Refs;
using System;
using System.Collections.Generic;

namespace Neo.FileStorage.Storage.Services.Object.Search.Writer
{
    public class SearchStream : ISearchResponseWriter
    {
        public Action<SearchResponse> Handler { get; init; }

        public void WriteIDs(IEnumerable<ObjectID> ids)
        {
            Handler(SearchResponse(ids));
        }

        private SearchResponse SearchResponse(IEnumerable<ObjectID> ids)
        {
            SearchResponse.Types.Body body = new();
            body.IdList.AddRange(ids);
            SearchResponse resp = new();
            resp.Body = body;
            return resp;
        }
    }
}
