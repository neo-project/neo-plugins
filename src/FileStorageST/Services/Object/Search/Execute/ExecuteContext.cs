using System.Threading;
using Neo.FileStorage.Placement;

namespace Neo.FileStorage.Storage.Services.Object.Search.Execute
{
    public partial class ExecuteContext
    {
        public CancellationToken Cancellation { get; init; }
        public SearchPrm Prm { get; init; }
        public SearchService SearchService { get; init; }

        private ulong currentEpoch;
        private Traverser traverser;

        public void Execute()
        {
            ExecuteLocal();
            if (!Prm.Local)
                ExecuteContainer();
        }
    }
}
