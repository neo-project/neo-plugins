using System.Security.Cryptography;
using Neo.FileStorage.API.Netmap;
using Neo.FileStorage.Network;
using Neo.FileStorage.Network.Cache;

namespace Neo.FileStorage.Services.Reputaion.Common
{
    public class RemoteTrustProvider : IRemoteWriterProvider
    {
        public ECDsa Key { get; init; }
        public Address LocalAddress { get; init; }
        public IWriterProvider DeadEndProvider { get; init; }
        public ClientCache ClientCache { get; init; }
        public IClientKeyRemoteProvider RemoteProvider { get; init; }

        public IWriterProvider InitRemote(NodeInfo ni)
        {
            if (ni is null) return DeadEndProvider;
            if (LocalAddress.ToString() == ni.Address)
                return new SimpleWriterProvider(new NonWriter());
            var addr = Address.FromString(ni.Address);
            var client = ClientCache.Get(addr);
            return RemoteProvider.WithClient(client);
        }
    }
}
