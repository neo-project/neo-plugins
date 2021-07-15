using System;
using System.Collections.Generic;
using System.Linq;

namespace Neo.FileStorage.Storage.Services.Reputaion.Common.Route
{
    public class TrustWriter : IWriter
    {
        public Router Router { get; init; }
        public RouteContext RouteContext { get; init; }
        private readonly Dictionary<string, IWriter> servers = new();
        private readonly object writeLock = new();

        public void Write(Trust trust)
        {
            lock (writeLock)
            {
                var route = Router.RouteBuilder.NextStage(RouteContext.Epoch, trust, RouteContext.Passed);
                if (!route.Any()) route = new() { null };
                foreach (var remote in route)
                {
                    string endpoint = "";
                    if (remote is not null) endpoint = remote.Address;
                    if (!servers.TryGetValue(endpoint, out IWriter writer))
                    {
                        try
                        {
                            var provider = Router.RemoteWriterProvider.InitRemote(remote);
                            writer = provider.InitWriter(RouteContext);
                            servers[endpoint] = writer;
                        }
                        catch (Exception)
                        {
                            continue;
                        }
                    }
                    try
                    {
                        writer.Write(trust);
                    }
                    catch { }
                }
            }
        }

        public void Close()
        {
            foreach (var writer in servers.Values)
                writer.Close();
        }
    }
}
