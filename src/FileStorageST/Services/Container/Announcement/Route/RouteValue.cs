using Neo.FileStorage.API.Netmap;
using System.Collections.Generic;
using FSAnnouncement = Neo.FileStorage.API.Container.AnnounceUsedSpaceRequest.Types.Body.Types.Announcement;

namespace Neo.FileStorage.Storage.Services.Container.Announcement.Route
{
    public class RouteValue
    {
        public List<NodeInfo> Route;
        public List<FSAnnouncement> Values;
    }
}
