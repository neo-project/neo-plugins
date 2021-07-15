using System.Threading;
using Neo.FileStorage.Storage.Services.Reputaion.Common;

namespace Neo.FileStorage.Storage.Services.Reputaion.Local.Control
{
    public class IterationContext : ICommonContext
    {
        public CancellationToken Cancellation { get; init; }
        public ulong Epoch { get; init; }
    }
}
