using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Threading;
using Akka.Actor;
using Neo.FileStorage.API.Acl;
using Neo.FileStorage.API.Container;
using Neo.FileStorage.API.Cryptography;
using Neo.FileStorage.API.Netmap;
using Neo.FileStorage.API.Refs;
using Neo.FileStorage.Cache;
using Neo.FileStorage.Core.Object;
using Neo.FileStorage.LocalObjectStorage.Engine;
using Neo.FileStorage.Morph.Event;
using Neo.FileStorage.Morph.Invoker;
using Neo.FileStorage.Network.Cache;
using Neo.FileStorage.Services.Accounting;
using Neo.FileStorage.Services.Container;
using Neo.FileStorage.Services.Container.Announcement;
using Neo.FileStorage.Services.Container.Announcement.Control;
using Neo.FileStorage.Services.Container.Announcement.Route;
using Neo.FileStorage.Services.Container.Announcement.Storage;
using Neo.FileStorage.Services.Control;
using Neo.FileStorage.Services.Control.Service;
using Neo.FileStorage.Services.Netmap;
using Neo.FileStorage.Services.Object.Acl;
using Neo.FileStorage.Services.Object.Get;
using Neo.FileStorage.Services.Object.Put;
using Neo.FileStorage.Services.Object.Search;
using Neo.FileStorage.Services.Object.Util;
using Neo.FileStorage.Services.ObjectManager.Placement;
using Neo.FileStorage.Services.Police;
using Neo.FileStorage.Services.Replicate;
using Neo.FileStorage.Services.Reputaion.Local.Client;
using Neo.FileStorage.Services.Session;
using Neo.FileStorage.Services.Session.Storage;
using Neo.FileStorage.Storage;
using Neo.FileStorage.Storage.Processors;
using Neo.Ledger;
using Neo.Network.P2P.Payloads;
using Neo.Persistence;
using Neo.VM;
using Neo.Wallets;
using APIAccountingService = Neo.FileStorage.API.Accounting.AccountingService;
using APIContainerService = Neo.FileStorage.API.Container.ContainerService;
using APINetmapService = Neo.FileStorage.API.Netmap.NetmapService;
using APIObjectService = Neo.FileStorage.API.Object.ObjectService;
using APISessionService = Neo.FileStorage.API.Session.SessionService;
using FSContainer = Neo.FileStorage.API.Container.Container;

namespace Neo.FileStorage
{
    public sealed partial class StorageService : IDisposable
    {
        private ControlServiceImpl InitializeControl(StorageEngine localStorage)
        {
            return new ControlServiceImpl
            {
                Key = key,
                LocalStorage = localStorage,
                MorphClient = morphClient,
                StorageNode = this,
            };
        }
    }
}
