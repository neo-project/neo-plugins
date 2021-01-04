using Microsoft.VisualStudio.TestTools.UnitTesting;
using Akka.TestKit.Xunit2;
using Neo.Network.P2P.Payloads;
using Neo.Ledger;
using Neo.SmartContract.Native;
using Neo.Cryptography.ECC;

namespace Neo.Plugins.Tests
{
    [TestClass]
    public class UT_OracleService : TestKit
    {
        [TestInitialize]
        public void TestSetup()
        {
            TestBlockchain.InitializeMockNeoSystem();
        }

        [TestMethod]
        public void TestCreateOracleResponseTx()
        {
            var snapshot = Blockchain.Singleton.GetSnapshot();

            var executionFactor = NativeContract.Policy.GetExecFeeFactor(snapshot);
            Assert.AreEqual(executionFactor, (uint)30);
            var feePerByte = NativeContract.Policy.GetFeePerByte(snapshot);
            Assert.AreEqual(feePerByte, (uint)1000);

            OracleRequest request = new OracleRequest
            {
                OriginalTxid = UInt256.Zero,
                GasForResponse = 100000000 * 1,
                Url = "https://127.0.0.1/test",
                Filter = "",
                CallbackContract = UInt160.Zero,
                CallbackMethod = "callback",
                UserData = new byte[0] { }
            };
            snapshot.Transactions.Add(request.OriginalTxid, new TransactionState()
            {
                BlockIndex = 1
            });
            OracleResponse response = new OracleResponse() { Id = 1, Code = OracleResponseCode.Success, Result = new byte[] { 0x00 } };
            ECPoint[] oracleNodes = new ECPoint[] { ECCurve.Secp256r1.G };
            var tx = OracleService.CreateResponseTx(snapshot, request, response, oracleNodes);

            Assert.AreEqual(tx.NetworkFee, 2217850);
            Assert.AreEqual(tx.SystemFee, 97782150);

            // case (2) The size of attribute exceed the maximum limit

            request.GasForResponse = 0_10000000;
            response.Result = new byte[10250];
            tx = OracleService.CreateResponseTx(snapshot, request, response, oracleNodes);
            Assert.AreEqual(response.Code, OracleResponseCode.InsufficientFunds);
            Assert.AreEqual(tx.NetworkFee, 2216850);
            Assert.AreEqual(tx.SystemFee, 7783150);
        }
    }
}
