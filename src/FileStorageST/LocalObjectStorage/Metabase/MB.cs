using System;
using System.Collections.Generic;
using Google.Protobuf;
using Neo.FileStorage.API.Object;
using Neo.FileStorage.API.Refs;
using Neo.FileStorage.Database;
using Neo.FileStorage.Database.LevelDB;
using static Neo.FileStorage.API.Object.SearchRequest.Types.Body.Types;
using static Neo.Helper;
using static Neo.Utility;

namespace Neo.FileStorage.Storage.LocalObjectStorage.Metabase
{
    public sealed partial class MB : IDisposable
    {
        private readonly byte[] ContainerPrefix = new byte[] { 0x00 };
        private readonly byte[] ObjectPrefix = new byte[] { 0x01 };
        private readonly byte[] GraveYardPrefix = new byte[] { 0x02 };
        private readonly byte[] TombstonePrefix = new byte[] { 0x03 };
        private readonly byte[] StorageGroupPrefix = new byte[] { 0x04 };
        private readonly byte[] RootPrefix = new byte[] { 0x05 };
        private readonly byte[] ParentPrefix = new byte[] { 0x06 };
        private readonly byte[] SmallPrefix = new byte[] { 0x07 };
        private readonly byte[] ToMoveItPrefix = new byte[] { 0x08 };
        private readonly byte[] PayloadHashPrefix = new byte[] { 0x09 };
        private readonly byte[] SplitPrefix = new byte[] { 0x0a };
        private readonly byte[] OwnerPrefix = new byte[] { 0x0b };
        private readonly byte[] AttributePrefix = new byte[] { 0x0c };
        private readonly byte[] ZeroValue = new byte[] { 0xFF };
        private readonly string path;
        private IDB db;
        private readonly Dictionary<MatchType, Func<string, byte[], string, bool>> matchers;

        public MB(string p)
        {
            path = p;
            matchers = new()
            {
                { MatchType.Unspecified, UnknownMatcher },
                { MatchType.StringEqual, StringEqualMatcher },
                { MatchType.StringNotEqual, StringNotEqualMatcher }
            };
        }

        public void Open()
        {
            db = new DB(path);
        }

        public void Dispose()
        {
            db?.Dispose();
        }

        private string StringifyValue(string key, byte[] value)
        {
            return key switch
            {
                Filter.FilterHeaderPayloadHash or Filter.FilterHeaderHomomorphicHash => value.ToHexString(),
                Filter.FilterHeaderCreationEpoch or Filter.FilterHeaderPayloadLength => BitConverter.ToUInt64(value).ToString(),
                _ => StrictUTF8.GetString(value),
            };
        }

        private bool UnknownMatcher(string key, byte[] value, string filter)
        {
            return false;
        }

        private bool StringEqualMatcher(string key, byte[] value, string filter)
        {
            return StringifyValue(key, value) == filter;
        }

        private bool StringNotEqualMatcher(string key, byte[] value, string filter)
        {
            return StringifyValue(key, value) != filter;
        }

        private byte[] BucketKeyHelper(string header, string value)
        {
            switch (header)
            {
                case Filter.FilterHeaderPayloadHash:
                    return value.HexToBytes();
                case Filter.FilterHeaderSplitID:
                    SplitID s = new();
                    if (s.Parse(value)) return s.ToByteArray();
                    return null;
                default:
                    return StrictUTF8.GetBytes(value);
            }
        }

        private Address ParseAddress(byte[] key)
        {
            if (key.Length < 1 + ContainerID.ValueSize + ObjectID.ValueSize) throw new ArgumentException("invalid format", nameof(key));
            byte[] cidv = key[1..(1 + ContainerID.ValueSize)];
            byte[] oidv = key[^ObjectID.ValueSize..];
            return new()
            {
                ContainerId = new()
                {
                    Value = ByteString.CopyFrom(cidv)
                },
                ObjectId = new()
                {
                    Value = ByteString.CopyFrom(oidv)
                },
            };
        }

        private byte[] ContainerSizeKey(ContainerID cid)
        {
            if (cid is null) throw new ArgumentNullException(nameof(cid));
            return Concat(ContainerPrefix, cid.Value.ToByteArray());
        }

        private ContainerID ParseContainerSizeKey(byte[] key)
        {
            if (key.Length != 1 + ContainerID.ValueSize) throw new ArgumentException("invalid format", nameof(key));
            return new ContainerID
            {
                Value = ByteString.CopyFrom(key[1..]),
            };
        }

        private byte[] GraveYardKey(Address address)
        {
            if (address is null) throw new ArgumentNullException(nameof(address));
            return Concat(GraveYardPrefix, address.ContainerId.Value.ToByteArray(), address.ObjectId.Value.ToByteArray());
        }

        private Address ParseGraveYardKey(byte[] key)
        {
            return ParseAddress(key);
        }

        private byte[] Primarykey(Address address)
        {
            if (address is null) throw new ArgumentNullException(nameof(address));
            return Concat(ObjectPrefix, address.ContainerId.Value.ToByteArray(), address.ObjectId.Value.ToByteArray());
        }

        private Address ParsePrimaryKey(byte[] key)
        {
            return ParseAddress(key);
        }

        private byte[] TombstoneKey(Address address)
        {
            if (address is null) throw new ArgumentNullException(nameof(address));
            return Concat(TombstonePrefix, address.ContainerId.Value.ToByteArray(), address.ObjectId.Value.ToByteArray());
        }

        private Address ParseTombstoneKey(byte[] key)
        {
            return ParseAddress(key);
        }

