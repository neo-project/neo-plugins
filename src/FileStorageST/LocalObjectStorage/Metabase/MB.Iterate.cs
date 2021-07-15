using System;
using System.Collections.Generic;
using System.Linq;
using Neo.FileStorage.API.Object;
using Neo.FileStorage.API.Refs;
using static Neo.Utility;

namespace Neo.FileStorage.Storage.LocalObjectStorage.Metabase
{
    public sealed partial class MB
    {
        public void IterateGraveYard(Func<Grave, bool> handler)
        {
            db.Iterate(GraveYardPrefix, (key, value) =>
            {
                return handler(new()
                {
                    GCMark = value.SequenceEqual(InhumeGCMarkValue),
                    Address = ParseGraveYardKey(key)
                });
            });
        }

        public void IterateExpired(ulong epoch, Action<ObjectType, Address> handler)
        {
            byte[] expired_epoch_key = StrictUTF8.GetBytes(Header.Types.Attribute.SysAttributeExpEpoch);
            db.Iterate(AttributePrefix, (key, value) =>
            {
                key = key[1..];
                byte[] cid = key[..32];
                key = key[32..];
                if (!key.AsSpan().StartsWith(expired_epoch_key)) return false;
                key = key[expired_epoch_key.Length..];
                ulong expired_epoch_value = ulong.Parse(StrictUTF8.GetString(key[..^32]));
                if (epoch <= expired_epoch_value) return false;
                byte[] oid = key[^32..];
                Address address = new()
                {
                    ContainerId = ContainerID.FromSha256Bytes(cid),
                    ObjectId = ObjectID.FromSha256Bytes(oid),
                };
                handler(GetObjectType(address), address);
                return false;
            });
        }

        private ObjectType GetObjectType(Address address)
        {
            if (InBucket(TombstoneKey(address))) return ObjectType.Tombstone;
            if (InBucket(StorageGroupKey(address))) return ObjectType.StorageGroup;
            return ObjectType.Regular;
        }

        public void IterateCoveredByTombstones(HashSet<Address> tss, Action<Address> func)
        {
            db.Iterate(GraveYardPrefix, (key, value) =>
            {
                if (value.SequenceEqual(InhumeGCMarkValue)) return false;
                if (tss.Contains(ParseGraveYardKey(value)))
                {
                    func(ParseGraveYardKey(key));
                }
                return false;
            });
        }
    }
}
