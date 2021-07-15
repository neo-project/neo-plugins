using Neo.FileStorage.Storage.Cache;
using Neo.FileStorage.Storage.Services.Reputaion.Common;
using Neo.FileStorage.Storage.Services.Reputaion.Local.Storage;

namespace Neo.FileStorage.Storage.Services.Reputaion.Local
{
    public class LocalTrustStorage : IIteratorProvider
    {
        public TrustStorage TrustStorage { get; init; }
        public NetmapCache NetmapCache { get; init; }
        public byte[] LocalKey { get; init; }

        public IIterator InitIterator(ICommonContext context)
        {
            TrustStorage.DataForEpoch(context.Epoch, out EpochTrustStorage storage);
            return new TrustIterator();
        }
    }
}
