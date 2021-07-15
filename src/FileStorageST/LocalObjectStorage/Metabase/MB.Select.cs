using System;
using System.Collections.Generic;
using System.Linq;
using Google.Protobuf;
using Neo.Cryptography;
using Neo.FileStorage.API.Object;
using Neo.FileStorage.API.Refs;
using static Neo.FileStorage.API.Object.SearchRequest.Types.Body.Types;
using static Neo.FileStorage.Storage.LocalObjectStorage.Metabase.Helper;
using static Neo.Helper;
using static Neo.Utility;
using FSObject = Neo.FileStorage.API.Object.Object;


namespace Neo.FileStorage.Storage.LocalObjectStorage.Metabase
{
    public sealed partial class MB
    {
        public List<Address> Select(ContainerID cid, SearchFilters filters)
        {
            if (cid is null) throw new ArgumentNullException(nameof(cid));
            if (BlindyProcess(filters))
                return new();
            var container_id = GroupFilters(filters, out List<Filter> fast, out List<Filter> slow);
            if (container_id is not null && container_id.Value != cid.Value)
                return new();
            int exp_len = fast.Count;
            Dictionary<Address, int> mAddr = new();
            if (0 == exp_len)
            {
                exp_len = 1;
                SelectAll(cid, mAddr);
            }
            else
                for (int i = 0; i < fast.Count; i++)
                    SelectFast(cid, fast[i], mAddr, i);
            List<Address> result = new();
            foreach (var (a, index) in mAddr)
            {
                if (index != exp_len) continue;
                if (IsGraveYard(a)) continue;
                if (!MatchSlowFilters(a, slow)) continue;
                result.Add(a);
            }
            return result;
        }

        private void SelectAll(ContainerID cid, Dictionary<Address, int> to)
        {
            SelectAllFromBucket(cid, Concat(ObjectPrefix, cid.Value.ToByteArray()), to, 0);
            SelectAllFromBucket(cid, Concat(TombstonePrefix, cid.Value.ToByteArray()), to, 0);
            SelectAllFromBucket(cid, Concat(StorageGroupPrefix, cid.Value.ToByteArray()), to, 0);
            SelectAllFromBucket(cid, Concat(ParentPrefix, cid.Value.ToByteArray()), to, 0);
        }

        private void SelectAllFromBucket(ContainerID cid, byte[] prefix, Dictionary<Address, int> to, int fnum)
        {
            db.Iterate(prefix, (key, value) =>
            {
                Address address = new()
                {
                    ContainerId = cid,
                    ObjectId = new()
                    {
                        Value = ByteString.CopyFrom(key[prefix.Length..])
                    }
                };
                MarkAddressInCache(to, fnum, address);
                return false;
            });
        }

        private void MarkAddressInCache(Dictionary<Address, int> to, int fnum, Address key)
        {
            if (to.TryGetValue(key, out int value) && value == fnum)
            {
                to[key] = value + 1;
            }
            else if (fnum == 0)
            {
                to[key] = 1;
            }
        }

        private void SelectFast(ContainerID cid, Filter filter, Dictionary<Address, int> to, int fnum)
        {
            switch (filter.Key)
            {
                case Filter.FilterHeaderObjectID:
                    SelectObjectID(cid, filter, to, fnum);
                    break;
                case Filter.FilterHeaderOwnerID:
                    SelectFromFKBT(cid, Concat(OwnerPrefix, cid.Value.ToByteArray()), filter, to, fnum);
                    break;
                case Filter.FilterHeaderPayloadHash:
                    SelectFromList(cid, Concat(PayloadHashPrefix, cid.Value.ToByteArray()), filter, to, fnum);
                    break;
                case Filter.FilterHeaderObjectType:
                    foreach (var p in KeyPrefixForType(cid, filter.MatchType, filter.Value))
                        SelectAllFromBucket(cid, p, to, fnum);
                    break;
                case Filter.FilterHeaderParent:
                    SelectFromList(cid, Concat(ParentPrefix, cid.Value.ToByteArray()), filter, to, fnum);
                    break;
                case Filter.FilterHeaderSplitID:
                    SelectFromList(cid, Concat(SplitPrefix, cid.Value.ToByteArray()), filter, to, fnum);
                    break;
                case Filter.FilterPropertyRoot:
                    SelectAllFromBucket(cid, Concat(RootPrefix, cid.Value.ToByteArray()), to, fnum);
                    break;
                case Filter.FilterPropertyPhy:
                    SelectAllFromBucket(cid, Concat(ObjectPrefix, cid.Value.ToByteArray()), to, fnum);
                    SelectAllFromBucket(cid, Concat(TombstonePrefix, cid.Value.ToByteArray()), to, fnum);
                    SelectAllFromBucket(cid, Concat(StorageGroupPrefix, cid.Value.ToByteArray()), to, fnum);
                    break;
                default:
                    byte[] attrPrefix = Concat(AttributePrefix, cid.Value.ToByteArray(), StrictUTF8.GetBytes(filter.Key));
                    if (filter.MatchType == MatchType.NotPresent)
                        SelectOutsideFKBT(cid, AllPrefixes(cid), attrPrefix, filter, to, fnum);
                    else
                        SelectFromFKBT(cid, attrPrefix, filter, to, fnum);
                    break;
            }
        }

