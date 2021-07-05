using Akka.Actor;
using Neo.IO;
using Neo.Ledger;
using Neo.Network.P2P;
using Neo.Network.P2P.Payloads;
using Neo.Wallets;
using System;
using System.Collections.Generic;
using System.Linq;
using static Neo.Ledger.Blockchain;

namespace Neo.Consensus
{
    partial class ConsensusService : UntypedActor
    {
        public class Start { }
        private class Timer { public uint Height; public byte ViewNumber; }

        private readonly ConsensusContext context;
        private readonly IActorRef localNode;
        private readonly IActorRef taskManager;
        private readonly IActorRef blockchain;
        private ICancelable timer_token;
        private DateTime block_received_time;
        private uint block_received_index;
        private bool started = false;

        /// <summary>
        /// This will record the information from last scheduled timer
        /// </summary>
        private DateTime clock_started = TimeProvider.Current.UtcNow;
        private TimeSpan expected_delay = TimeSpan.Zero;

        /// <summary>
        /// This will be cleared every block (so it will not grow out of control, but is used to prevent repeatedly
        /// responding to the same message.
        /// </summary>
        private readonly HashSet<UInt256> knownHashes = new HashSet<UInt256>();
        /// <summary>
        /// This variable is only true during OnRecoveryMessageReceived
        /// </summary>
        private bool isRecovering = false;
        private readonly Settings dbftSettings;
        private readonly NeoSystem neoSystem;

        public ConsensusService(NeoSystem neoSystem, Settings settings, Wallet wallet)
            : this(neoSystem, settings, new ConsensusContext(neoSystem, settings, wallet))
        {
        }

        internal ConsensusService(NeoSystem neoSystem, Settings settings, ConsensusContext context)
        {
            this.neoSystem = neoSystem;
            localNode = neoSystem.LocalNode;
            taskManager = neoSystem.TaskManager;
            blockchain = neoSystem.Blockchain;
            dbftSettings = settings;
            this.context = context;
            Context.System.EventStream.Subscribe(Self, typeof(Blockchain.PersistCompleted));
            Context.System.EventStream.Subscribe(Self, typeof(Blockchain.RelayResult));
        }

        private bool AddTransaction(Transaction tx, bool verify)
        {
            if (verify)
            {
                VerifyResult result = tx.Verify(neoSystem.Settings, context.Snapshot, context.VerificationContext);
                if (result != VerifyResult.Succeed)
                {
                    Log($"Rejected tx: {tx.Hash}, {result}{Environment.NewLine}{tx.ToArray().ToHexString()}", LogLevel.Warning);
                    RequestChangeView(result == VerifyResult.PolicyFail ? ChangeViewReason.TxRejectedByPolicy : ChangeViewReason.TxInvalid);
                    return false;
                }
            }
            context.Transactions[tx.Hash] = tx;
            context.VerificationContext.AddTransaction(tx);

            if(context.IsPrimary)
            {
                foreach (ExtensiblePayload payload in context.TXListPayloads)
                {
                    var message = context.GetMessage<TXListMessage>(payload);

                    if (message.TransactionHashes.Contains(tx.Hash) && !context.TXListVerification[message.ValidatorIndex].Contains(tx.Hash))
                    {
                        context.TXListVerification[message.ValidatorIndex].Append(tx.Hash);
                        CheckTXLists(message);
                    }
                    
                }
                    
            }
             
                

            return CheckPrepareResponse();
        }

        private void ChangeTimer(TimeSpan delay)
        {
            clock_started = TimeProvider.Current.UtcNow;
            expected_delay = delay;
            timer_token.CancelIfNotNull();
            timer_token = Context.System.Scheduler.ScheduleTellOnceCancelable(delay, Self, new Timer
            {
                Height = context.Block.Index,
                ViewNumber = context.ViewNumber
            }, ActorRefs.NoSender);
        }

