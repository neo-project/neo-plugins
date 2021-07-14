using Akka.Actor;
using Neo.IO;
using Neo.IO.Json;
using Neo.Ledger;
using Neo.Network.P2P.Payloads;
using Neo.Persistence;
using Neo.SmartContract;
using Neo.Wallets;
using System.Linq;

namespace Neo.Plugins.MultiSigInbox
{
    class MultiSigInboxService : UntypedActor
    {
        private readonly NeoSystem _system;
        private readonly IStore store;
        private readonly Wallet _wallet;
        private bool started = false;
        public class Start { }

        public MultiSigInboxService(NeoSystem system, IStore store, Wallet wallet)
           : this(system)
        {
            this.store = store;
            this._wallet = wallet;
        }

        internal MultiSigInboxService(NeoSystem system)
        {
            _system = system;
            Context.System.EventStream.Subscribe(Self, typeof(Blockchain.PersistCompleted));
        }

        protected override void OnReceive(object message)
        {
            if (message is Start)
            {
                if (started) return;
                OnStart();
            }
            else
            {
                if (!started) return;
                switch (message)
                {
                    case Blockchain.PersistCompleted p:
                        {
                            foreach (var tx in p.Block.Transactions)
                            {
                                store.Delete(MultiSigInboxPlugin.TxPrefix.Concat(tx.Hash.ToArray()).ToArray());
                            }
                            break;
                        }
                    case Blockchain.RelayResult rr:
                        {
                            if (rr.Result == VerifyResult.Succeed)
                            {
                                if (rr.Inventory is ExtensiblePayload payload && payload.Category == MultiSigInboxPlugin.MutiSigPayloadCategory)
                                    OnExtensiblePayload(payload);
                                else if (rr.Inventory is Transaction tx)
                                    OnVerifiedTransaction(tx);
                            }
                            break;
                        }
                }
            }
        }

        private void OnVerifiedTransaction(Transaction tx)
        {
            // Remove tx from inbox
            store.Delete(MultiSigInboxPlugin.TxPrefix.Concat(tx.Hash.ToArray()).ToArray());
        }

        private void OnExtensiblePayload(ExtensiblePayload payload)
        {
            // Add to inbox

            using var snapshot = _system.GetSnapshot();
            var newContext = ContractParametersContext.FromJson(JObject.Parse(payload.Data), snapshot);
            if (newContext.Completed) return;

            var tx = store.TryGet(MultiSigInboxPlugin.TxPrefix.Concat(newContext.Verifiable.Hash.ToArray()).ToArray());
            if (tx != null)
            {
                // merge context
                var somethingAdded = false;
                var oldContext = ContractParametersContext.FromJson(JObject.Parse(tx), snapshot);

                foreach (var entry in oldContext.ScriptHashes)
                {
                    var contract = _wallet.GetAccount(entry)?.Contract;
                    if (contract == null) continue;

                    var newSignatures = newContext.GetSignatures(entry);
                    if (newSignatures == null) continue;

                    var oldSignatures = oldContext.GetSignatures(entry)?.ToDictionary(u => u.Key, u => u.Value);
                    foreach (var sig in newSignatures)
                    {
                        if (oldSignatures?.ContainsKey(sig.Key) == true) continue;

                        if (oldContext.AddSignature(contract, sig.Key, sig.Value))
                        {
                            somethingAdded = true;
                        }
                    }
                }

                if (somethingAdded)
                {
                    if (oldContext.Completed)
                    {
                        oldContext.Verifiable.Witnesses = oldContext.GetWitnesses();
                        _system.Blockchain.Tell(oldContext.Verifiable);

                        store.Delete(MultiSigInboxPlugin.TxPrefix.Concat(oldContext.Verifiable.Hash.ToArray()).ToArray());
                    }
                    else
                    {
                        store.Put(MultiSigInboxPlugin.TxPrefix.Concat(oldContext.Verifiable.Hash.ToArray()).ToArray(), oldContext.ToJson().ToByteArray(false));
                    }
                }
            }
            else
            {
                // new context
                store.Put(MultiSigInboxPlugin.TxPrefix.Concat(newContext.Verifiable.Hash.ToArray()).ToArray(), newContext.ToJson().ToByteArray(false));
            }
        }

        private static void Log(string message, LogLevel level = LogLevel.Info)
        {
            Utility.Log(nameof(MultiSigInboxService), level, message);
        }

        private void OnStart()
        {
            Log("OnStart");
            started = true;
        }

        protected override void PostStop()
        {
            Log("OnStop");
            started = false;
            Context.System.EventStream.Unsubscribe(Self);
            base.PostStop();
        }

        public static Props Props(NeoSystem system, IStore store, Wallet wallet)
        {
            return Akka.Actor.Props.Create(() => new MultiSigInboxService(system, store, wallet));
        }
    }
}
