using System.Security.Cryptography;
using Neo.FileStorage.API.Session;
using Neo.FileStorage.Morph.Invoker;
using FSObject = Neo.FileStorage.API.Object.Object;
using FSVersion = Neo.FileStorage.API.Refs.Version;

namespace Neo.FileStorage.Storage.Services.ObjectManager.Transformer
{
    public class FormatTarget : IObjectTarget
    {
        public ECDsa Key { get; init; }
        public IObjectTarget Next { get; init; }
        public SessionToken SessionToken { get; init; }
        public MorphInvoker MorphInvoker { get; init; }

        private FSObject obj;
        private ulong size;

        public void WriteHeader(FSObject obj)
        {
            this.obj = obj;
        }

        public void WriteChunk(byte[] chunk)
        {
            size += (ulong)chunk.Length;
            Next.WriteChunk(chunk);
        }

        public AccessIdentifiers Close()
        {
            var curEpoch = CurrentEpoch();
            obj.Header.Version = FSVersion.SDKVersion();
            obj.Header.PayloadLength = size;
            obj.Header.SessionToken = SessionToken;
            obj.Header.CreationEpoch = curEpoch;
            if (obj.Parent is not null && obj.Parent.Signature is null)
            {
                var parent = obj.Parent;
                parent.Header.SessionToken = SessionToken;
                parent.Header.CreationEpoch = curEpoch;
                parent.Signature = parent.CalculateIDSignature(Key);
                obj.Header.Split.Parent = parent.CalculateID();
                obj.Header.Split.ParentSignature = parent.Signature;
                obj.Header.Split.ParentHeader = parent.Header;
            }
            obj.Signature = obj.CalculateIDSignature(Key);
            Next.WriteHeader(obj);
            Next.Close();
            return new AccessIdentifiers
            {
                Self = obj.ObjectId,
                Parent = obj.ParentId,
                ParentHeader = obj.Parent,
            };
        }

        private ulong CurrentEpoch()
        {
            return MorphInvoker.Epoch();
        }
    }
}
