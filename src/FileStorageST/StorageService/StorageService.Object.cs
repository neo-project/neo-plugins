using System;
using Akka.Actor;
using Neo.FileStorage.API.Acl;
using Neo.FileStorage.API.Refs;
using Neo.FileStorage.Cache;
using Neo.FileStorage.Placement;
using Neo.FileStorage.Storage.Services.Container.Announcement;
using Neo.FileStorage.Storage.Services.Object.Acl;
using Neo.FileStorage.Storage.Services.Object.Get;
using Neo.FileStorage.Storage.Services.Object.Put;
using Neo.FileStorage.Storage.Services.Object.Search;
using Neo.FileStorage.Storage.Services.Object.Search.Clients;
using Neo.FileStorage.Storage.Services.Object.Util;
using Neo.FileStorage.Storage.Services.Police;
using Neo.FileStorage.Storage.Services.Replicate;
using Neo.FileStorage.Storage.Services.Reputaion.Local.Client;
using FSContainer = Neo.FileStorage.API.Container.Container;

namespace Neo.FileStorage.Storage
{
    public sealed partial class StorageService : IDisposable
    {
        private ReputationClientCache reputationClientCache;

        private ObjectServiceImpl InitializeObject()
        {
            KeyStorage keyStorage = new(key, tokenStore);
            reputationClientCache = new()
            {
                StorageNode = this,
                MorphInvoker = morphInvoker,
                ReputationStorage = new(),
            };
            IActorRef replicator = system.ActorSystem.ActorOf(Replicator.Props(new()
            {
                RemoteSender = new()
                {
                    KeyStorage = keyStorage,
                    ClientCache = reputationClientCache,
                },
                LocalStorage = localStorage,
            }));
            IActorRef policeRef = system.ActorSystem.ActorOf(Policer.Props(new()
            {
                LocalAddress = LocalAddress,
                LocalStorage = localStorage,
                MorphInvoker = morphInvoker,
                PlacementBuilder = new NetworkMapBuilder(morphInvoker),
                RemoteHeader = new()
                {
                    KeyStorage = keyStorage,
                    ClientCache = reputationClientCache,
                },
                RedundantCopyCallback = address =>
                {
                    localStorage.Inhume(null, address);
                },
                ReplicatorRef = replicator
            }));

            TTLNetworkCache<ContainerID, FSContainer> containerCache = new(ContainerCacheSize, TimeSpan.FromSeconds(ContainerCacheTTLSeconds), cid =>
            {
                return morphInvoker.GetContainer(cid)?.Container;
            });
            TTLNetworkCache<ContainerID, EACLTable> eaclCache = new(EACLCacheSize, TimeSpan.FromSeconds(EACLCacheTTLSeconds), cid =>
            {
                return morphInvoker.GetEACL(cid)?.Table;
            });

            LoadPlacementBuilder loadPlacementBuilder = new()
            {
                MorphInvoker = morphInvoker,
            };

            LocalObjectInhumer objInhumer = new()
            {
                LocalStorage = localStorage,
            };
            var remover = new LocalObjectRemover
            {
                LocalStorage = localStorage,
            };
            PutService putService = new()
            {
                MorphInvoker = morphInvoker,
                LocalAddress = LocalAddress,
                KeyStorage = keyStorage,
                LocalStorage = localStorage,
                ObjectInhumer = objInhumer,
                ClientCache = reputationClientCache,
            };
            GetService getService = new()
            {
                Assemble = true,
                KeyStorage = keyStorage,
                LocalStorage = localStorage,
                ClientCache = reputationClientCache,
                MorphInvoker = morphInvoker,
                TraverserGenerator = new(morphInvoker, LocalAddress, 1),
            };
            SearchService searchService = new()
            {
                KeyStorage = keyStorage,
                LocalStorage = localStorage,
                MorphClient = new EpochSource(morphInvoker),
                ClientCache = new SearchClientCache(reputationClientCache),
                TraverserGenerator = new TraverserGenerator(morphInvoker, LocalAddress, trackCopies: false),
            };
            return new ObjectServiceImpl
            {
                AclChecker = new(),
                SignService = new()
                {
                    Key = key,
                    ResponseService = new()
                    {
                        StorageNode = this,
                        SplitService = new()
                        {
                            ObjectService = new()
                            {
                                GetService = getService,
                                PutService = putService,
                                SearchService = searchService,
                                DeleteService = new()
                                {
                                    MorphInvoker = morphInvoker,
                                    PutService = putService,
                                    SearchService = searchService,
                                    GetService = getService,
                                    KeyStorage = keyStorage,
                                },
                            }
                        }
                    }
                }
            };
        }
    }
}
