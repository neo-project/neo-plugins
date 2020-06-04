using Microsoft.AspNetCore.Http;
using Neo.IO.Caching;
using Neo.IO.Data.LevelDB;
using Neo.IO.Json;
using Neo.Ledger;
using Neo.Network.P2P.Payloads;
using Neo.Network.RPC;
using Neo.Persistence;
using Neo.Persistence.LevelDB;
using Neo.Wallets;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Snapshot = Neo.Persistence.Snapshot;

namespace Neo.Plugins
{
    public class RpcSystemAssetTrackerPlugin : Plugin, IPersistencePlugin, IRpcPlugin
    {
        private const byte SystemAssetUnspentCoinsPrefix = 0xfb;
        private const byte SystemAssetSpentUnclaimedCoinsPrefix = 0xfc;
        private const byte SystemAssetSentPrefix = 0xfd;
        private const byte SystemAssetReceivedPrefix = 0xfe;
        private DB _db;
        private DataCache<UserSystemAssetCoinOutputsKey, UserSystemAssetCoinOutputs> _userUnspentCoins;
        private bool _shouldTrackUnclaimed;
        private DataCache<UserSystemAssetCoinOutputsKey, UserSystemAssetCoinOutputs> _userSpentUnclaimedCoins;
        private bool _shouldTrackHistory;
        private DataCache<UserSystemAssetTransferKey, UserSystemAssetTransfer> _transfersSent;
        private DataCache<UserSystemAssetTransferKey, UserSystemAssetTransfer> _transfersReceived;
        private WriteBatch _writeBatch;
        private int _maxResults;
        private uint _lastPersistedBlock;
        private bool _shouldPersistBlock;
        private Neo.IO.Data.LevelDB.Snapshot _levelDbSnapshot;

        public override void Configure()
        {
            if (_db == null)
            {
                var dbPath = GetConfiguration().GetSection("DBPath").Value ?? "SystemAssetBalanceData";
                _db = DB.Open(Path.GetFullPath(dbPath), new Options { CreateIfMissing = true });
                _shouldTrackUnclaimed = (GetConfiguration().GetSection("TrackUnclaimed").Value ?? true.ToString()) != false.ToString();
                _shouldTrackHistory = (GetConfiguration().GetSection("TrackHistory").Value ?? true.ToString()) != false.ToString();
                try
                {
                    _lastPersistedBlock = _db.Get(ReadOptions.Default, SystemAssetUnspentCoinsPrefix).ToUInt32();
                }
                catch (LevelDBException ex)
                {
                    if (!ex.Message.Contains("not found"))
                        throw;
                    _lastPersistedBlock = 0;
                }
            }
            _maxResults = int.Parse(GetConfiguration().GetSection("MaxResults").Value ?? "0");
        }

        private void ResetBatch()
        {
            _writeBatch = new WriteBatch();
            _levelDbSnapshot?.Dispose();
            _levelDbSnapshot = _db.GetSnapshot();
            var dbOptions = new ReadOptions { FillCache = false, Snapshot = _levelDbSnapshot };
            _userUnspentCoins = new DbCache<UserSystemAssetCoinOutputsKey, UserSystemAssetCoinOutputs>(_db, dbOptions, _writeBatch, SystemAssetUnspentCoinsPrefix);
            if (_shouldTrackUnclaimed)
                _userSpentUnclaimedCoins = new DbCache<UserSystemAssetCoinOutputsKey, UserSystemAssetCoinOutputs>(_db, dbOptions, _writeBatch, SystemAssetSpentUnclaimedCoinsPrefix);
            if (_shouldTrackHistory)
            {
                _transfersSent = new DbCache<UserSystemAssetTransferKey, UserSystemAssetTransfer>(_db, dbOptions, _writeBatch, SystemAssetSentPrefix);
                _transfersReceived = new DbCache<UserSystemAssetTransferKey, UserSystemAssetTransfer>(_db, dbOptions, _writeBatch, SystemAssetReceivedPrefix);
            }
                
        }

