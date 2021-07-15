using System.Collections.Generic;
using System.Linq;
using Neo.FileStorage.API.Netmap;
using FSAnnouncement = Neo.FileStorage.API.Container.AnnounceUsedSpaceRequest.Types.Body.Types.Announcement;

namespace Neo.FileStorage.Storage.Services.Container.Announcement.Route.Placement
{
    public class RouteBuilder
    {
        public LoadPlacementBuilder PlacementBuilder { get; init; }

        public List<NodeInfo> NextStage(FSAnnouncement announcement, List<NodeInfo> passed)
        {
            if (1 < passed.Count) return null;
            var placement = PlacementBuilder.BuildPlacement(announcement.Epoch, announcement.ContainerId);
            return placement.Select(p => p.Any() ? p.First() : null).Where(p => p is not null).Select(p => p.Info).ToList();
        }

        /// <summary>
        /// CheckRoute checks if the route is a route correctly constructed by the builder for value a.
        /// Returns nil if route is correct, otherwise an error clarifying the inconsistency.
        /// </summary>
        public bool CheckRoute(FSAnnouncement announcement, List<NodeInfo> route)
        {
            for (int i = 0; i < route.Count; i++)
            {
                List<NodeInfo> servers;
                try
                {
                    servers = NextStage(announcement, route.Take(i).ToList());
                }
                catch
                {
                    return false;
                }
                if (servers is null || !servers.Any()) break;
                bool found = false;
                foreach (var server in servers)
                {
                    if (server.PublicKey.SequenceEqual(route[i].PublicKey))
                    {
                        found = true;
                        break;
                    }
                }
                if (!found)
                    return false;
            }
            return true;
        }
    }
}
