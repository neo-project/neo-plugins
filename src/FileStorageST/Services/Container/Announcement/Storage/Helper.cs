using System.Collections.Generic;
using System.Linq;

namespace Neo.FileStorage.Storage.Services.Container.Announcement.Storage
{
    public static class Helper
    {
        public static ulong FinalEstimation(List<ulong> sizes)
        {
            sizes.Sort();
            int lowerRank = 10, upperRank = 90;
            if (lowerRank <= sizes.Count)
            {
                int lowerIn = Percentile(lowerRank, sizes);
                int upperIn = Percentile(upperRank, sizes);
                sizes = sizes.Skip(lowerIn).Take(upperIn - lowerIn).ToList();
            }
            return sizes.Aggregate(0ul, (sum, p) => sum += p) / (ulong)sizes.Count;
        }

        public static int Percentile(int rank, List<ulong> sizes)
        {
            return sizes.Count * rank / 100;
        }
    }
}
