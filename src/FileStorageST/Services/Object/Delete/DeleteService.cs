using System;
using System.Threading;
using Neo.FileStorage.API.Object;
using Neo.FileStorage.Morph.Invoker;
using Neo.FileStorage.Storage.Services.Object.Delete.Execute;
using Neo.FileStorage.Storage.Services.Object.Delete.Writer;
using Neo.FileStorage.Storage.Services.Object.Get;
using Neo.FileStorage.Storage.Services.Object.Put;
using Neo.FileStorage.Storage.Services.Object.Search;
using Neo.FileStorage.Storage.Services.Object.Util;

namespace Neo.FileStorage.Storage.Services.Object.Delete
{
    public class DeleteService
    {
        public MorphInvoker MorphInvoker { get; init; }
        public PutService PutService { get; init; }
        public SearchService SearchService { get; init; }
        public GetService GetService { get; init; }
        public KeyStorage KeyStorage { get; init; }

        public DeletePrm ToDeletePrm(DeleteRequest request, DeleteResponse response)
        {
            var meta = request.MetaHeader;
            var key = KeyStorage.GetKey(meta?.SessionToken);
            if (key is null) throw new InvalidOperationException(nameof(ToDeletePrm) + " could not get key");
            var prm = DeletePrm.FromRequest(request);
            prm.Key = key;
            prm.Writer = new SimpleTombstoneWriter
            {
                Response = response,
            };
            return prm;
        }

        public void Delete(DeletePrm prm, CancellationToken cancellation)
        {
            var executor = new ExecuteContext
            {
                Cancellation = cancellation,
                DeleteService = this,
                Prm = prm,
            };
            executor.Execute();
        }
    }
}
