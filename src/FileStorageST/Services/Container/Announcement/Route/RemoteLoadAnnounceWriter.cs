
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Threading;
using Neo.FileStorage.API.Client;
using FSAnnouncement = Neo.FileStorage.API.Container.AnnounceUsedSpaceRequest.Types.Body.Types.Announcement;

namespace Neo.FileStorage.Storage.Services.Container.Announcement.Route
{
    public class RemoteLoadAnnounceWriter : IWriter
    {
        public CancellationToken Cancellation { get; init; }
        public IFSClient Client { get; init; }
        public ECDsa Key { get; init; }
        private readonly List<FSAnnouncement> buffer = new();

        public void Put(FSAnnouncement announcement)
        {
            buffer.Add(announcement);
        }

        public void Close()
        {
            Client.AnnounceContainerUsedSpace(buffer, context: Cancellation);
        }
    }
}