        private byte[] StorageGroupKey(Address address)
        {
            if (address is null) throw new ArgumentNullException(nameof(address));
            return Concat(StorageGroupPrefix, address.ContainerId.Value.ToByteArray(), address.ObjectId.Value.ToByteArray());
        }

        private Address ParseStorageGroupKey(byte[] key)
        {
            return ParseAddress(key);
        }

        private byte[] RootKey(Address address)
        {
            if (address is null) throw new ArgumentNullException(nameof(address));
            return Concat(RootPrefix, address.ContainerId.Value.ToByteArray(), address.ObjectId.Value.ToByteArray());
        }

        private Address ParseRootKey(byte[] key)
        {
            return ParseAddress(key);
        }


        private byte[] ParentKey(ContainerID cid, ObjectID parent)
        {
            if (parent is null || cid is null) throw new ArgumentException();
            return Concat(ParentPrefix, cid.Value.ToByteArray(), parent.Value.ToByteArray());
        }

        private void ParseParentKey(byte[] key, out ContainerID cid, out ObjectID parent)
        {
            var address = ParseAddress(key);
            cid = address.ContainerId;
            parent = address.ObjectId;
        }

        private byte[] SmallKey(Address address)
        {
            if (address is null) throw new ArgumentNullException(nameof(address));
            return Concat(SmallPrefix, address.ContainerId.Value.ToByteArray(), address.ObjectId.Value.ToByteArray());
        }

        private Address ParseSmallKey(byte[] key)
        {
            return ParseAddress(key);
        }

        private byte[] ToMoveItKey(Address address)
        {
            if (address is null) throw new ArgumentNullException(nameof(address));
            return Concat(ToMoveItPrefix, address.ContainerId.Value.ToByteArray(), address.ObjectId.Value.ToByteArray());
        }

        private Address ParseToMoveItKey(byte[] key)
        {
            return ParseAddress(key);
        }

        private byte[] PayloadHashKey(ContainerID cid, Checksum checksum)
        {
            if (cid is null || checksum is null) throw new ArgumentNullException();
            return Concat(PayloadHashPrefix, cid.Value.ToByteArray(), checksum.Sum.ToByteArray());
        }

        private void ParsePayloadHashKey(byte[] key, out ContainerID cid, out byte[] sum)
        {
            if (key.Length < 1 + ContainerID.ValueSize) throw new ArgumentException("invalid format", nameof(key));
            int offset = 1;
            byte[] cidv = key[offset..(offset + ContainerID.ValueSize)];
            offset += ContainerID.ValueSize;
            sum = key[offset..];
            cid = new()
            {
                Value = ByteString.CopyFrom(cidv)
            };
        }

        private byte[] SplitKey(ContainerID cid, SplitID sid)
        {
            if (cid is null || sid is null) throw new ArgumentNullException();
            return Concat(SplitPrefix, cid.Value.ToByteArray(), sid.ToByteArray());
        }

        private void ParseSplitKey(byte[] key, out ContainerID cid, out SplitID sid)
        {
            if (key.Length != 1 + ContainerID.ValueSize + SplitID.Size) throw new ArgumentException("invalid format", nameof(key));
            int offset = 1;
            byte[] cidv = key[offset..(offset + ContainerID.ValueSize)];
            offset += ContainerID.ValueSize;
            byte[] sidv = key[offset..];
            cid = new()
            {
                Value = ByteString.CopyFrom(cidv)
            };
            sid = sidv;
        }

        private byte[] OwnerKey(Address address, OwnerID wid)
        {
            if (address is null || wid is null) throw new ArgumentNullException();
            return Concat(OwnerPrefix, address.ContainerId.Value.ToByteArray(), wid.Value.ToByteArray(), address.ObjectId.Value.ToByteArray());
        }

        private void ParseOwnerKey(byte[] key, out Address address, out OwnerID wid)
        {
            if (key.Length != 1 + ContainerID.ValueSize + OwnerID.ValueSize) throw new ArgumentException("invalid format", nameof(key));
            int offset = 1;
            byte[] cidv = key[offset..(offset + ContainerID.ValueSize)];
            offset += ContainerID.ValueSize;
            byte[] widv = key[offset..(offset + OwnerID.ValueSize)];
            offset += OwnerID.ValueSize;
            byte[] oidv = key[offset..];
            address = new()
            {
                ContainerId = new()
                {
                    Value = ByteString.CopyFrom(cidv)
                },
                ObjectId = new()
                {
                    Value = ByteString.CopyFrom(oidv)
                }
            };
            wid = new()
            {
                Value = ByteString.CopyFrom(widv)
            };
        }

        private byte[] AttributeKey(Address address, Header.Types.Attribute attr)
        {
            if (address is null || attr is null) throw new ArgumentNullException();
            return Concat(AttributePrefix, address.ContainerId.Value.ToByteArray(), StrictUTF8.GetBytes(attr.Key), StrictUTF8.GetBytes(attr.Value), address.ObjectId.Value.ToByteArray());
        }

        private void ParseAttributeKey(byte[] key, out Address address, out byte[] attribute)
        {
            if (key.Length < 1 + ContainerID.ValueSize + ObjectID.ValueSize + 2) throw new ArgumentException("invalid format", nameof(key));
            byte[] cidv = key[1..(1 + ContainerID.ValueSize)];
            byte[] oidv = key[^ObjectID.ValueSize..];
            address = new()
            {
                ContainerId = new()
                {
                    Value = ByteString.CopyFrom(cidv)
                },
                ObjectId = new()
                {
                    Value = ByteString.CopyFrom(oidv)
                }
            };
            attribute = key[(1 + ContainerID.ValueSize)..^ObjectID.ValueSize];
        }
    }
}
