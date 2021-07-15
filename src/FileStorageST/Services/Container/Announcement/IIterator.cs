using System;
using FSAnnouncement = Neo.FileStorage.API.Container.AnnounceUsedSpaceRequest.Types.Body.Types.Announcement;

namespace Neo.FileStorage.Storage.Services.Container.Announcement.Route
{
    public interface IIterator
    {
        void Iterate(Func<FSAnnouncement, bool> filter, Action<FSAnnouncement> handler);
    }
}
