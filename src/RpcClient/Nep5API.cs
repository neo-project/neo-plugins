using Neo.Cryptography.ECC;
using Neo.Network.P2P.Payloads;
using Neo.Network.RPC.Models;
using Neo.SmartContract;
using Neo.VM;
using Neo.Wallets;
using System;
using System.Linq;
using System.Numerics;
using System.Threading.Tasks;
using static Neo.Helper;

namespace Neo.Network.RPC
{
    /// <summary>
    /// Call NEP5 methods with RPC API
    /// </summary>
    public class Nep5API : ContractClient
    {
        /// <summary>
        /// Nep5API Constructor
        /// </summary>
        /// <param name="rpcClient">the RPC client to call NEO RPC methods</param>
        public Nep5API(RpcClient rpcClient) : base(rpcClient) { }

        /// <summary>
        /// Get balance of NEP5 token
        /// </summary>
        /// <param name="scriptHash">contract script hash</param>
        /// <param name="account">account script hash</param>
        /// <returns></returns>
        public async Task<BigInteger> BalanceOf(UInt160 scriptHash, UInt160 account)
        {
            var result = await TestInvoke(scriptHash, "balanceOf", account).ConfigureAwait(false);
            BigInteger balance = result.Stack.Single().GetInteger();
            return balance;
        }

        /// <summary>
        /// Get name of NEP5 token
        /// </summary>
        /// <param name="scriptHash">contract script hash</param>
        /// <returns></returns>
        public async Task<string> Name(UInt160 scriptHash)
        {
            var result = await TestInvoke(scriptHash, "name").ConfigureAwait(false);
            return result.Stack.Single().GetString();
        }

        /// <summary>
        /// Get symbol of NEP5 token
        /// </summary>
        /// <param name="scriptHash">contract script hash</param>
        /// <returns></returns>
        public async Task<string> Symbol(UInt160 scriptHash)
        {
            var result = await TestInvoke(scriptHash, "symbol").ConfigureAwait(false);
            return result.Stack.Single().GetString();
        }

        /// <summary>
        /// Get decimals of NEP5 token
        /// </summary>
        /// <param name="scriptHash">contract script hash</param>
        /// <returns></returns>
        public async Task<byte> Decimals(UInt160 scriptHash)
        {
            var result = await TestInvoke(scriptHash, "decimals").ConfigureAwait(false);
            return (byte)result.Stack.Single().GetInteger();
        }

        /// <summary>
        /// Get total supply of NEP5 token
        /// </summary>
        /// <param name="scriptHash">contract script hash</param>
        /// <returns></returns>
        public async Task<BigInteger> TotalSupply(UInt160 scriptHash)
        {
            var result = await TestInvoke(scriptHash, "totalSupply").ConfigureAwait(false);
            return result.Stack.Single().GetInteger();
        }

        /// <summary>
        /// Get token information in one rpc call
        /// </summary>
        /// <param name="scriptHash">contract script hash</param>
        /// <returns></returns>
        public async Task<RpcNep5TokenInfo> GetTokenInfo(UInt160 scriptHash)
        {
            byte[] script = Concat(scriptHash.MakeScript("name"),
                scriptHash.MakeScript("symbol"),
                scriptHash.MakeScript("decimals"),
                scriptHash.MakeScript("totalSupply"));

            var result = await TestInvoke(scriptHash, "totalSupply").ConfigureAwait(false);
            var stack = result.Stack;

            return new RpcNep5TokenInfo
            {
                Name = stack[0].GetString(),
                Symbol = stack[1].GetString(),
                Decimals = (byte)stack[2].GetInteger(),
                TotalSupply = stack[3].GetInteger()
            };
        }

        /// <summary>
        /// Create NEP5 token transfer transaction
        /// </summary>
        /// <param name="scriptHash">contract script hash</param>
        /// <param name="fromKey">from KeyPair</param>
        /// <param name="to">to account script hash</param>
        /// <param name="amount">transfer amount</param>
        /// <returns></returns>
        public async Task<Transaction> CreateTransferTx(UInt160 scriptHash, KeyPair fromKey, UInt160 to, BigInteger amount)
        {
            var sender = Contract.CreateSignatureRedeemScript(fromKey.PublicKey).ToScriptHash();
            Signer[] signers = new[] { new Signer { Scopes = WitnessScope.CalledByEntry, Account = sender } };

            byte[] script = scriptHash.MakeScript("transfer", sender, to, amount);
            var tx = await rpcClient.MakeTransaction(script, signers)
                .AddSignature(fromKey)
                .SignAsync().ConfigureAwait(false);

            return tx;
        }

        /// <summary>
        /// Create NEP5 token transfer transaction from multi-sig account
        /// </summary>
        /// <param name="scriptHash">contract script hash</param>
        /// <param name="m">multi-sig min signature count</param>
        /// <param name="pubKeys">multi-sig pubKeys</param>
        /// <param name="fromKeys">sign keys</param>
        /// <param name="to">to account</param>
        /// <param name="amount">transfer amount</param>
        /// <returns></returns>
        public async Task<Transaction> CreateTransferTx(UInt160 scriptHash, int m, ECPoint[] pubKeys, KeyPair[] fromKeys, UInt160 to, BigInteger amount)
        {
            if (m > fromKeys.Length)
                throw new ArgumentException($"Need at least {m} KeyPairs for signing!");
            var sender = Contract.CreateMultiSigContract(m, pubKeys).ScriptHash;
            Signer[] signers = new[] { new Signer { Scopes = WitnessScope.CalledByEntry, Account = sender } };

            byte[] script = scriptHash.MakeScript("transfer", sender, to, amount);

            Transaction tx = await rpcClient.MakeTransaction(script, signers)
                .AddMultiSig(fromKeys, m, pubKeys)
                .SignAsync().ConfigureAwait(false);

            return tx;
        }
    }
}
