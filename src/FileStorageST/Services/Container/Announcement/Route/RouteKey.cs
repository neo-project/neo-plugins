
using System;

namespace Neo.FileStorage.Storage.Services.Container.Announcement.Route
{
    public class RouteKey : IEquatable<RouteKey>
    {
        public ulong Epoch;
        public string Cid;

        bool IEquatable<RouteKey>.Equals(RouteKey other)
        {
            if (other is null) return false;
            return other.Epoch == Epoch && other.Cid == Cid;
        }
    }
}
