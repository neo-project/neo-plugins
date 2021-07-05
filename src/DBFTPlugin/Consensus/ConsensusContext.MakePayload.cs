using Neo.Ledger;
using Neo.Network.P2P.Payloads;
using Neo.SmartContract;
using Neo.Wallets;
using System;
using System.Collections.Generic;
using System.Linq;
using static Neo.Consensus.RecoveryMessage;

namespace Neo.Consensus
{
    public partial class ConsensusContext
    {

        public ExtensiblePayload MakeChangeView(ChangeViewReason reason)
        {
            return ChangeViewPayloads[MyIndex] = MakeSignedPayload(new ChangeView
            {
                Reason = reason,
                Timestamp = TimeProvider.Current.UtcNow.ToTimestampMS()
            });
        }

        public ExtensiblePayload MakeCommit()
        {
            return CommitPayloads[MyIndex] ?? (CommitPayloads[MyIndex] = MakeSignedPayload(new Commit
            {
                Signature = EnsureHeader().Sign(keyPair, neoSystem.Settings.Network)
            }));
        }

        private ExtensiblePayload MakeSignedPayload(ConsensusMessage message)
        {
            message.BlockIndex = Block.Index;
            message.ValidatorIndex = (byte)MyIndex;
            message.ViewNumber = ViewNumber;
            ExtensiblePayload payload = CreatePayload(message, null);
            SignPayload(payload);
            return payload;
        }

        private void SignPayload(ExtensiblePayload payload)
        {
            ContractParametersContext sc;
            try
            {
                sc = new ContractParametersContext(neoSystem.StoreView, payload, dbftSettings.Network);
                wallet.Sign(sc);
            }
            catch (InvalidOperationException)
            {
                return;
            }
            payload.Witness = sc.GetWitnesses()[0];
        }

        /// <summary>
        /// Prevent that block exceed the max size
        /// </summary>
        /// <param name="txs">Ordered transactions</param>
        internal void EnsureMaxBlockLimitation(IEnumerable<Transaction> txs)
        {
            uint maxTransactionsPerBlock = neoSystem.Settings.MaxTransactionsPerBlock;

            // Limit Speaker proposal to the limit `MaxTransactionsPerBlock` or all available transactions of the mempool
            txs = txs.Take((int)maxTransactionsPerBlock);
            List<UInt256> hashes = new List<UInt256>();
            Transactions = new Dictionary<UInt256, Transaction>();
            VerificationContext = new TransactionVerificationContext();

            // Expected block size
            var blockSize = GetExpectedBlockSizeWithoutTransactions(txs.Count());
            var blockSystemFee = 0L;

            // Iterate transaction until reach the size or maximum system fee
            foreach (Transaction tx in txs)
            {
                // Check if maximum block size has been already exceeded with the current selected set
                blockSize += tx.Size;
                if (blockSize > dbftSettings.MaxBlockSize) break;

                // Check if maximum block system fee has been already exceeded with the current selected set
                blockSystemFee += tx.SystemFee;
                if (blockSystemFee > dbftSettings.MaxBlockSystemFee) break;

                hashes.Add(tx.Hash);
                Transactions.Add(tx.Hash, tx);
                VerificationContext.AddTransaction(tx);
            }

            TransactionHashes = hashes.ToArray();
        }

        public ExtensiblePayload MakePrepareRequest(Transaction[] transactions)
        {
            // if in single node mode, get transactoin from mempool
            if(transactions == null && Validators.Length == 1)
                EnsureMaxBlockLimitation(neoSystem.MemPool.GetSortedVerifiedTransactions());

            Block.Header.Timestamp = Math.Max(TimeProvider.Current.UtcNow.ToTimestampMS(), PrevHeader.Timestamp + 1);

            return PreparationPayloads[MyIndex] = MakeSignedPayload(new PrepareRequest
            {
                Version = Block.Version,
                PrevHash = Block.PrevHash,
                Timestamp = Block.Timestamp,
                TransactionHashes = transactions.Select(p => p.Hash).ToArray()
            }) ;
        }

        public ExtensiblePayload MakeRecoveryRequest()
        {
            return MakeSignedPayload(new RecoveryRequest
            {
                Timestamp = TimeProvider.Current.UtcNow.ToTimestampMS()
            });
        }

        public ExtensiblePayload MakeRecoveryMessage()
        {
            PrepareRequest prepareRequestMessage = null;
            if (TransactionHashes != null)
            {
                prepareRequestMessage = new PrepareRequest
                {
                    Version = Block.Version,
                    PrevHash = Block.PrevHash,
                    ViewNumber = ViewNumber,
                    Timestamp = Block.Timestamp,
                    BlockIndex = Block.Index,
                    TransactionHashes = TransactionHashes
                };
            }
            return MakeSignedPayload(new RecoveryMessage
            {
                ChangeViewMessages = LastChangeViewPayloads.Where(p => p != null).Select(p => GetChangeViewPayloadCompact(p)).Take(M).ToDictionary(p => p.ValidatorIndex),
                PrepareRequestMessage = prepareRequestMessage,
                // We only need a PreparationHash set if we don't have the PrepareRequest information.
                PreparationHash = TransactionHashes == null ? PreparationPayloads.Where(p => p != null).GroupBy(p => GetMessage<PrepareResponse>(p).PreparationHash, (k, g) => new { Hash = k, Count = g.Count() }).OrderByDescending(p => p.Count).Select(p => p.Hash).FirstOrDefault() : null,
                PreparationMessages = PreparationPayloads.Where(p => p != null).Select(p => GetPreparationPayloadCompact(p)).ToDictionary(p => p.ValidatorIndex),
                CommitMessages = CommitSent
                    ? CommitPayloads.Where(p => p != null).Select(p => GetCommitPayloadCompact(p)).ToDictionary(p => p.ValidatorIndex)
                    : new Dictionary<byte, CommitPayloadCompact>()
            });
        }

        public ExtensiblePayload MakePrepareResponse()
        {
            return PreparationPayloads[MyIndex] = MakeSignedPayload(new PrepareResponse
            {
                PreparationHash = PreparationPayloads[Block.PrimaryIndex].Hash
            });
        }

        public ExtensiblePayload MakeTXListMessage()
        {
            EnsureMaxBlockLimitation(neoSystem.MemPool.GetSortedVerifiedTransactions());

            return TXListPayloads[MyIndex] = MakeSignedPayload(new TXListMessage
            {
                TransactionHashes = TransactionHashes
            });
        }

        public ExtensiblePayload MakeTXListRequest() {

            return  MakeSignedPayload(new TXListRequest {});
        }
    }
}
