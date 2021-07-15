using Neo.FileStorage.InnerRing.Services.Audit;

namespace Neo.FileStorage.InnerRing.Processors
{
    public class EpochAuditReporter : IReporter
    {
        public ulong epoch;

        public IReporter reporter;

        public void WriteReport(Report report)
        {
            report.Result().AuditEpoch = epoch;
            reporter.WriteReport(report);
        }
    }
}
