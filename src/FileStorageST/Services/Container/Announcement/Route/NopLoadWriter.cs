using FSAnnouncement = Neo.FileStorage.API.Container.AnnounceUsedSpaceRequest.Types.Body.Types.Announcement;

namespace Neo.FileStorage.Storage.Services.Container.Announcement.Route
{
    public class NopLoadWriter : IWriter
    {
        public void Put(FSAnnouncement announcement)
        {
            return;
        }

        public void Close()
        {
            return;
        }
    }
}