        private bool ProcessBlock(Snapshot snapshot, Block block)
        {
            if (block.Transactions.Length <= 1)
            {
                _lastPersistedBlock = block.Index;
                return false;
            }

            ResetBatch();

            var transactionsCache = snapshot.Transactions;
            foreach (Transaction tx in block.Transactions)
            {
                ushort outputIndex = 0;
                foreach (TransactionOutput output in tx.Outputs)
                {
                    bool isGoverningToken = output.AssetId.Equals(Blockchain.GoverningToken.Hash);
                    if (isGoverningToken || output.AssetId.Equals(Blockchain.UtilityToken.Hash))
                    {
                        // Add new unspent UTXOs by account script hash.
                        UserSystemAssetCoinOutputs outputs = _userUnspentCoins.GetAndChange(
                            new UserSystemAssetCoinOutputsKey(isGoverningToken, output.ScriptHash, tx.Hash),
                            () => new UserSystemAssetCoinOutputs());
                        outputs.AddTxIndex(outputIndex, output.Value);
                    }
                    outputIndex++;
                }

                // Iterate all input Transactions by grouping by common input hashes.
                foreach (var group in tx.Inputs.GroupBy(p => p.PrevHash))
                {
                    TransactionState txPrev = transactionsCache[group.Key];
                    // For each input being spent by this transaction.
                    foreach (CoinReference input in group)
                    {
                        // Get the output from the previous transaction that is now being spent.
                        var outPrev = txPrev.Transaction.Outputs[input.PrevIndex];

                        bool isGoverningToken = outPrev.AssetId.Equals(Blockchain.GoverningToken.Hash);
                        if (isGoverningToken || outPrev.AssetId.Equals(Blockchain.UtilityToken.Hash))
                        {
                            // Remove spent UTXOs for unspent outputs by account script hash.
                            var userCoinOutputsKey =
                                new UserSystemAssetCoinOutputsKey(isGoverningToken, outPrev.ScriptHash, input.PrevHash);
                            UserSystemAssetCoinOutputs outputs = _userUnspentCoins.GetAndChange(
                                userCoinOutputsKey, () => new UserSystemAssetCoinOutputs());
                            outputs.RemoveTxIndex(input.PrevIndex);
                            if (outputs.AmountByTxIndex.Count == 0)
                                _userUnspentCoins.Delete(userCoinOutputsKey);

                            if (_shouldTrackUnclaimed && isGoverningToken)
                            {
                                UserSystemAssetCoinOutputs spentUnclaimedOutputs = _userSpentUnclaimedCoins.GetAndChange(
                                    userCoinOutputsKey, () => new UserSystemAssetCoinOutputs());
                                spentUnclaimedOutputs.AddTxIndex(input.PrevIndex, outPrev.Value);
                            }
                        }
                    }
                }

                if (_shouldTrackUnclaimed && tx is ClaimTransaction claimTransaction)
                {
                    foreach (CoinReference input in claimTransaction.Claims)
                    {
                        TransactionState txPrev = transactionsCache[input.PrevHash];
                        var outPrev = txPrev.Transaction.Outputs[input.PrevIndex];

                        var claimedCoinKey =
                            new UserSystemAssetCoinOutputsKey(true, outPrev.ScriptHash, input.PrevHash);
                        UserSystemAssetCoinOutputs spentUnclaimedOutputs = _userSpentUnclaimedCoins.GetAndChange(
                            claimedCoinKey, () => new UserSystemAssetCoinOutputs());
                        spentUnclaimedOutputs.RemoveTxIndex(input.PrevIndex);
                        if (spentUnclaimedOutputs.AmountByTxIndex.Count == 0)
                            _userSpentUnclaimedCoins.Delete(claimedCoinKey);

                        if (snapshot.SpentCoins.TryGet(input.PrevHash)?.Items.Remove(input.PrevIndex) == true)
                            snapshot.SpentCoins.GetAndChange(input.PrevHash);
                    }
                }

                if (_shouldTrackHistory)
                {
                    UInt160 from = UInt160.Zero;
                    // need to get all the neo and gas outputs from the inputs
                    List<TransactionOutput> prevOutputs = new List<TransactionOutput>();
                    foreach (var group in tx.Inputs.GroupBy(p => p.PrevHash))
                    {
                        TransactionState txPrev = transactionsCache[group.Key];
                        foreach (CoinReference input in group)
                        {
                            TransactionOutput outPrev = txPrev.Transaction.Outputs[input.PrevIndex];
                            if (outPrev.AssetId.Equals(Blockchain.GoverningToken.Hash) || outPrev.AssetId.Equals(Blockchain.UtilityToken.Hash))
                                prevOutputs.Add(outPrev);
                        }
                    }

                    Dictionary<UInt256, Fixed8> dic = new Dictionary<UInt256, Fixed8>();
                    if (prevOutputs.Count > 0)
                    {
                        // has neo or gas input, group by asset id, sum each token value
                        from = prevOutputs.First().ScriptHash;
                        dic = prevOutputs.GroupBy(p => p.AssetId).ToDictionary(p => p.Key, p => p.Sum(q => q.Value));
                    }

                    ushort index = 0;
                    foreach (TransactionOutput output in tx.Outputs)
                    {
                        if (output.AssetId.Equals(Blockchain.GoverningToken.Hash) || output.AssetId.Equals(Blockchain.UtilityToken.Hash))
                        {
                            // add transfers except from and to are same
                            if (!from.Equals(output.ScriptHash))
                            {
                                _transfersSent.Add(new UserSystemAssetTransferKey(from, output.AssetId, block.Timestamp, index),
                                    new UserSystemAssetTransfer()
                                    {
                                        UserScriptHash = output.ScriptHash,
                                        BlockIndex = block.Index,
                                        TxHash = tx.Hash,
                                        Amount = output.Value
                                    });
                                _transfersReceived.Add(new UserSystemAssetTransferKey(output.ScriptHash, output.AssetId, block.Timestamp, index),
                                    new UserSystemAssetTransfer
                                    {
                                        UserScriptHash = from,
                                        BlockIndex = block.Index,
                                        TxHash = tx.Hash,
                                        Amount = output.Value
                                    });
                                index++;
                            }
                            // deduct corresponding asset value
                            if (dic.TryGetValue(output.AssetId, out Fixed8 remain))
                            {
                                remain -= output.Value;
                                if (remain <= Fixed8.Zero)
                                    dic.Remove(output.AssetId);
                                else
                                    dic[output.AssetId] = remain;
                            }
                        }
                    }

                    // handle the remainings in the dic
                    foreach (var pair in dic)
                    {
                        if (pair.Value > Fixed8.Zero)
                        {
                            _transfersSent.Add(new UserSystemAssetTransferKey(from, pair.Key, block.Timestamp, index),
                                new UserSystemAssetTransfer()
                                {
                                    UserScriptHash = UInt160.Zero,
                                    BlockIndex = block.Index,
                                    TxHash = tx.Hash,
                                    Amount = pair.Value
                                });
                            _transfersReceived.Add(new UserSystemAssetTransferKey(UInt160.Zero, pair.Key, block.Timestamp, index),
                                new UserSystemAssetTransfer
                                {
                                    UserScriptHash = from,
                                    BlockIndex = block.Index,
                                    TxHash = tx.Hash,
                                    Amount = pair.Value
                                });
                            index++;
                        }
                    }
                }
            }

            // Write the current height into the key of the prefix itself
            _writeBatch.Put(SystemAssetUnspentCoinsPrefix, block.Index);
            _lastPersistedBlock = block.Index;
            return true;
        }

