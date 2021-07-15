using System.Threading;

namespace Neo.FileStorage.Storage.Services.Container.Announcement.Route
{
    public interface IIteratorProvider
    {
        IIterator InitIterator(CancellationToken cancellation);
    }
}