        private void InitializeConsensus(byte viewNumber)
        {
            context.Reset(viewNumber);
            if (viewNumber > 0)
                Log($"View changed: view={viewNumber} primary={context.Validators[context.GetPrimaryIndex((byte)(viewNumber - 1u))]}", LogLevel.Warning);
            Log($"Initialize: height={context.Block.Index} view={viewNumber} index={context.MyIndex} role={(context.IsPrimary ? "Primary" : context.WatchOnly ? "WatchOnly" : "Backup")}");
            if (context.WatchOnly) return;
            if (context.IsPrimary)
            {
                if (isRecovering)
                {
                    ChangeTimer(TimeSpan.FromMilliseconds(neoSystem.Settings.MillisecondsPerBlock << (viewNumber + 1)));
                }
                else
                {
                    TimeSpan span = neoSystem.Settings.TimePerBlock;
                    if (block_received_index + 1 == context.Block.Index)
                    {
                        var diff = TimeProvider.Current.UtcNow - block_received_time;
                        if (diff >= span)
                            span = TimeSpan.Zero;
                        else
                            span -= diff;
                    }
                    ChangeTimer(span);
                }
            }
            else
            {
                ChangeTimer(TimeSpan.FromMilliseconds(neoSystem.Settings.MillisecondsPerBlock << (viewNumber + 1)));
            }
        }

        private static void Log(string message, LogLevel level = LogLevel.Info)
        {
            Utility.Log(nameof(ConsensusService), level, message);
        }

        // this function increases existing timer (never decreases) with a value proportional to `maxDelayInBlockTimes`*`Blockchain.MillisecondsPerBlock`
        private void ExtendTimerByFactor(int maxDelayInBlockTimes)
        {
            TimeSpan nextDelay = expected_delay - (TimeProvider.Current.UtcNow - clock_started) + TimeSpan.FromMilliseconds(maxDelayInBlockTimes * neoSystem.Settings.MillisecondsPerBlock / (double)context.M);
            if (!context.WatchOnly && !context.ViewChanging && !context.CommitSent && (nextDelay > TimeSpan.Zero))
                ChangeTimer(nextDelay);
        }