        private void SelectFromFKBT(ContainerID cid, byte[] keyPrefix, Filter filter, Dictionary<Address, int> to, int fnum)
        {
            if (matchers.TryGetValue(filter.MatchType, out var matcher))
            {
                db.Iterate(keyPrefix, (key, value) =>
                {
                    if (matcher(filter.Key, key[keyPrefix.Length..^ObjectID.ValueSize], filter.Value))
                    {
                        ObjectID oid = new()
                        {
                            Value = ByteString.CopyFrom(key[^ObjectID.ValueSize..])
                        };
                        Address address = new()
                        {
                            ContainerId = cid,
                            ObjectId = oid,
                        };
                        MarkAddressInCache(to, fnum, address);
                    }
                    return false;
                });
            }
        }

        private void SelectOutsideFKBT(ContainerID cid, List<byte[]> keyPrefixes, byte[] attrPrefix, Filter filter, Dictionary<Address, int> to, int fnum)
        {
            HashSet<Address> excludes = new();
            db.Iterate(attrPrefix, (key, _) =>
            {
                ParseAttributeKey(key, out Address address, out _);
                excludes.Add(address);
                return false;
            });
            foreach (var p in keyPrefixes)
            {
                db.Iterate(p, (key, _) =>
                {
                    Address address = ParseAddress(key);
                    if (!excludes.Contains(address))
                        MarkAddressInCache(to, fnum, address);
                    return false;
                });
            }
        }

        private void SelectObjectID(ContainerID cid, Filter filter, Dictionary<Address, int> to, int fnum)
        {
            void AppendObjectID(ObjectID oid)
            {
                Address address = new()
                {
                    ContainerId = cid,
                    ObjectId = oid,
                };
                try
                {
                    if (!Exists(address)) return;
                }
                catch (Exception e)
                {
                    if (e is not SplitInfoException)
                        return;
                }
                MarkAddressInCache(to, fnum, address);
            }
            switch (filter.MatchType)
            {
                case MatchType.StringEqual:
                    AppendObjectID(ObjectID.FromBase58String(filter.Value));
                    break;
                default:
                    if (matchers.TryGetValue(filter.MatchType, out Func<string, byte[], string, bool> matcher))
                    {
                        foreach (var p in KeyPrefixForType(cid, MatchType.StringNotEqual, ""))
                        {
                            db.Iterate(p, (key, value) =>
                            {
                                ObjectID oid = new()
                                {
                                    Value = ByteString.CopyFrom(key[^ObjectID.ValueSize..])
                                };
                                if (matcher(filter.Key, StrictUTF8.GetBytes(oid.ToBase58String()), filter.Value))
                                {
                                    AppendObjectID(oid);
                                }
                                return false;
                            });
                        }
                    }
                    break;
            }
        }

        private void SelectFromList(ContainerID cid, byte[] keyPrefix, Filter filter, Dictionary<Address, int> to, int fnum)
        {
            List<ObjectID> list = new();
            switch (filter.MatchType)
            {
                case MatchType.StringEqual:
                    var data = db.Get(Concat(keyPrefix, PrefixHelper(filter)));
                    if (data is null) return;
                    list = DecodeObjectIDList(data);
                    break;
                default:
                    if (matchers.TryGetValue(filter.MatchType, out Func<string, byte[], string, bool> matcher))
                    {
                        db.Iterate(keyPrefix, (key, value) =>
                        {
                            if (matcher(filter.Key, key[keyPrefix.Length..], filter.Value))
                            {
                                list.AddRange(DecodeObjectIDList(value));
                            }
                            return false;
                        });
                    }
                    break;
            }
            foreach (var oid in list)
            {
                Address address = new()
                {
                    ContainerId = cid,
                    ObjectId = oid
                };
                MarkAddressInCache(to, fnum, address);
            }
        }

