using Neo.FileStorage.Morph.Invoker;

namespace Neo.FileStorage.Storage.Services.Object.Search
{
    public class EpochSource : IEpochSource
    {
        private readonly MorphInvoker morphInvoker;

        public EpochSource(MorphInvoker invoker)
        {
            morphInvoker = invoker;
        }

        ulong IEpochSource.CurrentEpoch()
        {
            return morphInvoker.Epoch();
        }
    }
}
