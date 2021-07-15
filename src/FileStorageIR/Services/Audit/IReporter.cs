
namespace Neo.FileStorage.InnerRing.Services.Audit
{
    public interface IReporter
    {
        void WriteReport(Report r);
    }
}
