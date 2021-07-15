
using System;
using System.Security.Cryptography;
using Neo.FileStorage.API.Netmap;
using Neo.FileStorage.Cache;
using Neo.FileStorage.Network;

namespace Neo.FileStorage.Storage.Services.Container.Announcement.Route
{
    public class RemoteLoadAnnounceProvider
    {
        public ECDsa Key { get; init; }
        public Address LocalAddress { get; init; }
        public ClientCache ClientCache { get; init; }
        public IWriterProvider DeadEndProvider { get; init; }

        public IWriterProvider InitRemote(NodeInfo info)
        {
            if (info is null)
                throw new ArgumentNullException(nameof(info));
            if (LocalAddress.ToString() == info.Address)
                return new SimpleProvider(new NopLoadWriter());
            var addr = Network.Address.FromString(info.Address);
            var client = ClientCache.Get(addr);
            return new RemoteLoadAnnounceWriterProvider
            {
                Key = Key,
                Client = client,
            };
        }
    }
}
