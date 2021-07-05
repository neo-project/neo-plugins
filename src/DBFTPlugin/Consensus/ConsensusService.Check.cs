using Akka.Actor;
using Neo.IO;
using Neo.Network.P2P;
using Neo.Network.P2P.Payloads;
using System;
using System.Linq;
namespace Neo.Consensus
{
    partial class ConsensusService
    {

        private bool CheckTXLists(TXListMessage message)
        {
            // If the all the tranactions in the list is confirmed, then mark the list as valid.
            if(message.TransactionHashes.Length == context.TXListVerification[message.ValidatorIndex].Length)
            {
                context.ValidTXListPayloads[message.ValidatorIndex] = context.TXListPayloads[message.ValidatorIndex];
            }

            if (context.ValidTXListPayloads.Count(p => context.GetMessage(p)?.ViewNumber == context.ViewNumber) > context.F)
            {

                /// TODO: Generate the final TXList based on the valid TXList Payloads.
                foreach (ExtensiblePayload payload in context.ValidTXListPayloads)
                {
                    TXListMessage response = (TXListMessage)context.GetMessage(payload);

                    var transactions = context.ValidTXListPayloads
                        .Where(p => p != null)
                        .Select(p => context.GetMessage<TXListMessage>(p).TransactionHashes)
                        .Cast<UInt256>()
                        .ToList()
                        .Distinct()
                        .Where(p => context.Transactions.ContainsKey(p))
                        .Select(p => context.Transactions[p])
                        .OrderByDescending(p => p.NetworkFee + p.SystemFee)
                        .ToArray();


                    context.MakePrepareRequest(transactions);
                }

                    return true;
            }

            return false;
        }

        private bool CheckPrepareResponse()
        {
            if (context.TransactionHashes.Length == context.Transactions.Count)
            {
                // if we are the primary for this view, but acting as a backup because we recovered our own
                // previously sent prepare request, then we don't want to send a prepare response.
                if (context.IsPrimary || context.WatchOnly) return true;

                // Check maximum block size via Native Contract policy
                if (context.GetExpectedBlockSize() > dbftSettings.MaxBlockSize)
                {
                    Log($"Rejected block: {context.Block.Index} The size exceed the policy", LogLevel.Warning);
                    RequestChangeView(ChangeViewReason.BlockRejectedByPolicy);
                    return false;
                }
                // Check maximum block system fee via Native Contract policy
                if (context.GetExpectedBlockSystemFee() > dbftSettings.MaxBlockSystemFee)
                {
                    Log($"Rejected block: {context.Block.Index} The system fee exceed the policy", LogLevel.Warning);
                    RequestChangeView(ChangeViewReason.BlockRejectedByPolicy);
                    return false;
                }

                // Timeout extension due to prepare response sent
                // around 2*15/M=30.0/5 ~ 40% block time (for M=5)
                ExtendTimerByFactor(2);

                Log($"Sending {nameof(PrepareResponse)}");
                localNode.Tell(new LocalNode.SendDirectly { Inventory = context.MakePrepareResponse() });
                CheckPreparations();
            }
            return true;
        }

        private void CheckCommits()
        {
            if (context.CommitPayloads.Count(p => context.GetMessage(p)?.ViewNumber == context.ViewNumber) >= context.M && context.TransactionHashes.All(p => context.Transactions.ContainsKey(p)))
            {
                block_received_index = context.Block.Index;
                block_received_time = TimeProvider.Current.UtcNow;
                Block block = context.CreateBlock();
                Log($"Sending {nameof(Block)}: height={block.Index} hash={block.Hash} tx={block.Transactions.Length}");
                blockchain.Tell(block);
            }
        }

        private void CheckExpectedView(byte viewNumber)
        {
            if (context.ViewNumber >= viewNumber) return;
            var messages = context.ChangeViewPayloads.Select(p => context.GetMessage<ChangeView>(p)).ToArray();
            // if there are `M` change view payloads with NewViewNumber greater than viewNumber, then, it is safe to move
            if (messages.Count(p => p != null && p.NewViewNumber >= viewNumber) >= context.M)
            {
                if (!context.WatchOnly)
                {
                    ChangeView message = messages[context.MyIndex];
                    // Communicate the network about my agreement to move to `viewNumber`
                    // if my last change view payload, `message`, has NewViewNumber lower than current view to change
                    if (message is null || message.NewViewNumber < viewNumber)
                        localNode.Tell(new LocalNode.SendDirectly { Inventory = context.MakeChangeView(ChangeViewReason.ChangeAgreement) });
                }
                InitializeConsensus(viewNumber);
            }
        }

        private void CheckPreparations()
        {
            if (context.PreparationPayloads.Count(p => p != null) >= context.M && context.TransactionHashes.All(p => context.Transactions.ContainsKey(p)))
            {
                ExtensiblePayload payload = context.MakeCommit();
                Log($"Sending {nameof(Commit)}");
                context.Save();
                localNode.Tell(new LocalNode.SendDirectly { Inventory = payload });
                // Set timer, so we will resend the commit in case of a networking issue
                ChangeTimer(TimeSpan.FromMilliseconds(neoSystem.Settings.MillisecondsPerBlock));
                CheckCommits();
            }
        }
    }
}