        private void ProcessSkippedBlocks(Snapshot snapshot)
        {
            for (uint blockIndex = _lastPersistedBlock + 1; blockIndex < snapshot.PersistingBlock.Index; blockIndex++)
            {
                var skippedBlock = Blockchain.Singleton.Store.GetBlock(blockIndex);
                if (skippedBlock.Transactions.Length <= 1)
                {
                    _lastPersistedBlock = skippedBlock.Index;
                    continue;
                }

                _shouldPersistBlock = ProcessBlock(snapshot, skippedBlock);
                OnCommit(snapshot);
            }
        }

        public void OnPersist(Snapshot snapshot, IReadOnlyList<Blockchain.ApplicationExecuted> applicationExecutedList)
        {
            if (snapshot.PersistingBlock.Index > _lastPersistedBlock + 1)
                ProcessSkippedBlocks(snapshot);

            _shouldPersistBlock = ProcessBlock(snapshot, snapshot.PersistingBlock);
        }

        public void OnCommit(Snapshot snapshot)
        {
            if (!_shouldPersistBlock) return;
            _userUnspentCoins.Commit();
            if (_shouldTrackUnclaimed) _userSpentUnclaimedCoins.Commit();
            if (_shouldTrackHistory)
            {
                _transfersSent.Commit();
                _transfersReceived.Commit();
            }
            _db.Write(WriteOptions.Default, _writeBatch);
        }

