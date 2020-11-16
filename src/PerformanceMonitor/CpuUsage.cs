using System.Collections.Generic;

namespace Neo.Plugins
{
    class CpuUsage
    {
        public double TotalUsage;
        public Dictionary<int, double> ThreadsUsage;

        public CpuUsage(double totalUsage, Dictionary<int, double> threadsUsage)
        {
            TotalUsage = totalUsage;
            ThreadsUsage = threadsUsage;
        }
    }
}
