using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Google.Protobuf;
using Neo.FileStorage.API.Object;
using Neo.FileStorage.Morph.Invoker;
using Neo.FileStorage.Storage.LocalObjectStorage.Engine;
using Neo.FileStorage.Storage.Services.Object.Get.Execute;
using Neo.FileStorage.Storage.Services.Object.Get.Writer;
using Neo.FileStorage.Storage.Services.Object.Util;
using Neo.FileStorage.Storage.Services.Reputaion.Local.Client;
using FSRange = Neo.FileStorage.API.Object.Range;


namespace Neo.FileStorage.Storage.Services.Object.Get
{
    public partial class GetService
    {
        //Default value should be true
        public bool Assemble { get; init; }
        public KeyStorage KeyStorage { get; init; }
        public StorageEngine LocalStorage { get; init; }
        public ReputationClientCache ClientCache { get; init; }
        public MorphInvoker MorphInvoker { get; init; }
        public TraverserGenerator TraverserGenerator { get; init; }

        public void Get(GetPrm prm, CancellationToken cancellation)
        {
            Get(prm, null, false, cancellation);
        }


        public void Head(HeadPrm prm, CancellationToken cancellation)
        {
            Get(prm, null, true, cancellation);
        }

        public void GetRange(RangePrm prm, CancellationToken cancellation)
        {
            Get(prm, prm.Range, false, cancellation);
        }

        public GetRangeHashResponse GetRangeHash(RangeHashPrm prm, CancellationToken cancellation)
        {
            List<byte[]> hashes = new();
            foreach (var range in prm.Ranges)
            {
                if (cancellation.IsCancellationRequested) throw new OperationCanceledException();
                var writer = new RangeHashGenerator(prm.HashType);
                var range_prm = new RangePrm
                {
                    Range = range,
                    Writer = writer,
                };
                range_prm.WithGetCommonPrm(prm);
                Get(range_prm, range, false, cancellation);
                hashes.Add(writer.GetHash());
            }
            GetRangeHashResponse resp = new();
            resp.Body = new();
            resp.Body.HashList.AddRange(hashes.Select(p => ByteString.CopyFrom(p)));
            return resp;
        }

        internal void Get(GetCommonPrm prm, FSRange range, bool head_only, CancellationToken cancellation)
        {
            RangePrm range_prm = new();
            range_prm.WithGetCommonPrm(prm);
            range_prm.Range = range;
            var executor = new ExecuteContext
            {
                Cancellation = cancellation,
                Prm = range_prm,
                Range = range,
                HeadOnly = head_only,
                GetService = this,
            };
            executor.Execute();
        }
    }
}
