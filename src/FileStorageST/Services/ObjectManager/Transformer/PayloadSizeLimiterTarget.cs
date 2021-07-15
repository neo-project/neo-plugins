using System;
using System.Collections.Generic;
using System.Linq;
using Google.Protobuf;
using Neo.FileStorage.API.Object;
using Neo.FileStorage.API.Refs;
using FSAttribute = Neo.FileStorage.API.Object.Header.Types.Attribute;
using FSObject = Neo.FileStorage.API.Object.Object;


namespace Neo.FileStorage.Storage.Services.ObjectManager.Transformer
{
    public class PayloadSizeLimiterTarget : IObjectTarget
    {
        private readonly ulong maxSize;
        private readonly IObjectTarget target;
        private readonly List<ObjectID> previous = new();
        private readonly SplitID splitID;
        private ulong written;
        private FSObject current;
        private FSObject parent;
        private IEnumerable<FSAttribute> parentAttributes;
        private PayloadHasher[] currentHashers;
        private PayloadHasher[] parentHashers;

        public PayloadSizeLimiterTarget(ulong maxSz, IObjectTarget t)
        {
            maxSize = maxSz;
            splitID = new();
            target = t;
        }

        public void WriteHeader(FSObject obj)
        {
            current = FromObject(obj);
            Initialize();
        }

        public AccessIdentifiers Close()
        {
            return Release(true);
        }

        private void Initialize()
        {
            if (previous.Any())
            {
                if (previous.Count == 1)
                {
                    DetachParent();
                }
                current.Header.Split.Previous = previous[^1];
            }
            InitializeCurrent();
        }

        private void InitializeCurrent()
        {
            currentHashers = new PayloadHasher[]
            {
                new(ChecksumType.Sha256),
                new(ChecksumType.Tz),
            };
        }

        private AccessIdentifiers Release(bool close)
        {
            var withParent = close && previous.Count > 0;
            if (withParent)
            {
                Calculatehash(parent, parentHashers);
                parent.Header.PayloadLength = written;
                current.Header.Split.Parent = parent.ObjectId;
            }
            Calculatehash(current, currentHashers);
            target.WriteHeader(current);
            var ids = target.Close();
            previous.Add(ids.Self);
            if (withParent)
            {
                InitializeLinking(ids.ParentHeader);
                InitializeCurrent();
                Release(false);
            }
            return ids;
        }

        private void Calculatehash(FSObject obj, PayloadHasher[] hashers)
        {
            foreach (var hasher in hashers)
            {
                switch (hasher.Type)
                {
                    case ChecksumType.Sha256:
                        obj.PayloadChecksum = new()
                        {
                            Type = hasher.Type,
                            Sum = ByteString.CopyFrom(hasher.Sum()),
                        };
                        break;
                    case ChecksumType.Tz:
                        obj.PayloadHomomorphicHash = new()
                        {
                            Type = hasher.Type,
                            Sum = ByteString.CopyFrom(hasher.Sum()),
                        };
                        break;
                    default:
                        throw new InvalidOperationException($"{nameof(PayloadSizeLimiterTarget)} not supported checksum type");
                }
            }
        }

        private void InitializeLinking(FSObject parentHeader)
        {
            current = FromObject(current);
            current.Parent = parentHeader;
            current.Children = previous;
            current.SplitId = parentHeader.SplitId;
        }

        public void WriteChunk(byte[] chunk)
        {
            if (written > 0 && written % maxSize == 0)
            {
                if (written == maxSize)
                    PrepareFirstChild();
                Release(false);
                Initialize();
            }
            var len = (ulong)chunk.Length;
            var cut = len;
            var leftToEdge = maxSize - written % maxSize;
            if (cut > leftToEdge)
                cut = leftToEdge;
            Write(chunk);
            written += cut;
            if (len > leftToEdge)
                WriteChunk(chunk[(int)cut..]);
        }

        private void Write(byte[] chunk)
        {
            foreach (var hasher in currentHashers)
                hasher.Write(chunk);
            foreach (var hasher in parentHashers)
                hasher.Write(chunk);
            target.WriteChunk(chunk);
        }

        private void PrepareFirstChild()
        {
            current.Header.Split = new();
            current.SplitId = splitID;
            parentAttributes = current.Header.Attributes;
            current.Header.Attributes.Clear();
        }

        private void DetachParent()
        {
            parent = current;
            current = FromObject(parent);
            parent.Header.Split = null;
            parent.Signature = null;
            parentHashers = currentHashers;
            parent.Header.Attributes.Clear();
            parent.Header.Attributes.AddRange(parentAttributes);
        }

        private FSObject FromObject(FSObject obj)
        {
            FSObject r = new()
            {
                Header = new()
                {
                    ContainerId = obj.ContainerId,
                    OwnerId = obj.OwnerId,
                    ObjectType = obj.ObjectType,
                }
            };
            r.Attributes.AddRange(obj.Attributes);
            if (obj.SplitId is not null)
                r.Header.Split.SplitId = obj.SplitId.ToByteString();
            return r;
        }
    }
}