        private List<byte[]> AllPrefixes(ContainerID cid)
        {
            List<byte[]> prefixes = new();
            prefixes.Add(Concat(ObjectPrefix, cid.Value.ToByteArray()));
            prefixes.Add(Concat(ParentPrefix, cid.Value.ToByteArray()));
            prefixes.Add(Concat(TombstonePrefix, cid.Value.ToByteArray()));
            prefixes.Add(Concat(StorageGroupPrefix, cid.Value.ToByteArray()));
            return prefixes;
        }

        private List<byte[]> KeyPrefixForType(ContainerID cid, MatchType op, string value)
        {
            List<byte[]> prefixes = new();
            List<byte[]> TypePrefix(ObjectType t)
            {
                switch (t)
                {
                    case ObjectType.Regular:
                        prefixes.Add(Concat(ObjectPrefix, cid.Value.ToByteArray()));
                        prefixes.Add(Concat(ParentPrefix, cid.Value.ToByteArray()));
                        break;
                    case ObjectType.Tombstone:
                        prefixes.Add(Concat(TombstonePrefix, cid.Value.ToByteArray()));
                        break;
                    case ObjectType.StorageGroup:
                        prefixes.Add(Concat(StorageGroupPrefix, cid.Value.ToByteArray()));
                        break;
                }
                return prefixes;
            };
            switch (op)
            {
                case MatchType.StringNotEqual:
                    foreach (var ot in Enum.GetValues(typeof(ObjectType)))
                    {
                        if (ot.ToString() != value)
                        {
                            TypePrefix((ObjectType)ot);
                        }
                    }
                    break;
                case MatchType.StringEqual:
                    TypePrefix((ObjectType)Enum.Parse(typeof(ObjectType), value));
                    break;
                default:
                    break;
            }
            return prefixes;
        }

        private bool BlindyProcess(SearchFilters filters)
        {
            foreach (var f in filters.Filters)
            {
                if (f.MatchType == MatchType.NotPresent && f.Key.StartsWith(Filter.ReservedFilterPrefix))
                    return true;
            }
            return false;
        }

        private ContainerID GroupFilters(SearchFilters filters, out List<Filter> fast, out List<Filter> slow)
        {
            ContainerID cid = null;
            fast = new();
            slow = new();
            foreach (var filter in filters.Filters)
            {
                switch (filter.Key)
                {
                    case Filter.FilterHeaderContainerID:
                        cid = ContainerID.FromBase58String(filter.Value);
                        break;
                    case Filter.FilterHeaderVersion:
                    case Filter.FilterHeaderCreationEpoch:
                    case Filter.FilterHeaderPayloadLength:
                    case Filter.FilterHeaderHomomorphicHash:
                        slow.Add(filter);
                        break;
                    default:
                        fast.Add(filter);
                        break;
                }
            }
            return cid;
        }

        private bool MatchSlowFilters(Address address, List<Filter> slow)
        {
            if (!slow.Any()) return true;
            FSObject obj;
            try
            {
                obj = Get(address, true, false);
            }
            catch (Exception)
            {
                return false;
            }
            foreach (var f in slow)
            {
                var matchFunc = matchers[f.MatchType];
                byte[] data;
                switch (f.Key)
                {
                    case Filter.FilterHeaderVersion:
                        data = StrictUTF8.GetBytes(obj.Version.String());
                        break;
                    case Filter.FilterHeaderHomomorphicHash:
                        data = obj.PayloadHomomorphicHash.Sum.ToByteArray();
                        break;
                    case Filter.FilterHeaderCreationEpoch:
                        data = BitConverter.GetBytes(obj.CreationEpoch);
                        break;
                    case Filter.FilterHeaderPayloadLength:
                        data = BitConverter.GetBytes(obj.PayloadSize);
                        break;
                    default:
                        continue;
                }
                if (!matchFunc(f.Key, data, f.Value)) return false;
            }
            return true;
        }

        private byte[] PrefixHelper(Filter filter)
        {
            switch (filter.Key)
            {
                case Filter.FilterHeaderPayloadHash:
                    return filter.Value.HexToBytes();
                case Filter.FilterHeaderSplitID:
                    var sid = new SplitID();
                    sid.Parse(filter.Value);
                    return sid.ToByteArray();
                case Filter.FilterHeaderParent:
                    return Base58.Decode(filter.Value);
                default:
                    return StrictUTF8.GetBytes(filter.Value);
            }
        }
    }
}
