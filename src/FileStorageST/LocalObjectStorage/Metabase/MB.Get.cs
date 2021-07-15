using System.Collections.Generic;
using System.Linq;
using Neo.FileStorage.API.Object;
using Neo.FileStorage.API.Refs;
using static Neo.FileStorage.Storage.LocalObjectStorage.Metabase.Helper;
using static Neo.Helper;
using FSObject = Neo.FileStorage.API.Object.Object;

namespace Neo.FileStorage.Storage.LocalObjectStorage.Metabase
{
    public sealed partial class MB
    {
        private bool IsGraveYard(Address address)
        {
            return db.Get(GraveYardKey(address)) is not null;
        }

        public FSObject Get(Address address, bool raw = false)
        {
            return Get(address, true, raw);
        }

        private FSObject Get(Address address, bool check_grave_yard, bool raw)
        {
            if (check_grave_yard && IsGraveYard(address))
                throw new ObjectAlreadyRemovedException();
            FSObject obj = GetObject(Primarykey(address));
            if (obj is not null) return obj;
            obj = GetObject(TombstoneKey(address));
            if (obj is not null) return obj;
            obj = GetObject(StorageGroupKey(address));
            if (obj is not null) return obj;
            return GetVirtualObject(address, raw);
        }

        private FSObject GetObject(byte[] key)
        {
            byte[] data = db.Get(key);
            if (data is null) return null;
            return FSObject.Parser.ParseFrom(data);
        }

        private FSObject GetVirtualObject(Address address, bool raw)
        {
            if (raw)
                throw new SplitInfoException(GetSplitInfo(address));
            var data = db.Get(ParentKey(address.ContainerId, address.ObjectId));
            if (data is null) throw new ObjectNotFoundException();
            var children = DecodeObjectIDList(data);
            if (!children.Any()) throw new ObjectNotFoundException();
            var child = children[^1];
            var obj = GetObject(Primarykey(new()
            {
                ContainerId = address.ContainerId,
                ObjectId = child,
            }));
            if (obj.Parent is null)
                throw new ObjectNotFoundException();
            return obj.Parent;
        }

        private SplitInfo GetSplitInfo(Address address)
        {
            byte[] data = db.Get(RootKey(address));
            if (data is null) throw new ObjectNotFoundException();
            return SplitInfo.Parser.ParseFrom(data);
        }

        public bool Exists(Address address)
        {
            if (IsGraveYard(address))
                throw new ObjectAlreadyRemovedException();
            if (InBucket(Primarykey(address))) return true;
            List<byte[]> keys = new();
            db.Iterate(Concat(ParentPrefix, address.ContainerId.Value.ToByteArray(), address.ObjectId.Value.ToByteArray()),
                (key, _) =>
                {
                    keys.Add(key);
                    return false;
                });
            if (keys.Any())
                throw new SplitInfoException(GetSplitInfo(address));
            if (InBucket(TombstoneKey(address))) return true;
            return InBucket(StorageGroupKey(address));
        }

        private bool InBucket(byte[] key)
        {
            return db.Get(key) is not null;
        }
    }
}
