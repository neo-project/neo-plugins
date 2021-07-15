using Neo.FileStorage.API.Client;

namespace Neo.FileStorage.Storage.Services.Reputaion.Common
{
    public interface IClientKeyRemoteProvider
    {
        IWriterProvider WithClient(IFSClient client);
    }
}
