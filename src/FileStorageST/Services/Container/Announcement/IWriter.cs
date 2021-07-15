using FSAnnouncement = Neo.FileStorage.API.Container.AnnounceUsedSpaceRequest.Types.Body.Types.Announcement;

namespace Neo.FileStorage.Storage.Services.Container.Announcement.Route
{
    public interface IWriter
    {
        void Put(FSAnnouncement announcement);
        void Close();
    }
}
