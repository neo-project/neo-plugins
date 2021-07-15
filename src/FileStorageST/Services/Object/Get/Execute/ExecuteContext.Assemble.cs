using System;
using System.Collections.Generic;
using System.Linq;
using Neo.FileStorage.API.Object;
using Neo.FileStorage.API.Refs;
using Neo.FileStorage.Storage.LocalObjectStorage;
using Neo.FileStorage.Storage.Services.Object.Get.Writer;
using Neo.FileStorage.Storage.Utils;
using static Neo.Utility;
using FSObject = Neo.FileStorage.API.Object.Object;
using FSRange = Neo.FileStorage.API.Object.Range;

namespace Neo.FileStorage.Storage.Services.Object.Get.Execute
{
    public partial class ExecuteContext
    {
        private (ObjectID, List<ObjectID>) InitFromChild(ObjectID oid)
        {
            var child = GetChild(oid, null, true);
            var parent = child.Parent;
            if (parent is null)
            {
                throw new InvalidOperationException("asseble, received child with empty parent");
            }
            collectedObject = parent;
            if (Range is not null)
            {
                var seek_len = Range.Length;
                var seek_off = Range.Offset;
                var parent_size = parent.PayloadSize;
                if (parent_size < seek_off + seek_len)
                {
                    throw new RangeOutOfBoundsException();
                }
                var child_size = child.PayloadSize;
                currentOffset = parent_size - child_size;
                ulong from = 0;
                if (currentOffset < seek_off)
                    from = seek_off - currentOffset;
                ulong to = 0;
                if (currentOffset + from < seek_off + seek_len)
                    to = seek_off + seek_len - currentOffset;
                collectedObject.Payload = child.Payload.Range(from, to);
            }
            else
            {
                collectedObject.Payload = child.Payload;
            }
            return (child.PreviousId, child.Children.ToList());
        }

        private FSObject GetChild(ObjectID oid, FSRange range, bool with_header)
        {
            var writer = new SimpleObjectWriter();
            RangePrm prm = new();
            prm.WithGetCommonPrm(Prm);
            prm.Writer = writer;
            prm.Range = Range;
            prm.Address = new()
            {
                ContainerId = Prm.Address.ContainerId,
                ObjectId = oid,
            };
            prm.Local = false;
            GetService.Get(prm, range, false, Cancellation);
            var child = writer.Obj;
            if (with_header && !child.IsChild())
            {
                throw new InvalidOperationException("assemble, wrong child header");
            }
            return child;
        }

        private void Assemble()
        {
            Log("GetExecutor", LogLevel.Debug, "trying to assemble the object...");
            Assembling = true;
            var child_id = splitInfo.Link;
            if (child_id is null)
                child_id = splitInfo.LastPart;
            var result = InitFromChild(child_id);
            var prev = result.Item1;
            var children = result.Item2;
            if (children != null && 0 < children.Count)
            {
                if (Range is null)
                {
                    if (WriteCollectedHeader())
                    {
                        OvertakePayloadDirectly(children, null, false);
                    }
                }
                else
                {
                    if (OvertakePayloadInReverse(children[^1]))
                    {
                        WriteObjectPayload(collectedObject);
                    }
                }
            }
            else if (prev != null)
            {
                if (WriteCollectedHeader())
                {
                    if (OvertakePayloadInReverse(prev))
                    {
                        WriteObjectPayload(collectedObject);
                    }
                }
            }
            else
            {
                Log("GetExecutor", LogLevel.Debug, " could not init parent from child");
            }
        }

        private void OvertakePayloadDirectly(List<ObjectID> children, List<FSRange> ranges, bool check_right)
        {
            var with_range = ranges is not null && 0 < ranges.Count && Range != null;
            for (int i = 0; i < children.Count; i++)
            {
                FSRange r = null;
                if (with_range) r = ranges[i];
                try
                {
                    var child = GetChild(children[i], r, !with_range && check_right);
                    WriteObjectPayload(child);
                }
                catch (Exception)
                {
                    return;
                }
            }
        }

        private bool OvertakePayloadInReverse(ObjectID prev)
        {
            if (!BuildChainInReverse(prev, out List<ObjectID> oids, out List<FSRange> ranges)) return false;
            oids.Reverse();
            ranges.Reverse();
            OvertakePayloadDirectly(oids, ranges, false);
            return true;
        }

        private FSObject HeadChild(ObjectID oid)
        {
            var child_addr = new Address
            {
                ContainerId = Prm.Address.ContainerId,
                ObjectId = oid,
            };
            var prm = new HeadPrm();
            prm.WithGetCommonPrm(Prm);
            prm.Local = false;
            prm.Address = child_addr;
            prm.Short = false;
            var writer = new SimpleObjectWriter();
            prm.Writer = writer;
            GetService.Head(prm, Cancellation);
            var child = writer.Obj;
            if (child.ParentId is not null && !child.IsChild())
            {
                throw new InvalidOperationException("assemble, parent address in child object differs");
            }
            return child;
        }

        private bool BuildChainInReverse(ObjectID prev, out List<ObjectID> oids, out List<FSRange> ranges)
        {
            ulong from = 0, to = 0;
            if (Range is not null)
            {
                from = Range.Offset;
                to = from + Range.Length;
            }
            oids = new List<ObjectID>();
            ranges = new List<FSRange>();
            while (prev != null)
            {
                if (currentOffset < from) break;
                FSObject head;
                try
                {
                    head = HeadChild(prev);
                }
                catch (Exception)
                {
                    return false;
                }
                if (Range is not null)
                {
                    var sz = head.PayloadSize;
                    currentOffset -= sz;
                    if (currentOffset < to)
                    {
                        var off = 0ul;
                        if (currentOffset < from)
                        {
                            off = from - currentOffset;
                            sz -= from - currentOffset;
                        }
                        if (to < currentOffset + off + sz)
                            sz = to - off - currentOffset;
                        var r = new FSRange
                        {
                            Offset = off,
                            Length = sz,
                        };
                        ranges.Add(r);
                        oids.Add(head.ObjectId);
                    }
                }
                else
                {
                    oids.Add(head.ObjectId);
                }
                prev = head.PreviousId;
            }
            return true;
        }
    }
}
