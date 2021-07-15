using System;
using System.Linq;
using System.Threading;
using Neo.FileStorage.API.Cryptography;
using Neo.FileStorage.API.Netmap;
using Neo.FileStorage.Morph.Event;
using Neo.FileStorage.Storage.Services.Netmap;

namespace Neo.FileStorage.Storage
{
    public sealed partial class StorageService : IDisposable
    {
        private const int reBootstrapInterval = 2;
        private ulong startEpoch;
        private long reBoostrapTurnedOff = 0;

        private NetmapServiceImpl InitializeNetmap()
        {
            startEpoch = morphInvoker.Epoch();
            netmapProcessor.AddEpochParser(MorphEvent.NewEpochEvent.ParseNewEpochEvent);
            netmapProcessor.AddEpochHandler(p =>
            {
                if (p is MorphEvent.NewEpochEvent e)
                {
                    Interlocked.Exchange(ref CurrentEpoch, e.EpochNumber);
                }
            });
            netmapProcessor.AddEpochHandler(p =>
            {
                if (p is MorphEvent.NewEpochEvent e)
                {
                    var ni = NetmapLocalNodeInfo(e.EpochNumber);
                    if (ni is null)
                        Utility.Log(nameof(StorageService), LogLevel.Debug, $"could not update node info, not found in netmap");
                    Interlocked.Exchange(ref LocalNodeInfo, ni);
                }
            });
            netmapProcessor.AddEpochHandler(p =>
            {
                if (p is MorphEvent.NewEpochEvent e)
                {
                    if (0 < Interlocked.Read(ref reBoostrapTurnedOff)) return;
                    if ((e.EpochNumber - startEpoch) % reBootstrapInterval == 0)
                    {
                        var ni = LocalNodeInfo.Clone();
                        ni.State = NodeInfo.Types.State.Online;
                        try
                        {
                            var r = morphInvoker.AddPeer(ni);
                            if (!r) throw new InvalidOperationException("add peer return false");
                        }
                        catch (Exception exp)
                        {
                            Utility.Log(nameof(StorageService), LogLevel.Debug, $"could not add peer when rebootstrap, error: {exp}");
                        }
                    }
                }
            });
            return new NetmapServiceImpl
            {
                SignService = new()
                {
                    Key = key,
                    ResponseService = new()
                    {
                        StorageNode = this,
                        NetmapService = new()
                        {
                            StorageNode = this,
                        }
                    }
                }
            };
        }

        private NodeInfo NetmapLocalNodeInfo(ulong epoch)
        {
            var nm = morphInvoker.EpochSnapshot(epoch);
            Node node = null;
            foreach (var n in nm.Nodes)
            {
                if (n.PublicKey.SequenceEqual(key.PublicKey()))
                {
                    node = n;
                    break;
                }
            }
            return node?.Info;
        }
    }
}