        public bool ShouldThrowExceptionFromCommit(Exception ex)
        {
            return true;
        }

        public void PreProcess(HttpContext context, string method, JArray _params)
        {
        }

        private UInt160 GetScriptHashFromParam(string addressOrScriptHash)
        {
            return addressOrScriptHash.Length < 40 ?
                addressOrScriptHash.ToScriptHash() : UInt160.Parse(addressOrScriptHash);
        }

        private long GetSysFeeAmountForHeight(DataCache<UInt256, BlockState> blocks, uint height)
        {
            return blocks.TryGet(Blockchain.Singleton.GetBlockHash(height)).SystemFeeAmount;
        }

        private void CalculateClaimable(Snapshot snapshot, Fixed8 value, uint startHeight, uint endHeight, out Fixed8 generated, out Fixed8 sysFee)
        {
            uint amount = 0;
            uint ustart = startHeight / Blockchain.DecrementInterval;
            if (ustart < Blockchain.GenerationAmount.Length)
            {
                uint istart = startHeight % Blockchain.DecrementInterval;
                uint uend = endHeight / Blockchain.DecrementInterval;
                uint iend = endHeight % Blockchain.DecrementInterval;
                if (uend >= Blockchain.GenerationAmount.Length)
                {
                    uend = (uint)Blockchain.GenerationAmount.Length;
                    iend = 0;
                }
                if (iend == 0)
                {
                    uend--;
                    iend = Blockchain.DecrementInterval;
                }
                while (ustart < uend)
                {
                    amount += (Blockchain.DecrementInterval - istart) * Blockchain.GenerationAmount[ustart];
                    ustart++;
                    istart = 0;
                }
                amount += (iend - istart) * Blockchain.GenerationAmount[ustart];
            }

            Fixed8 fractionalShare = value / 100000000;
            generated = fractionalShare * amount;
            sysFee = fractionalShare * (GetSysFeeAmountForHeight(snapshot.Blocks, endHeight - 1) -
                     (startHeight == 0 ? 0 : GetSysFeeAmountForHeight(snapshot.Blocks, startHeight - 1)));
        }

