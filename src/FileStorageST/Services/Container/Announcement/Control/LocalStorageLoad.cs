using Neo.FileStorage.Storage.LocalObjectStorage.Engine;
using Neo.FileStorage.Storage.Services.Container.Announcement.Route;
using System;
using FSAnnouncement = Neo.FileStorage.API.Container.AnnounceUsedSpaceRequest.Types.Body.Types.Announcement;

namespace Neo.FileStorage.Storage.Services.Container.Announcement.Control
{
    public class LocalStorageLoad : IIterator
    {
        public StorageEngine LocalStorage { get; init; }

        public void Iterate(Func<FSAnnouncement, bool> filter, Action<FSAnnouncement> handler)
        {
            var containers = LocalStorage.ListContainers();
            foreach (var cid in containers)
            {
                var size = LocalStorage.ContainerSize(cid);
                var a = new FSAnnouncement
                {
                    ContainerId = cid,
                    UsedSpace = size,
                };
                if (filter is not null && !filter(a)) continue;
                handler(a);
            }
        }
    }
}
