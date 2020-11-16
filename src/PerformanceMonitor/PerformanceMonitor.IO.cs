using Neo.ConsoleService;
using Neo.IO.Json;
using Neo.Ledger;
using Neo.Network.P2P;
using Neo.Network.P2P.Payloads;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace Neo.Plugins
{
    partial class PerformanceMonitor
    {
        /// <summary>
        /// Process "block sync" command
        /// </summary>
        [ConsoleCommand("block sync", Category = "Block Commands", Description = "Show the delay in seconds in the synchronization of the blocks in the network.")]
        private void OnBlockSynchronizationCommand()
        {
            var lastBlockRemote = GetMaxRemoteBlockCount();

            if (lastBlockRemote == 0)
            {
                Console.WriteLine("There are no remote nodes to synchronize the local chain");
            }
            else
            {
                var delayInMilliseconds = GetBlockSynchronizationDelay(true);
                if (delayInMilliseconds <= 0)
                {
                    Console.WriteLine("The time to confirm a new block has timed out.");
                }
                else
                {
                    string time;
                    if (delayInMilliseconds < 1000)
                    {
                        time = $"{delayInMilliseconds:0.#} ms";
                    }
                    else
                    {
                        time = $"{(delayInMilliseconds / 1000.0):0.#} sec";
                    }
                    Console.WriteLine($"Time to synchronize to the last remote block: {time}");
                }
            }
        }

        /// <summary>
        /// Gets the delay in the synchronization of the blocks between the
        /// connected nodes
        /// </summary>
        /// <returns>
        /// Returns the delay in the synchronization between the local and
        /// the remote nodes in milliseconds
        /// </returns>
        [RpcMethod]
        public JObject GetBlockSyncTime(JArray _params)
        {
            if (_params.Count != 0)
            {
                throw new RpcException(-32602, "Invalid params");
            }
            var lastBlockRemote = GetMaxRemoteBlockCount();
            if (lastBlockRemote <= 0)
            {
                throw new RpcException(-100, "There are no connected nodes");
            }

            var delayInMilliseconds = GetBlockSynchronizationDelay();
            if (delayInMilliseconds <= 0)
            {
                throw new RpcException(-100, "TimeOut");
            }

            return delayInMilliseconds;
        }

        /// <summary>
        /// Calculates the delay in the synchronization of the blocks between the connected nodes
        /// </summary>
        /// <param name="printMessages">
        /// Specifies if the messages should be printed in the console.
        /// </param>
        /// <returns>
        /// If the number of remote nodes is greater than zero, returns the delay in the
        /// synchronization between the local and the remote nodes in milliseconds; otherwise,
        /// returns zero.
        /// </returns>
        private double GetBlockSynchronizationDelay(bool printMessages = false)
        {
            var cancel = new CancellationTokenSource();
            var timeLimitInMilliseconds = 60 * 1000; // limit the waiting time to 1 minute

            var lastBlockRemote = GetMaxRemoteBlockCount();
            if (lastBlockRemote == 0)
            {
                return 0;
            }

            var lastBlock = Math.Max(lastBlockRemote, Blockchain.Singleton.Height);

            bool showBlock = printMessages;
            DateTime remote = DateTime.UtcNow;
            DateTime local = remote;

            Task monitorRemote = new Task(() =>
            {
                var lastRemoteBlockIndex = WaitPersistedBlock(lastBlock, cancel.Token);
                remote = DateTime.UtcNow;
                if (showBlock && lastRemoteBlockIndex > lastBlock)
                {
                    showBlock = false;
                    Console.WriteLine($"Updated block index to {lastRemoteBlockIndex}");
                    SendBlockchainPingMessage();
                }
            }, cancel.Token);

            Task monitorLocal = new Task(() =>
            {
                var lastPersistedBlockIndex = WaitRemoteBlock(lastBlock, cancel.Token);
                local = DateTime.UtcNow;
                if (showBlock && lastPersistedBlockIndex > lastBlock)
                {
                    showBlock = false;
                    Console.WriteLine($"Updated block index to {lastPersistedBlockIndex}");
                    SendBlockchainPingMessage();
                }
            }, cancel.Token);

            Task timer = new Task(async () =>
            {
                try
                {
                    await Task.Delay(timeLimitInMilliseconds, cancel.Token);
                    cancel.Cancel();
                }
                catch (OperationCanceledException) { }
            }, cancel.Token);

            if (printMessages)
            {
                Console.WriteLine($"Current block index is {lastBlock}");
                Console.WriteLine("Waiting for the next block...");
            }

            Task[] tasks = new Task[]
            {
                monitorRemote, monitorLocal
            };

            monitorRemote.Start();
            monitorLocal.Start();
            timer.Start();

            try
            {
                Task.WaitAll(tasks, cancel.Token);
                cancel.Cancel();
            }
            catch (OperationCanceledException)
            {
                return 0;
            }

            var delay = remote - local;

            return Math.Abs(delay.TotalMilliseconds);
        }

        /// <summary>
        /// Pauses the current thread until a new block is persisted in the blockchain
        /// </summary>
        /// <param name="blockIndex">
        /// Specifies if the block index to start monitoring.
        /// </param>
        /// <param name="token">
        /// A cancellation token to stop the task if the caller is canceled
        /// </param>
        /// <returns>
        /// If the <paramref name="token"/> is canceled, returns <param name="blockIndex">;
        /// otherwise, returns the index of the persisted block.
        /// </returns>
        private uint WaitPersistedBlock(uint blockIndex, CancellationToken token)
        {
            var persistedBlockIndex = blockIndex;
            var updatePersistedBlock = new TaskCompletionSource<bool>();

            CommitHandler commit = (snapshot) =>
            {
                if (snapshot.Height > blockIndex)
                {
                    persistedBlockIndex = snapshot.Height;
                    updatePersistedBlock.TrySetResult(true);
                }
            };

            OnCommitEvent += commit;
            try
            {
                updatePersistedBlock.Task.Wait(token);
            }
            catch (OperationCanceledException) { }
            OnCommitEvent -= commit;

            return persistedBlockIndex;
        }

        /// <summary>
        /// Pauses the current thread until a new block is received from remote nodes
        /// </summary>
        /// <param name="blockIndex">
        /// Specifies if the block index to start monitoring.
        /// </param>
        /// <param name="token">
        /// A cancellation token to stop the task if the caller is canceled
        /// </param>
        /// <returns>
        /// If the <paramref name="token"/> is canceled, returns <param name="blockIndex">;
        /// otherwise, returns the index of the received block.
        /// </returns>
        private uint WaitRemoteBlock(uint blockIndex, CancellationToken token)
        {
            var updateRemoteBlock = new TaskCompletionSource<bool>();

            var stopBroadcast = new CancellationTokenSource();

            Task broadcast = Task.Run(() =>
            {
                while (!stopBroadcast.Token.IsCancellationRequested)
                {
                    // receive a PingPayload is what updates RemoteNode LastBlockIndex
                    SendBlockchainPingMessage();
                    Thread.Sleep(1000);
                }
            });


            P2PMessageHandler p2pMessage = (message) =>
            {
                if (message.Command == MessageCommand.Pong && message.Payload is PingPayload)
                {
                    var lastBlockIndex = GetMaxRemoteBlockCount();
                    if (lastBlockIndex > blockIndex && !token.IsCancellationRequested)
                    {
                        blockIndex = lastBlockIndex;
                        updateRemoteBlock.TrySetResult(true);
                    }
                }
            };

            OnP2PMessageEvent += p2pMessage;

            // receive a PingPayload is what updates RemoteNode LastBlockIndex
            SendBlockchainPingMessage();
            try
            {
                updateRemoteBlock.Task.Wait(token);
            }
            catch (OperationCanceledException) { }
            stopBroadcast.Cancel();

            OnP2PMessageEvent -= p2pMessage;

            return blockIndex;
        }

        /// <summary>
        /// Gets the block count of the remote node with the highest height
        /// </summary>
        /// <returns>
        /// If the number of remote nodes is greater than zero, returns block count of the
        /// node with the highest height; otherwise returns zero.
        /// </returns>
        private uint GetMaxRemoteBlockCount()
        {
            var remotes = LocalNode.Singleton.GetRemoteNodes();

            uint maxCount = 0;

            foreach (var node in remotes)
            {
                if (node.LastBlockIndex > maxCount)
                {
                    maxCount = node.LastBlockIndex;
                }
            }

            return maxCount;
        }

        /// <summary>
        /// Process "transaction size" command
        /// Prints the size of the transaction in bytes identified by its hash
        /// </summary>
        [ConsoleCommand("tx size", Category = "Transaction Commands", Description = "Show the size of the transaction in bytes identified by its hash.")]
        private void OnTransactionSizeCommand(UInt256 transactionHash)
        {
            using (var snapshot = Blockchain.Singleton.GetSnapshot())
            {
                Transaction tx = snapshot.GetTransaction(transactionHash);

                if (tx == null)
                {
                    Console.WriteLine("Transaction not found");
                }
                else
                {
                    var size = GetSize(tx);
                    Console.WriteLine($"Transaction Hash: {tx.Hash}");
                    Console.WriteLine($"            Size: {size} bytes");
                }
            }
        }

        /// <summary>
        /// Gets the size in bytes of the transaction identified by its hash
        /// </summary>
        /// Returns the size of the transaction in bytes
        /// </returns>
        [RpcMethod]
        public JObject GetTxSize(JArray _params)
        {
            if (_params.Count != 1 || !UInt256.TryParse(_params[0].AsString(), out var txHash))
            {
                throw new RpcException(-32602, "Invalid params");
            }

            using (var snapshot = Blockchain.Singleton.GetSnapshot())
            {
                Transaction tx = snapshot.GetTransaction(txHash);

                if (tx is null)
                    throw new RpcException(-100, "Transaction not found");

                return GetSize(tx);
            }
        }

        /// <summary>
        /// Returns the time the given block was active
        /// </summary>
        /// <param name="tx">
        /// The given tx to verify the size
        /// </param>
        /// <returns>
        /// Returns 0 if <paramref name="tx"/> is is null; otherwise,
        /// returns the size of the transaction in bytes
        /// </returns>
        public int GetSize(Transaction tx)
        {
            if (tx != null)
            {
                return tx.Size;
            }

            return 0;
        }

        /// <summary>
        /// Process "transaction avgsize" command
        /// Prints the average size in bytes of the latest transactions
        /// </summary>
        [ConsoleCommand("tx avgsize", Category = "Transaction Commands", Description = "Show the average size in bytes of the latest transactions.")]
        private void OnTransactionAverageSizeCommand(uint txCount = 1000)
        {
            uint desiredCount = txCount;

            if (desiredCount < 1)
            {
                Console.WriteLine("Minimum 1 transaction");
                return;
            }

            if (desiredCount > 10000)
            {
                Console.WriteLine("Maximum 10000 transactions");
                return;
            }

            var averageInBytes = GetSizePerTransaction(desiredCount);
            Console.WriteLine(averageInBytes.ToString("Average size/tx: 0 bytes"));
        }

        /// <summary>
        /// Gets the average size of the latest transactions in bytes
        /// </summary>
        /// <returns>
        /// Returns the average size per transaction in bytes
        /// </returns>
        [RpcMethod]
        public JObject GetTxAvgSize(JArray _params)
        {
            if (_params.Count > 1)
            {
                throw new RpcException(-32602, "Invalid params");
            }
            uint desiredCount = 1000;
            if (_params.Count > 0)
            {
                if (!uint.TryParse(_params[0].AsString(), out desiredCount))
                {
                    throw new RpcException(-32602, "Invalid params");
                }

                if (desiredCount < 1)
                {
                    throw new RpcException(-100, "Minimum 1 transaction");
                }

                if (desiredCount > 10000)
                {
                    throw new RpcException(-100, "Maximum 10000 transaction");
                }
            }

            using (var snapshot = Blockchain.Singleton.GetSnapshot())
            {
                return GetSizePerTransaction(desiredCount);
            }
        }

        /// <summary>
        /// Returns the average size of the latest transactions
        /// </summary>
        /// <param name="desiredCount">
        /// The desired number of transactions that should be checked to calculate the average size
        /// </param>
        /// <returns>
        /// Returns the average size per transaction in bytes if the number of analysed transactions
        /// is greater than zero; otherwise, returns 0.0
        /// </returns>
        private double GetSizePerTransaction(uint desiredCount)
        {
            using (var snapshot = Blockchain.Singleton.GetSnapshot())
            {
                var firstIndex = Blockchain.GenesisBlock.Index;
                var blockHash = snapshot.CurrentBlockHash;
                var countedTxs = 0;

                Block block = snapshot.GetBlock(blockHash);
                int totalsize = 0;

                if (block.Index == firstIndex)
                {
                    return totalsize;
                }

                do
                {
                    foreach (var tx in block.Transactions)
                    {
                        if (tx != null)
                        {
                            totalsize += tx.Size;
                            countedTxs++;

                            if (desiredCount <= countedTxs)
                            {
                                break;
                            }
                        }
                    }

                    block = snapshot.GetBlock(block.PrevHash);
                } while (block != null && block.Index != firstIndex && desiredCount > countedTxs);

                double averageSize = 0.0;
                if (countedTxs > 0)
                {
                    averageSize = 1.0 * totalsize / countedTxs;
                }

                return averageSize;
            }
        }
    }
}
