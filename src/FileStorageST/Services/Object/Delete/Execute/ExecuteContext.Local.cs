using Google.Protobuf;

namespace Neo.FileStorage.Storage.Services.Object.Delete.Execute
{
    public partial class ExecuteContext
    {
        private void ExecuteLocal()
        {
            FromTombstone();
            SaveTomestone();
            BroadcastTomstone();
        }

        private void FromTombstone()
        {
            ulong ts_lifetime = TombstoneLifetime();
            tombstone = new()
            {
                ExpirationEpoch = CurrentEpoch() + ts_lifetime,
            };
            AddMembers(new() { Prm.Address.ObjectId });
            FromSplitInfo();
            if (SplitInfo is not null)
            {
                tombstone.SplitId = SplitInfo.SplitId;
                CollectMembers();
            }
            InitTombstoneObject();
        }

        private void SaveTomestone()
        {
            var id = DeleteService.PutService.Put(this, false);
            Prm.Writer.SetAddress(new()
            {
                ContainerId = Prm.Address.ContainerId,
                ObjectId = id,
            });
        }

        private void BroadcastTomstone()
        {
            _ = DeleteService.PutService.Put(this, false);
        }

        private void InitTombstoneObject()
        {
            var payload = tombstone.ToByteString();
            TombstoneObject = new();
            TombstoneObject.Header = new();
            TombstoneObject.Header.ContainerId = Prm.Address.ContainerId;
            TombstoneObject.Header.OwnerId = Prm.SessionToken.Body.OwnerId;
            TombstoneObject.Header.ObjectType = API.Object.ObjectType.Tombstone;
            TombstoneObject.Payload = payload;
            TombstoneObject.Attributes.Add(new() { Key = API.Object.Header.Types.Attribute.SysAttributeExpEpoch, Value = "10" });
        }

        private ulong TombstoneLifetime()
        {
            return 0; // TODO: fix
        }

        private ulong CurrentEpoch()
        {
            return DeleteService.MorphInvoker.Epoch();
        }
    }
}
