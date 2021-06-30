using System.Security.Cryptography;
using Neo.FileStorage.API.Client;
using Neo.FileStorage.Services.Reputaion.Common;

namespace Neo.FileStorage.Services.Reputaion.Local.Remote
{
    public class RemoteProvider : IClientKeyRemoteProvider
    {
        public ECDsa Key { get; init; }

        public IWriterProvider WithClient(IFSClient client)
        {
            return new TrustWriterProvider
            {
                Key = Key,
                Client = client
            };
        }
    }
}
