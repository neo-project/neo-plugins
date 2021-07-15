using System;
using Akka.Actor;
using Neo.FileStorage.API.Cryptography;
using Neo.FileStorage.Cache;
using Neo.FileStorage.Morph.Event;
using Neo.FileStorage.Morph.Invoker;
using Neo.FileStorage.Storage.Cache;
using Neo.FileStorage.Storage.Services.Reputaion.Common;
using Neo.FileStorage.Storage.Services.Reputaion.Common.Route;
using Neo.FileStorage.Storage.Services.Reputaion.EigenTrust.Calculate;
using Neo.FileStorage.Storage.Services.Reputaion.EigenTrust.Route;
using Neo.FileStorage.Storage.Services.Reputaion.EigenTrust.Storage.Consumers;
using Neo.FileStorage.Storage.Services.Reputaion.EigenTrust.Storage.Daughters;
using Neo.FileStorage.Storage.Services.Reputaion.Intermediate;
using Neo.FileStorage.Storage.Services.Reputaion.Local;
using Neo.FileStorage.Storage.Services.Reputaion.Local.Routes;
using Neo.FileStorage.Storage.Services.Reputaion.Local.Storage;
using Neo.FileStorage.Storage.Services.Reputaion.Service;
using Neo.FileStorage.Utils;

namespace Neo.FileStorage.Storage
{
    public sealed partial class StorageService : IDisposable
    {
        public const int DefaultReputationWorkPoolSize = 32;
        private BlockTimer eigenTrustTimer;

        private ReputationServiceImpl InitializeReputation()
        {
            IActorRef workPool = system.ActorSystem.ActorOf(WorkerPool.Props("Reputation", DefaultReputationWorkPoolSize));
            NetmapCache netmapCache = new(this, morphInvoker);
            TrustStorage trustStorage = new();
            DaughtersStorage daughterStorage = new();
            ConsumersStorage consumersStorage = new();
            ConsumerStorageWriterProvider consumerStorageWriterProvider = new()
            {
                Storage = consumersStorage
            };
            DaughterStorageWriterProvider daughterStorageWriterProvider = new()
            {
                Storage = daughterStorage
            };
            LocalTrustStorage localTrustStorage = new()
            {
                TrustStorage = trustStorage,
                NetmapCache = netmapCache,
                LocalKey = key.PublicKey()
            };
            ManagerBuilder managerBuilder = new()
            {
                NetmapCache = netmapCache
            };
            LocalRouteBuilder localRouteBuilder = new()
            {
                ManagerBuilder = managerBuilder
            };
            IntermediateRouteBuilder intermediateRouteBuilder = new()
            {
                ManagerBuilder = managerBuilder
            };
            ClientCache clientCache = new();
            RemoteTrustProvider remoteLocalTrustProvider = new()
            {
                LocalAddress = LocalAddress,
                DeadEndProvider = daughterStorageWriterProvider,
                ClientCache = clientCache,
                RemoteProvider = new Storage.Services.Reputaion.Local.Remote.RemoteProvider
                {
                    Key = key
                }
            };
            RemoteTrustProvider remoteIntermediateTrustProvider = new()
            {
                LocalAddress = LocalAddress,
                DeadEndProvider = consumerStorageWriterProvider,
                ClientCache = clientCache,
                RemoteProvider = new Storage.Services.Reputaion.Intermediate.Remote.RemoteProvider
                {
                    Key = key
                }
            };
            Router localTrustRouter = new()
            {
                LocalNodeInfo = LocalNodeInfo,
                RemoteWriterProvider = remoteLocalTrustProvider,
                RouteBuilder = localRouteBuilder
            };
            Router intermediateTrustRouter = new()
            {
                LocalNodeInfo = LocalNodeInfo,
                RemoteWriterProvider = remoteIntermediateTrustProvider,
                RouteBuilder = intermediateRouteBuilder
            };
            Calculator eigenTrustCalculator = new()
            {
                MorphInvoker = morphInvoker,
                InitialTrustSource = new()
                {
                    NetmapCache = netmapCache
                },
                IntermediateValueTarget = intermediateTrustRouter,
                WorkerPool = workPool,
                FinalWriteTarget = new()
                {
                    PrivateKey = key,
                    PublicKey = key.PublicKey(),
                    MorphInvoker = morphInvoker
                },
                DaughterTrustSource = new()
                {
                    DaughterStorage = daughterStorage,
                    ConsumerStorage = consumersStorage
                }
            };
            Storage.Services.Reputaion.EigenTrust.Control.Controller eigenTrustController = new()
            {
                DaughterTrustCalculator = eigenTrustCalculator,
                MorphInvoker = morphInvoker,
                WorkerPool = workPool
            };
            Storage.Services.Reputaion.Local.Control.Controller localTrustController = new()
            {
                LocalTrustStorage = localTrustStorage,
                Router = localTrustRouter
            };
            netmapProcessor.AddEpochHandler(p =>
            {
                if (p is MorphEvent.NewEpochEvent e)
                {
                    localTrustController.Report(e.EpochNumber);
                }
            });
            EigenTrustDuration duration = new() { MorphInvoker = morphInvoker };
            eigenTrustTimer = new(duration.Value, () =>
            {
                ulong epoch = 0;
                try
                {
                    epoch = morphInvoker.Epoch();
                }
                catch (Exception e)
                {
                    Utility.Log(nameof(StorageService), LogLevel.Debug, $"eigen trust timer handler when get epoch, error: {e}");
                }
                eigenTrustController.Continue(epoch);
            });
            blockTimers.Add(eigenTrustTimer);
            return new ReputationServiceImpl
            {
                SignService = new()
                {
                    Key = key,
                    ResponseService = new()
                    {
                        StorageNode = this,
                        ReputationService = new()
                        {
                            LocalRouter = localTrustRouter,
                            IntermediateRouter = intermediateTrustRouter,
                            RouteBuilder = localRouteBuilder
                        }
                    }
                }
            };
        }

        private class EigenTrustDuration
        {
            public MorphInvoker MorphInvoker { get; init; }
            private uint value = 0;

            public uint Value()
            {
                lock (this)
                {
                    if (value == 0)
                        UpdateInternal();
                    return value;
                }
            }

            public void Update()
            {
                lock (this)
                {
                    UpdateInternal();
                }
            }

            public void UpdateInternal()
            {
                try
                {
                    var amount = MorphInvoker.EigenTrustIterations();
                    var duration = MorphInvoker.EpochDuration();
                    value = (uint)(duration / amount);
                }
                catch
                {
                    return;
                }
            }
        }
    }
}
