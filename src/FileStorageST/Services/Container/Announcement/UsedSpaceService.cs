using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Threading;
using Neo.FileStorage.API.Container;
using Neo.FileStorage.API.Cryptography;
using Neo.FileStorage.API.Netmap;
using Neo.FileStorage.Storage.Services.Container.Announcement.Route;
using Neo.FileStorage.Storage.Services.Container.Announcement.Route.Placement;
using FSAnnouncement = Neo.FileStorage.API.Container.AnnounceUsedSpaceRequest.Types.Body.Types.Announcement;

namespace Neo.FileStorage.Storage.Services.Container.Announcement
{
    public class UsedSpaceService
    {
        public ECDsa Key { get; init; }
        public NodeInfo LocalNodeInfo { get; init; }
        public LoadRouter Router { get; init; }
        public LoadPlacementBuilder Loadbuilder { get; init; }
        public RouteBuilder RouteBuilder { get; init; }

        public AnnounceUsedSpaceResponse AnnounceUsedSpace(AnnounceUsedSpaceRequest request, CancellationToken context)
        {
            List<NodeInfo> passed = new();
            for (var header = request.VerifyHeader; header != null; header = header.Origin)
            {
                passed.Add(new() { PublicKey = header.BodySignature.Key });
            }
            passed.Reverse();
            passed.Add(LocalNodeInfo);

            var writer = Router.InitWriter(new RouteContext(passed, context));
            foreach (var announcement in request.Body.Announcements)
            {
                ProcessLoadValue(announcement, passed, writer);
            }
            var resp = new AnnounceUsedSpaceResponse()
            {
                Body = new AnnounceUsedSpaceResponse.Types.Body { }
            };
            Key.SignResponse(resp);
            return resp;
        }

        private void ProcessLoadValue(FSAnnouncement announcement, List<NodeInfo> route, LoadWriter writer)
        {
            if (!Loadbuilder.IsNodeFromContainerKey(announcement.Epoch, announcement.ContainerId, route[0].PublicKey.ToByteArray()))
                throw new Exception("node outside the container");
            RouteBuilder.CheckRoute(announcement, route);
            writer.Put(announcement);
        }
    }
}