        private bool AddClaims(JArray claimableOutput, ref Fixed8 runningTotal, int maxClaims,
            Snapshot snapshot, DataCache<UInt256, SpentCoinState> storeSpentCoins,
            KeyValuePair<UserSystemAssetCoinOutputsKey, UserSystemAssetCoinOutputs> claimableInTx)
        {
            foreach (var claimTransaction in claimableInTx.Value.AmountByTxIndex)
            {
                var utxo = new JObject();
                var txId = claimableInTx.Key.TxHash.ToString().Substring(2);
                utxo["txid"] = txId;
                utxo["n"] = claimTransaction.Key;
                var spentCoinState = storeSpentCoins.TryGet(claimableInTx.Key.TxHash);
                var startHeight = spentCoinState.TransactionHeight;
                var endHeight = spentCoinState.Items[claimTransaction.Key];
                CalculateClaimable(snapshot, claimTransaction.Value, startHeight, endHeight, out var generated,
                    out var sysFee);
                var unclaimed = generated + sysFee;
                utxo["value"] = (double) (decimal) claimTransaction.Value;
                utxo["start_height"] = startHeight;
                utxo["end_height"] = endHeight;
                utxo["generated"] = (double) (decimal) generated;
                utxo["sys_fee"] = (double) (decimal) sysFee;
                utxo["unclaimed"] = (double) (decimal) unclaimed;
                runningTotal += unclaimed;
                claimableOutput.Add(utxo);
                if (claimableOutput.Count > maxClaims)
                    return false;
            }

            return true;
        }

        private JObject ProcessGetClaimableSpents(JArray parameters)
        {
            UInt160 scriptHash = GetScriptHashFromParam(parameters[0].AsString());
            var dbCache = new DbCache<UserSystemAssetCoinOutputsKey, UserSystemAssetCoinOutputs>(
                _db, null, null, SystemAssetSpentUnclaimedCoinsPrefix);

            JObject json = new JObject();
            JArray claimable = new JArray();
            json["claimable"] = claimable;
            json["address"] = scriptHash.ToAddress();

            Fixed8 totalUnclaimed = Fixed8.Zero;
            using (Snapshot snapshot = Blockchain.Singleton.GetSnapshot())
            {
                var storeSpentCoins = snapshot.SpentCoins;
                byte[] prefix = new [] { (byte) 1 }.Concat(scriptHash.ToArray()).ToArray();
                foreach (var claimableInTx in dbCache.Find(prefix))
                    if (!AddClaims(claimable, ref totalUnclaimed, _maxResults, snapshot, storeSpentCoins,
                        claimableInTx))
                        break;
            }
            json["unclaimed"] = (double) (decimal) totalUnclaimed;
            return json;
        }

        private JObject ProcessGetUnclaimed(JArray parameters)
        {
            UInt160 scriptHash = GetScriptHashFromParam(parameters[0].AsString());
            JObject json = new JObject();

            Fixed8 available = Fixed8.Zero;
            Fixed8 unavailable = Fixed8.Zero;
            var spentsCache = new DbCache<UserSystemAssetCoinOutputsKey, UserSystemAssetCoinOutputs>(
                _db, null, null, SystemAssetSpentUnclaimedCoinsPrefix);
            var unspentsCache = new DbCache<UserSystemAssetCoinOutputsKey, UserSystemAssetCoinOutputs>(
                _db, null, null, SystemAssetUnspentCoinsPrefix);
            using (Snapshot snapshot = Blockchain.Singleton.GetSnapshot())
            {
                var storeSpentCoins = snapshot.SpentCoins;
                byte[] prefix = new [] { (byte) 1 }.Concat(scriptHash.ToArray()).ToArray();
                foreach (var claimableInTx in spentsCache.Find(prefix))
                {
                    var spentCoinState = storeSpentCoins.TryGet(claimableInTx.Key.TxHash);
                    foreach (var claimTxIndex in claimableInTx.Value.AmountByTxIndex)
                    {
                        var startHeight = spentCoinState.TransactionHeight;
                        var endHeight = spentCoinState.Items[claimTxIndex.Key];
                        CalculateClaimable(snapshot, claimTxIndex.Value, startHeight, endHeight, out var generated,
                            out var sysFee);
                        available += generated + sysFee;
                    }
                }

                var transactionsCache = snapshot.Transactions;
                foreach (var claimableInTx in unspentsCache.Find(prefix))
                {
                    var transaction = transactionsCache.TryGet(claimableInTx.Key.TxHash);

                    foreach (var claimTxIndex in claimableInTx.Value.AmountByTxIndex)
                    {
                        var startHeight = transaction.BlockIndex;
                        var endHeight = Blockchain.Singleton.Height;
                        CalculateClaimable(snapshot, claimTxIndex.Value, startHeight, endHeight,
                            out var generated,
                            out var sysFee);
                        unavailable += generated + sysFee;
                    }
                }
            }

            json["available"] = (double) (decimal) available;
            json["unavailable"] = (double) (decimal) unavailable;
            json["unclaimed"] = (double) (decimal) (available + unavailable);
            return json;
        }

