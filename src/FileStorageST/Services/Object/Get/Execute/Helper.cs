using Google.Protobuf;
using Neo.FileStorage.API.Client;
using Neo.FileStorage.API.Session;
using Neo.FileStorage.Storage.LocalObjectStorage.Engine;
using Neo.FileStorage.Storage.Services.Reputaion.Local.Client;
using FSObject = Neo.FileStorage.API.Object.Object;

namespace Neo.FileStorage.Storage.Services.Object.Get.Execute
{
    public static class Helper
    {
        public static FSObject GetObject(this IFSClient client, ExecuteContext context)
        {
            if (!context.Assembling && context.Prm.Forwarder is not null)
                return context.Prm.Forwarder.Forward(client.Raw());
            var options = context.Prm.CallOptions
                .WithExtraXHeaders(new XHeader[] { new() { Key = XHeader.XHeaderNetmapEpoch, Value = context.CurrentEpoch.ToString() } })
                .WithKey(context.Prm.Key);
            if (context.HeadOnly)
            {
                return client.GetObjectHeader(
                    context.Prm.Address,
                    false,
                    context.Prm.Raw,
                    options,
                    context.Cancellation).Result;
            }
            if (context.Range is not null)
            {
                var data = client.GetObjectPayloadRangeData(context.Prm.Address,
                    context.Range,
                    context.Prm.Raw,
                    options,
                    context.Cancellation).Result;
                return new() { Payload = ByteString.CopyFrom(data) };
            }
            return client.GetObject(
                context.Prm.Address,
                context.Prm.Raw,
                options,
                context.Cancellation).Result;
        }

        public static FSObject GetObject(this StorageEngine engine, ExecuteContext context)
        {
            if (context.HeadOnly)
            {
                return engine.Head(context.Prm.Address, context.Prm.Raw);
            }
            else if (context.Range is not null)
            {
                return engine.GetRange(context.Prm.Address, context.Range);
            }
            else
            {
                return engine.Get(context.Prm.Address);
            }
        }

        public static bool IsChild(this FSObject obj)
        {
            return obj.Parent != null && obj.Parent.Address.Equals(obj.Address);
        }
    }
}
