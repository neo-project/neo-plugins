using System.Collections.Generic;
using System.Linq;
using Neo.FileStorage.API.Netmap;

namespace Neo.FileStorage.Storage.Services.Reputaion.Common.Route
{
    public static class Helper
    {
        public static bool CheckRoute(this IBuilder builder, ulong epoch, Trust trust, List<NodeInfo> route)
        {
            for (int i = 0; i < route.Count; i++)
            {
                List<NodeInfo> servers;
                try
                {
                    servers = builder.NextStage(epoch, trust, route.Take(i + 1).ToList());
                }
                catch
                {
                    return false;
                }
                if (!servers.Any()) return true;
                bool found = false;
                for (int j = 0; j < servers.Count; j++)
                {
                    if (servers[j].PublicKey.Equals(route[j].PublicKey))
                    {
                        found = true;
                        break;
                    }
                }
                if (!found) return false;
            }
            return true;
        }
    }
}
