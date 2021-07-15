using Neo.FileStorage.API.Netmap;

namespace Neo.FileStorage.Storage.Services.Reputaion.Common
{
    public interface IRemoteWriterProvider
    {
        IWriterProvider InitRemote(NodeInfo ni);
    }
}
