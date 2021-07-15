using System;
using System.Linq;
using System.Threading.Tasks;
using Google.Protobuf;
using Neo.FileStorage.Core.Object;
using Neo.FileStorage.Placement;
using Neo.FileStorage.Storage.Services.ObjectManager.Transformer;
using FSObject = Neo.FileStorage.API.Object.Object;

namespace Neo.FileStorage.Storage.Services.Object.Put
{
    public class DistributeTarget : IObjectTarget
    {
        public Network.Address LocalAddress { get; init; }
        public Traverser Traverser { get; init; }
        public ObjectValidator ObjectValidator { get; init; }
        public Func<Network.Address, IObjectTarget> NodeTargetInitializer { get; init; }
        public Action<Network.Address> Relay;

        private FSObject obj;
        private byte[] payload;
        private int offset;

        public void WriteHeader(FSObject init)
        {
            obj = init;
            payload = new byte[obj.PayloadSize];
            offset = 0;
        }

        public void WriteChunk(byte[] chunk)
        {
            chunk.CopyTo(payload, offset);
            offset += chunk.Length;
        }

        public AccessIdentifiers Close()
        {
            obj.Payload = ByteString.CopyFrom(payload);
            if (!ObjectValidator.ValidateContent(obj))
                throw new InvalidOperationException($"{nameof(DistributeTarget)} invalid content");
            while (true)
            {
                var addrs = Traverser.Next();
                if (!addrs.Any()) break;
                var tasks = new Task[addrs.Count];
                for (int i = 0; i < addrs.Count; i++)
                {
                    tasks[i] = Task.Run(() =>
                    {
                        if (Relay is not null)
                            Relay(addrs[i]);
                        var target = NodeTargetInitializer(addrs[i]);
                        target.WriteHeader(obj);
                        target.Close();
                        Traverser.SubmitSuccess();
                    });
                }
                Task.WaitAll(tasks);
            }
            if (!Traverser.Success())
                throw new InvalidOperationException($"{nameof(DistributeTarget)} incomplete object put");
            return new AccessIdentifiers
            {
                Self = obj.ObjectId,
            };
        }
    }
}
