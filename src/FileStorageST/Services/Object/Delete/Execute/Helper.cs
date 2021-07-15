using System;
using System.Collections.Generic;
using System.Linq;
using Neo.FileStorage.API.Object;
using Neo.FileStorage.API.Refs;
using Neo.FileStorage.Storage.Services.Object.Get;
using Neo.FileStorage.Storage.Services.Object.Get.Writer;
using Neo.FileStorage.Storage.Services.Object.Put;
using Neo.FileStorage.Storage.Services.Object.Search;
using Neo.FileStorage.Storage.Services.Object.Search.Writer;
using FSObject = Neo.FileStorage.API.Object.Object;

namespace Neo.FileStorage.Storage.Services.Object.Delete.Execute
{
    public static class Util
    {
        public static FSObject HeadAddress(this GetService service, ExecuteContext context, Address address)
        {
            var writer = new SimpleObjectWriter();
            var prm = new HeadPrm
            {
                Address = address,
                Raw = true,
                Writer = writer,
                Short = false,
            };
            prm.WithCommonPrm(context.Prm);
            service.Head(prm, context.Cancellation);
            return writer.Obj;
        }

        public static SplitInfo SplitInfo(this GetService service, ExecuteContext context)
        {
            try
            {
                service.HeadAddress(context, context.Prm.Address);
                return null;
            }
            catch (Exception e) when (e is LocalObjectStorage.SplitInfoException se)
            {
                return se.SplitInfo;
            }
            catch (Exception e) when (e is SplitInfoException se)
            {
                return se.SplitInfo;
            }
        }

        public static List<ObjectID> Children(this GetService service, ExecuteContext context)
        {
            Address address = new()
            {
                ObjectId = context.SplitInfo.Link,
                ContainerId = context.Prm.Address.ContainerId,
            };
            var header = service.HeadAddress(context, address);
            return header.Children.ToList();
        }

        public static ObjectID Previous(this GetService service, ExecuteContext context, ObjectID oid)
        {
            Address address = new()
            {
                ObjectId = oid,
                ContainerId = context.Prm.Address.ContainerId,
            };
            var header = service.HeadAddress(context, address);
            return header.PreviousId;
        }

        public static List<ObjectID> SplitMembers(this SearchService service, ExecuteContext context)
        {
            SearchFilters filters = new();
            filters.AddSplitIDFilter(MatchType.StringEqual, context.SplitInfo.SplitId);
            SimpleIDWriter writer = new();
            SearchPrm prm = new()
            {
                ContainerID = context.Prm.Address.ContainerId,
                Filters = filters,
                Writer = writer,
            };
            prm.WithCommonPrm(context.Prm);
            service.Search(prm, context.Cancellation);
            return writer.IDs;
        }

        public static ObjectID Put(this PutService service, ExecuteContext context, bool broadcast)
        {
            var streamer = service.Put(context.Cancellation);
            var prm = new PutInitPrm
            {
                Header = context.TombstoneObject.CutPayload(),
            };
            prm.WithCommonPrm(context.Prm);
            if (broadcast)
            {
                prm.TrackCopies = false;
            }
            streamer.Init(prm);
            streamer.Chunk(context.TombstoneObject.Payload);
            var resp = (PutResponse)streamer.Close();
            return resp.Body.ObjectId;
        }
    }
}
