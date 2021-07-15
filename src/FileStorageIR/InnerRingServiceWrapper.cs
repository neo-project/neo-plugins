using System;
using System.Collections.Generic;
using Akka.Actor;
using Neo.Ledger;
using Neo.Network.P2P.Payloads;
using Neo.Persistence;
using Neo.VM;
using Neo.Wallets;
using static Neo.FileStorage.InnerRing.InnerRingService;
using FSInnerRingService = Neo.FileStorage.InnerRing.InnerRingService;

namespace Neo.FileStorage.InnerRing
{
    public sealed class InnerRingServiceWrapper : IDisposable
    {
        private readonly IActorRef innering;

        public InnerRingServiceWrapper(NeoSystem main, NeoSystem side)
        {
            innering = main.ActorSystem.ActorOf(FSInnerRingService.Props(main, side));//TODO: mount to side chain?
            innering.Tell(new Start() { });
        }

        public void OnPersisted(Block block, DataCache _, IReadOnlyList<Blockchain.ApplicationExecuted> applicationExecutedList, bool flag)
        {
            if (!flag) innering.Tell(new BlockEvent() { block = block, flag = flag });
            foreach (var appExec in applicationExecutedList)
            {
                Transaction tx = appExec.Transaction;
                VMState state = appExec.VMState;
                if (tx is null || state != VMState.HALT) continue;
                var notifys = appExec.Notifications;
                if (notifys is null) continue;
                foreach (var notify in notifys)
                {
                    var contract = notify.ScriptHash;
                    if (flag && contract != Settings.Default.FsContractHash) continue;
                    if (!flag && !Settings.Default.Contracts.Contains(contract)) continue;
                    innering.Tell(new ContractEvent() { notify = notify, flag = flag });
                }
            }
        }
        public void Dispose()
        {
            innering.Tell(new Stop() { });
        }
    }
}