        private void OnPersistCompleted(Block block)
        {
            Log($"Persisted {nameof(Block)}: height={block.Index} hash={block.Hash} tx={block.Transactions.Length}");
            knownHashes.Clear();
            InitializeConsensus(0);
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
                    case Timer timer:
                        OnTimer(timer);
                        break;
                    case Transaction transaction:
                        OnTransaction(transaction);
                        break;
                    case Blockchain.PersistCompleted completed:
                        OnPersistCompleted(completed.Block);
                        break;
                    case Blockchain.RelayResult rr:
                        if (rr.Result == VerifyResult.Succeed && rr.Inventory is ExtensiblePayload payload && payload.Category == "dBFT")
                            OnConsensusPayload(payload);
                        break;
                }
            }
        }

        private void RequestRecovery()
        {
            Log($"Sending {nameof(RecoveryRequest)}: height={context.Block.Index} view={context.ViewNumber} nc={context.CountCommitted} nf={context.CountFailed}");
            localNode.Tell(new LocalNode.SendDirectly { Inventory = context.MakeRecoveryRequest() });
        }

        private void OnStart()
        {
            Log("OnStart");
            started = true;
            if (!dbftSettings.IgnoreRecoveryLogs && context.Load())
            {
                if (context.Transactions != null)
                {
                    blockchain.Ask<Blockchain.FillCompleted>(new Blockchain.FillMemoryPool
                    {
                        Transactions = context.Transactions.Values
                    }).Wait();
                }
                if (context.CommitSent)
                {
                    CheckPreparations();
                    return;
                }
            }
            InitializeConsensus(0);
            // Issue a ChangeView with NewViewNumber of 0 to request recovery messages on start-up.
            if (!context.WatchOnly)
                RequestRecovery();
        }

        private void OnTimer(Timer timer)
        {
            if (context.WatchOnly || context.BlockSent) return;
            if (timer.Height != context.Block.Index || timer.ViewNumber != context.ViewNumber) return;
            if (context.IsPrimary && !context.RequestSentOrReceived)
            {
                SendPrepareRequest();
            }
            else if ((context.IsPrimary && context.RequestSentOrReceived) || context.IsBackup)
            {
                if (context.CommitSent)
                {
                    // Re-send commit periodically by sending recover message in case of a network issue.
                    Log($"Sending {nameof(RecoveryMessage)} to resend {nameof(Commit)}");
                    localNode.Tell(new LocalNode.SendDirectly { Inventory = context.MakeRecoveryMessage() });
                    ChangeTimer(TimeSpan.FromMilliseconds(neoSystem.Settings.MillisecondsPerBlock << 1));
                }
                else
                {
                    var reason = ChangeViewReason.Timeout;

                    if (context.Block != null && context.TransactionHashes?.Length > context.Transactions?.Count)
                    {
                        reason = ChangeViewReason.TxNotFound;
                    }

                    RequestChangeView(reason);
                }
            }
        }

        private void OnTransaction(Transaction transaction)
        {
            if (!context.IsBackup || context.NotAcceptingPayloadsDueToViewChanging || !context.RequestSentOrReceived || context.ResponseSent || context.BlockSent)
                return;
            if (context.Transactions.ContainsKey(transaction.Hash)) return;
            if (!context.TransactionHashes.Contains(transaction.Hash)) return;
            AddTransaction(transaction, true);
        }

        protected override void PostStop()
        {
            Log("OnStop");
            started = false;
            Context.System.EventStream.Unsubscribe(Self);
            context.Dispose();
            base.PostStop();
        }

        public static Props Props(NeoSystem neoSystem, Settings dbftSettings, Wallet wallet)
        {
            return Akka.Actor.Props.Create(() => new ConsensusService(neoSystem, dbftSettings, wallet));
        }

        private void RequestChangeView(ChangeViewReason reason)
        {
            if (context.WatchOnly) return;
            // Request for next view is always one view more than the current context.ViewNumber
            // Nodes will not contribute for changing to a view higher than (context.ViewNumber+1), unless they are recovered
            // The latter may happen by nodes in higher views with, at least, `M` proofs
            byte expectedView = context.ViewNumber;
            expectedView++;
            ChangeTimer(TimeSpan.FromMilliseconds(neoSystem.Settings.MillisecondsPerBlock << (expectedView + 1)));
            if ((context.CountCommitted + context.CountFailed) > context.F)
            {
                RequestRecovery();
            }
            else
            {
                Log($"Sending {nameof(ChangeView)}: height={context.Block.Index} view={context.ViewNumber} nv={expectedView} nc={context.CountCommitted} nf={context.CountFailed} reason={reason}");
                localNode.Tell(new LocalNode.SendDirectly { Inventory = context.MakeChangeView(reason) });
                CheckExpectedView(expectedView);
            }
        }

        private bool ReverifyAndProcessPayload(ExtensiblePayload payload)
        {
            RelayResult relayResult = blockchain.Ask<RelayResult>(new Blockchain.Reverify { Inventories = new IInventory[] { payload } }).Result;
            if (relayResult.Result != VerifyResult.Succeed) return false;
            OnConsensusPayload(payload);
            return true;
        }

        private void SendTXListRequest()
        {
            // If there is only one validator, skip this step.
            if (context.Validators.Length == 1)
            {
                SendPrepareRequest();
                CheckPreparations();
                return;
            }
                

            Log($"Sending {nameof(TXListRequest)}: height={context.Block.Index} view={context.ViewNumber}");
            localNode.Tell(new LocalNode.SendDirectly { Inventory = context.MakeTXListRequest() });

       

            if (context.TransactionHashes.Length > 0)
            {
                foreach (InvPayload payload in InvPayload.CreateGroup(InventoryType.TX, context.TransactionHashes))
                    localNode.Tell(Message.Create(MessageCommand.Inv, payload));
            }
            ChangeTimer(TimeSpan.FromMilliseconds((neoSystem.Settings.MillisecondsPerBlock << (context.ViewNumber + 1)) - (context.ViewNumber == 0 ? neoSystem.Settings.MillisecondsPerBlock : 0)));

        }

        private void SendPrepareRequest()
        {
            Log($"Sending {nameof(PrepareRequest)}: height={context.Block.Index} view={context.ViewNumber}");
            localNode.Tell(new LocalNode.SendDirectly { Inventory = context.MakePrepareRequest() });

            if (context.Validators.Length == 1)
                CheckPreparations();

            if (context.TransactionHashes.Length > 0)
            {
                foreach (InvPayload payload in InvPayload.CreateGroup(InventoryType.TX, context.TransactionHashes))
                    localNode.Tell(Message.Create(MessageCommand.Inv, payload));
            }
            ChangeTimer(TimeSpan.FromMilliseconds((neoSystem.Settings.MillisecondsPerBlock << (context.ViewNumber + 1)) - (context.ViewNumber == 0 ? neoSystem.Settings.MillisecondsPerBlock : 0)));
        }
    }
}