        private bool AddUnspents(JArray unspents, ref Fixed8 runningTotal,
            KeyValuePair<UserSystemAssetCoinOutputsKey, UserSystemAssetCoinOutputs> unspentInTx)
        {
            var txId = unspentInTx.Key.TxHash.ToString().Substring(2);
            foreach (var unspent in unspentInTx.Value.AmountByTxIndex)
            {
                var utxo = new JObject();
                utxo["txid"] = txId;
                utxo["n"] = unspent.Key;
                utxo["value"] = (double) (decimal) unspent.Value;
                runningTotal += unspent.Value;

                unspents.Add(utxo);
                if (unspents.Count > _maxResults)
                    return false;
            }
            return true;
        }

        private JObject ProcessGetUnspents(JArray _params)
        {
            UInt160 scriptHash = GetScriptHashFromParam(_params[0].AsString());
            byte startingToken = 0; // 0 = Utility Token (GAS), 1 = Governing Token (NEO)
            int maxIterations = 2;

            if (_params.Count > 1)
            {
                maxIterations = 1;
                bool isGoverningToken = _params[1].AsBoolean();
                if (isGoverningToken) startingToken = 1;
            }

            var unspentsCache = new DbCache<UserSystemAssetCoinOutputsKey, UserSystemAssetCoinOutputs>(
                _db, null, null, SystemAssetUnspentCoinsPrefix);

            string[] nativeAssetNames = {"GAS", "NEO"};
            UInt256[] nativeAssetIds = {Blockchain.UtilityToken.Hash, Blockchain.GoverningToken.Hash};

            JObject json = new JObject();
            JArray balances = new JArray();
            json["balance"] = balances;
            json["address"] = scriptHash.ToAddress();
            for (byte tokenIndex = startingToken; maxIterations-- > 0; tokenIndex++)
            {
                byte[] prefix = new [] { tokenIndex }.Concat(scriptHash.ToArray()).ToArray();

                var unspents = new JArray();
                Fixed8 total = new Fixed8(0);

                foreach (var unspentInTx in unspentsCache.Find(prefix))
                    if (!AddUnspents(unspents, ref total, unspentInTx)) break;

                if (unspents.Count <= 0) continue;

                var balance = new JObject();
                balance["unspent"] = unspents;
                balance["asset_hash"] = nativeAssetIds[tokenIndex].ToString().Substring(2);
                balance["asset_symbol"] = balance["asset"] = nativeAssetNames[tokenIndex];
                balance["amount"] = new JNumber((double) (decimal) total); ;
                balances.Add(balance);
            }

            return json;
        }

