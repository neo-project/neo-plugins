using Neo.Ledger;
using Neo.Network.P2P.Payloads;
using Neo.Persistence;
using System.Collections.Generic;

namespace Neo.Plugins
{
    internal static class BlockHelper
    {
        /// <summary>
        /// Returns the time the given block was active
        /// </summary>
        /// <param name="block">
        /// The given block to verify the time
        /// </param>
        /// <returns>
        /// Returns 0 if <paramref name="block"/> is the current block or it is the genesis block or it is null;
        /// otherwise, returns the time the block was active in milliseconds
        /// </returns>
        public static ulong GetTime(this Block block)
        {
            ulong time = 0;

            if (block != null && block.Index > 0 && block.Index < Blockchain.Singleton.Height)
            {
                var nextBlock = Blockchain.Singleton.GetBlock(block.Index + 1);

                if (nextBlock != null)
                {
                    time = nextBlock.Timestamp - block.Timestamp;
                }
            }

            return time;
        }

        /// <summary>
        /// Returns the average time the latest blocks are active
        /// </summary>
        /// <param name="desiredCount">
        /// The desired number of blocks that should be checked to calculate the average time
        /// </param>
        /// <returns>
        /// Returns the average time per block in milliseconds if the number of analysed blocks
        /// is greater than zero; otherwise, returns 0.0
        /// </returns>
        public static double GetAverageTimePerBlock(this SnapshotView snapshot, uint desiredCount)
        {
            var firstIndex = Blockchain.GenesisBlock.Index;
            var blockHash = snapshot.CurrentBlockHash;

            var countedBlocks = -1;
            Block block = snapshot.GetBlock(blockHash);
            ulong totaltime = 0;

            do
            {
                totaltime += block.GetTime();
                block = snapshot.GetBlock(block.PrevHash);
                countedBlocks++;
            } while (block != null && block.Index != firstIndex && desiredCount > countedBlocks);

            double averageTime = 0.0;
            if (countedBlocks > 0)
            {
                averageTime = 1.0 * totaltime / countedBlocks;
            }

            return averageTime;
        }

        /// <summary>
        /// Returns the block timestamp for each of the n latest blocks.
        /// </summary>
        /// <param name="desiredCount">
        /// The desired number of blocks that should map the timestamp
        /// </param>
        /// <param name="lastHeight">
        /// The current height of the blockchain
        /// </param>
        /// <returns>
        /// Returns a dictionary that maps each block index with its timestamp if the number of blocks
        /// is greater than zero; otherwise, returns an empty dictionary.
        /// </returns>
        public static Dictionary<uint, ulong> GetBlocksTimestamp(this SnapshotView snapshot, uint desiredCount, Block lastBlock = null)
        {
            var dictionary = new Dictionary<uint, ulong>();
            var firstIndex = Blockchain.GenesisBlock.Index;
            var blockHash = snapshot.CurrentBlockHash;

            var countedBlocks = 0;
            Block block;

            if (lastBlock != null && lastBlock.Index < snapshot.Height)
            {
                block = lastBlock;
            }
            else
            {
                block = snapshot.GetBlock(blockHash);
            }

            while (block != null && block.Index != firstIndex && desiredCount > countedBlocks)
            {
                dictionary.Add(block.Index, block.Timestamp);

                block = snapshot.GetBlock(block.PrevHash);
                countedBlocks++;
            }

            return dictionary;
        }
    }
}
