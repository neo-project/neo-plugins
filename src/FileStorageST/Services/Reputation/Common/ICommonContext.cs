using System.Threading;

namespace Neo.FileStorage.Storage.Services.Reputaion.Common
{
    public interface ICommonContext
    {
        CancellationToken Cancellation { get; }
        ulong Epoch { get; }
    }
}
