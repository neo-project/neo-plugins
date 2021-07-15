using System;
using System.Collections.Generic;
using System.Linq;
using Google.Protobuf;
using Neo.FileStorage.API.Object;
using Neo.FileStorage.API.Refs;
using Neo.FileStorage.Storage.LocalObjectStorage.Blob;
using static Neo.FileStorage.Storage.LocalObjectStorage.Helper;
using static Neo.FileStorage.Storage.LocalObjectStorage.Metabase.Helper;
using FSObject = Neo.FileStorage.API.Object.Object;

namespace Neo.FileStorage.Storage.LocalObjectStorage.Metabase
{
    public sealed partial class MB
    {
        public void Put(FSObject obj, BlobovniczaID bid = null, SplitInfo si = null)
        {
            bool is_parent = si is not null;
            bool exist;
            try
            {
                exist = Exists(obj.Address);
            }
            catch (SplitInfoException)
            {
                exist = true;
            }
            if (exist)
            {
                if (!is_parent && bid is not null)
                {
                    UpdateBlobovniczaID(obj.Address, bid);
                    return;
                }
                if (is_parent)
                {
                    UpdateSplitInfo(obj.Address, si);
                    return;
                }
                return;
            }
            if (obj.Parent is not null && !is_parent)
            {
                SplitInfo psi = SplitInfoFromObject(obj);
                Put(obj.Parent, bid, psi);
            }
            foreach (var item in UniqueIndexes(obj, si, bid))
            {
                db.Put(item.Item1, item.Item2);
            }
            foreach (var item in ListIndexes(obj))
            {
                List<ObjectID> list;
                var data = db.Get(item.Item1);
                if (data is null)
                    list = new();
                else
                    list = DecodeObjectIDList(data);
                list.Add(item.Item2);
                db.Put(item.Item1, EncodeObjectIDList(list));
            }
            foreach (var key in FakeBucketTreeIndexes(obj))
            {
                db.Put(key, ZeroValue);
            }
            if (obj.ObjectType == ObjectType.Regular && !is_parent)
            {
                ChangeContainerSize(obj.ContainerId, obj.PayloadSize, true);
            }
        }

        private void UpdateBlobovniczaID(Address address, BlobovniczaID bid)
        {
            byte[] key = SmallKey(address);
            if (db.Get(key) is null)
                throw new InvalidOperationException("updating blobovnicza id on object without it");
            db.Put(SmallKey(address), bid);
        }

        private void UpdateSplitInfo(Address address, SplitInfo si)
        {
            byte[] key = RootKey(address);
            byte[] old = db.Get(key);
            if (old is null)
                throw new InvalidOperationException("updating split info on object without it");
            SplitInfo osi = SplitInfo.Parser.ParseFrom(old);
            si = MergeSplitInfo(osi, si);
            db.Put(key, si.ToByteArray());
        }

        private SplitInfo SplitInfoFromObject(FSObject obj)
        {
            if (obj.Parent is null) return null;
            SplitInfo result = new();
            result.SplitId = obj.SplitId.ToByteString();
            if (IsLinkObject(obj))
                result.Link = obj.ObjectId;
            else if (IsLastObject(obj))
                result.LastPart = obj.ObjectId;
            else
                throw new InvalidOperationException("invalid root object");
            return result;
        }

        private bool IsLinkObject(FSObject obj)
        {
            return obj.Children.Any() && obj.Parent is not null;
        }

        private bool IsLastObject(FSObject obj)
        {
            return !obj.Children.Any() && obj.Parent is not null;
        }

        private List<(byte[], byte[])> UniqueIndexes(FSObject obj, SplitInfo si, BlobovniczaID bid)
        {
            List<(byte[], byte[])> result = new();
            if (si is null)
            {
                switch (obj.ObjectType)
                {
                    case ObjectType.Regular:
                        result.Add((Primarykey(obj.Address), obj.CutPayload().ToByteArray()));
                        break;
                    case ObjectType.Tombstone:
                        result.Add((TombstoneKey(obj.Address), obj.CutPayload().ToByteArray()));
                        break;
                    case ObjectType.StorageGroup:
                        result.Add((StorageGroupKey(obj.Address), obj.CutPayload().ToByteArray()));
                        break;
                    default:
                        throw new UnknownObjectTypeException();
                }
                if (bid is not null)
                    result.Add((SmallKey(obj.Address), bid));
            }
            if (obj.ObjectType == ObjectType.Regular && !obj.HasParent)
            {
                byte[] splitInfo = Array.Empty<byte>();
                if (si is not null) splitInfo = si.ToByteArray();
                result.Add((RootKey(obj.Address), splitInfo));
            }
            return result;
        }
    }
}
