using System.Threading;
using Neo.FileStorage.API.Object;
using Neo.FileStorage.Core.Object;
using Neo.FileStorage.Morph.Invoker;
using Neo.FileStorage.Network;
using Neo.FileStorage.Storage.LocalObjectStorage.Engine;
using Neo.FileStorage.Storage.Services.Object.Put.Writer;
using Neo.FileStorage.Storage.Services.Object.Util;
using Neo.FileStorage.Storage.Services.Reputaion.Local.Client;

namespace Neo.FileStorage.Storage.Services.Object.Put
{
    public class PutService
    {
        public MorphInvoker MorphInvoker { get; init; }
        public Address LocalAddress { get; init; }
        public KeyStorage KeyStorage { get; init; }
        public StorageEngine LocalStorage { get; init; }
        public LocalObjectInhumer ObjectInhumer { get; init; }
        public ReputationClientCache ClientCache { get; init; }

        public PutStream Put(CancellationToken cancellation)
        {
            return new PutStream()
            {
                Cancellation = cancellation,
                PutService = this,
            };
        }
    }
}
