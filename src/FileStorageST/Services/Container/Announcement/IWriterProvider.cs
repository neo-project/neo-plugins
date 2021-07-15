using System.Threading;

namespace Neo.FileStorage.Storage.Services.Container.Announcement.Route
{
    public interface IWriterProvider
    {
        IWriter InitWriter(CancellationToken cancellation);
    }
}
