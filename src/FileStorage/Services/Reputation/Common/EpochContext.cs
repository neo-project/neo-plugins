using System.Threading;

namespace Neo.FileStorage.Services.Reputaion.Common
{
    public class EpochContext : ICommonContext
    {
        public CancellationToken Cancellation { get; init; }
        public ulong Epoch { get; init; }
    }
}