        private JObject ProcessGetUtxoTransfers(JArray _params)
        {
            UInt160 userScriptHash = GetScriptHashFromParam(_params[0].AsString());
            DateTime now = DateTime.UtcNow;
            DateTime sevenDaysAgo = now - TimeSpan.FromDays(7);
            List<UInt256> tokens = new List<UInt256>() { Blockchain.GoverningToken.Hash, Blockchain.UtilityToken.Hash };
            uint start, end;
            if (_params.Count > 1)
            {
                if (!uint.TryParse(_params[1].AsString(), out start))
                {   // neo or gas
                    if (_params[1].AsString().ToLower() == "neo")
                        tokens.Remove(Blockchain.UtilityToken.Hash);
                    else if (_params[1].AsString().ToLower() == "gas")
                        tokens.Remove(Blockchain.GoverningToken.Hash);
                    else
                        throw new RpcException(-32602, "Invalid params");
                    start = _params.Count > 2 ? (uint)_params[2].AsNumber() : sevenDaysAgo.ToTimestamp();
                    end = _params.Count > 3 ? (uint)_params[3].AsNumber() : now.ToTimestamp();
                }
                else
                {
                    start = (uint)_params[1].AsNumber();
                    end = _params.Count > 2 ? (uint)_params[2].AsNumber() : now.ToTimestamp();
                }
            }
            else
            {
                start = sevenDaysAgo.ToTimestamp();
                end = now.ToTimestamp();
            }
            if (end < start) throw new RpcException(-32602, "Invalid params");

            JObject json = new JObject();
            JArray transfersSent = new JArray();
            json["address"] = userScriptHash.ToAddress();
            json["sent"] = transfersSent;
            JArray transfersReceived = new JArray();
            json["received"] = transfersReceived;
            foreach (var assetId in tokens)
            {
                AddTransfers(SystemAssetSentPrefix, userScriptHash, assetId, start, end, transfersSent);
                AddTransfers(SystemAssetReceivedPrefix, userScriptHash, assetId, start, end, transfersReceived);
            }
            return json;
        }

        private void AddTransfers(byte dbPrefix, UInt160 userScriptHash, UInt256 assetId, uint startTime, uint endTime, JArray parentJArray)
        {
            var prefix = new[] { dbPrefix }.Concat(userScriptHash.ToArray()).Concat(assetId.ToArray()).ToArray();
            var startTimeBytes = BitConverter.GetBytes(startTime);
            var endTimeBytes = BitConverter.GetBytes(endTime);
            if (BitConverter.IsLittleEndian)
            {
                Array.Reverse(startTimeBytes);
                Array.Reverse(endTimeBytes);
            }

            var transferPairs = _db.FindRange<UserSystemAssetTransferKey, UserSystemAssetTransfer>(
                prefix.Concat(startTimeBytes).ToArray(),
                prefix.Concat(endTimeBytes).ToArray());

            Fixed8 sum = Fixed8.Zero;
            JArray transfers = new JArray();
            JObject group = new JObject();
            group["asset_hash"] = assetId.ToString();
            group["asset"] = assetId == Blockchain.GoverningToken.Hash ? "NEO" : assetId == Blockchain.UtilityToken.Hash ? "GAS" : "";
            int resultCount = 0;
            foreach (var pair in transferPairs)
            {
                if (++resultCount > _maxResults) break;
                JObject transfer = new JObject();
                transfer["block_index"] = pair.Value.BlockIndex;
                transfer["timestamp"] = pair.Key.Timestamp;
                transfer["txid"] = pair.Value.TxHash.ToString();
                transfer["transfer_address"] = pair.Value.UserScriptHash.ToString();
                transfer["amount"] = pair.Value.Amount.ToString();
                sum += pair.Value.Amount;
                transfers.Add(transfer);
            }
            group["total_amount"] = sum.ToString();
            group["transactions"] = transfers;

            parentJArray.Add(group);
        }

        public JObject OnProcess(HttpContext context, string method, JArray parameters)
        {
            if (_shouldTrackUnclaimed)
            {
                if (method == "getclaimable") return ProcessGetClaimableSpents(parameters);
                if (method == "getunclaimed") return ProcessGetUnclaimed(parameters);
            }
            if (_shouldTrackHistory)
                if (method == "getutxotransfers") return ProcessGetUtxoTransfers(parameters);
            return method != "getunspents" ? null : ProcessGetUnspents(parameters);
        }

        public void PostProcess(HttpContext context, string method, JArray _params, JObject result)
        {
        }
    }
}
