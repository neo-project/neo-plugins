using Neo.FileStorage.API.Object;

namespace Neo.FileStorage.Storage.LocalObjectStorage
{
    public static class Helper
    {
        public static SplitInfo MergeSplitInfo(SplitInfo from, SplitInfo to)
        {
            to.SplitId = from.SplitId;
            if (from.LastPart is not null) to.LastPart = from.LastPart;
            if (from.Link is not null) to.Link = from.Link;
            return to;
        }
    }
}
