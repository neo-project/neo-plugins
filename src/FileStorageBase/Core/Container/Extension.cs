using System;
using Neo.FileStorage.API.Refs;
using V2Container = Neo.FileStorage.API.Container;

namespace Neo.FileStorage.Core.Container
{
    public static class Extension
    {
        public static bool CheckFormat(this V2Container.Container container)
        {
            if (container.PlacementPolicy is null) return false;
            if (!API.Refs.Version.IsSupportedVersion(container.Version)) return false;
            if (container.OwnerId?.Value?.Length != OwnerID.ValueSize) return false;
            try
            {
                var guid = container.NonceUUID;
            }
            catch (ArgumentException)
            {
                return false;
            }
            return true;
        }
    }
}
