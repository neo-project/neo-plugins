using System.Threading;
using Neo.FileStorage.Storage.Services.Object.Util;
using Neo.FileStorage.Storage.Services.Reputaion.Local.Client;

namespace Neo.FileStorage.Storage.Services.Object.Put
{
    public class RemoteSender
    {
        public KeyStorage KeyStorage { get; init; }
        public ReputationClientCache ClientCache { get; init; }

        public void PutObject(RemotePutPrm prm, CancellationToken context)
        {
            var t = new RemoteTarget
            {
                Cancellation = context,
                KeyStorage = KeyStorage,
                Address = prm.Node,
                ClientCache = ClientCache,
            };
            t.WriteHeader(prm.Object);
            t.Close();
        }
    }
}
