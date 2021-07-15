using Microsoft.VisualStudio.TestTools.UnitTesting;
using Neo.Cryptography.ECC;
using Neo.FileStorage.Morph.Event;
using Neo.IO;
using System;
using System.Collections.Generic;
using System.Linq;
using static Neo.FileStorage.Morph.Event.MorphEvent;
using Array = Neo.VM.Types.Array;

namespace Neo.FileStorage.Tests.Morph.Event
{
    [TestClass]
    public class UT_MorphEvent
    {
        [TestMethod]
        public void ParseNewEpochEventTest()
        {
            Array array = new Array();
            array.Add((ulong)1);
            IContractEvent @event = NewEpochEvent.ParseNewEpochEvent(array);
            Assert.IsTrue(@event is NewEpochEvent);
            Assert.AreEqual(((NewEpochEvent)@event).EpochNumber, (ulong)1);
            array.Add(1);
            Action action = () => NewEpochEvent.ParseNewEpochEvent(array);
            Assert.ThrowsException<Exception>(action);
            @event.ContractEvent();
        }

        [TestMethod]
        public void ParseAddPeerEventTest()
        {
            Array array = new Array();
            array.Add(new byte[] { 0x01 });
            IContractEvent @event = AddPeerEvent.ParseAddPeerEvent(array);
            Assert.IsTrue(@event is AddPeerEvent);
            Assert.AreEqual(((AddPeerEvent)@event).Node.ToString(), new byte[] { 0x01 }.ToString());
            array.Add(1);
            Action action = () => AddPeerEvent.ParseAddPeerEvent(array);
            Assert.ThrowsException<Exception>(action);
            @event.ContractEvent();
        }

        [TestMethod]
        public void ParseUpdatePeerEventTest()
        {
            Array array = new Array();
            array.Add(1);
            array.Add(TestBlockchain.wallet.GetAccounts().ToArray()[0].GetKey().PublicKey.ToArray());
            IContractEvent @event = UpdatePeerEvent.ParseUpdatePeerEvent(array);
            Assert.IsTrue(@event is UpdatePeerEvent);
            Assert.AreEqual(((UpdatePeerEvent)@event).PublicKey.ToString(), TestBlockchain.wallet.GetAccounts().ToArray()[0].GetKey().PublicKey.ToString());
            Assert.AreEqual(((UpdatePeerEvent)@event).Status, (uint)1);
            array.Add(1);
            Action action = () => UpdatePeerEvent.ParseUpdatePeerEvent(array);
            Assert.ThrowsException<Exception>(action);
            @event.ContractEvent();
        }

        [TestMethod]
        public void ParseLockEventTest()
        {
            Array array = new Array();
            array.Add(new byte[] { 0x01 });
            array.Add(UInt160.Zero.ToArray());
            array.Add(UInt160.Zero.ToArray());
            array.Add(1);
            array.Add(1);
            IContractEvent @event = LockEvent.ParseLockEvent(array);
            Assert.IsTrue(@event is LockEvent);
            Assert.AreEqual(((LockEvent)@event).Id.ToString(), new byte[] { 0x01 }.ToString());
            Assert.AreEqual(((LockEvent)@event).LockAccount, UInt160.Zero);
            Assert.AreEqual(((LockEvent)@event).UserAccount, UInt160.Zero);
            Assert.AreEqual(((LockEvent)@event).Amount, (long)1);
            Assert.AreEqual(((LockEvent)@event).Util, (long)1);
            array.Add(1);
            Action action = () => LockEvent.ParseLockEvent(array);
            Assert.ThrowsException<Exception>(action);
            @event.ContractEvent();
        }

        [TestMethod]
        public void ParseContainerPutEventTest()
        {
            Array array = new Array();
            array.Add(new byte[] { 0x01 });
            array.Add(new byte[] { 0x01 });
            array.Add(TestBlockchain.wallet.GetAccounts().ToArray()[0].GetKey().PublicKey.ToArray());
            array.Add(VM.Types.Null.Null);
            IContractEvent @event = ContainerPutEvent.ParseContainerPutEvent(array);
            Assert.IsTrue(@event is ContainerPutEvent);
            Assert.AreEqual(((ContainerPutEvent)@event).RawContainer.ToString(), new byte[] { 0x01 }.ToString());
            Assert.AreEqual(((ContainerPutEvent)@event).Signature.ToString(), new byte[] { 0x01 }.ToString());
            Assert.AreEqual(((ContainerPutEvent)@event).PublicKey.ToHexString(), TestBlockchain.wallet.GetAccounts().ToArray()[0].GetKey().PublicKey.ToString());
            array.Add(1);
            Action action = () => ContainerPutEvent.ParseContainerPutEvent(array);
            Assert.ThrowsException<Exception>(action);
            @event.ContractEvent();
        }

        [TestMethod]
        public void ParseContainerDeleteEventTest()
        {
            Array array = new Array();
            array.Add(new byte[] { 0x01 });
            array.Add(new byte[] { 0x01 });
            array.Add(VM.Types.Null.Null);
            IContractEvent @event = ContainerDeleteEvent.ParseContainerDeleteEvent(array);
            Assert.IsTrue(@event is ContainerDeleteEvent);
            Assert.AreEqual(((ContainerDeleteEvent)@event).ContainerID.ToHexString(), new byte[] { 0x01 }.ToHexString());
            Assert.AreEqual(((ContainerDeleteEvent)@event).Signature.ToHexString(), new byte[] { 0x01 }.ToHexString());
            array.Add(1);
            Action action = () => ContainerDeleteEvent.ParseContainerDeleteEvent(array);
            Assert.ThrowsException<Exception>(action);
            @event.ContractEvent();
        }

        [TestMethod]
        public void ParseBindEventTest()
        {
            List<ECPoint> keys = TestBlockchain.wallet.GetAccounts().ToArray().Select(p => p.GetKey().PublicKey).ToList();
            Array ecpints = new Array();
            foreach (ECPoint item in keys)
            {
                ecpints.Add(item.ToArray());
            }
            Array array = new Array();
            array.Add(UInt160.Zero.ToArray());
            array.Add(ecpints);
            IContractEvent @event = BindEvent.ParseBindEvent(array);
            Assert.IsTrue(@event is BindEvent);
            Assert.AreEqual(((BindEvent)@event).UserAccount, UInt160.Zero);
            Assert.AreEqual(((BindEvent)@event).Keys.Length, 7);
            array.Add(1);
            Action action = () => BindEvent.ParseBindEvent(array);
            Assert.ThrowsException<Exception>(action);
            @event.ContractEvent();
        }

        [TestMethod]
        public void ParseChequeEventTest()
        {
            Array array = new Array();
            array.Add(new byte[] { 0x01 });
            array.Add(UInt160.Zero.ToArray());
            array.Add(1);
            array.Add(UInt160.Zero.ToArray());
            IContractEvent @event = ChequeEvent.ParseChequeEvent(array);
            Assert.IsTrue(@event is ChequeEvent);
            Assert.AreEqual(((ChequeEvent)@event).Id.ToHexString(), new byte[] { 0x01 }.ToHexString());
            Assert.AreEqual(((ChequeEvent)@event).UserAccount, UInt160.Zero);
            Assert.AreEqual(((ChequeEvent)@event).Amount, (long)1);
            Assert.AreEqual(((ChequeEvent)@event).LockAccount, UInt160.Zero);
            array.Add(1);
            Action action = () => ChequeEvent.ParseChequeEvent(array);
            Assert.ThrowsException<Exception>(action);
            @event.ContractEvent();
        }

        [TestMethod]
        public void ParseDepositEventTest()
        {
            Array array = new Array();
            array.Add(UInt160.Zero.ToArray());
            array.Add(1);
            array.Add(UInt160.Zero.ToArray());
            array.Add(new byte[] { 0x01 });
            IContractEvent @event = DepositEvent.ParseDepositEvent(array);
            Assert.IsTrue(@event is DepositEvent);
            Assert.AreEqual(((DepositEvent)@event).Id.ToHexString(), new byte[] { 0x01 }.ToHexString());
            Assert.AreEqual(((DepositEvent)@event).From, UInt160.Zero);
            Assert.AreEqual(((DepositEvent)@event).Amount, (long)1);
            Assert.AreEqual(((DepositEvent)@event).To, UInt160.Zero);
            array.Add(1);
            Action action = () => DepositEvent.ParseDepositEvent(array);
            Assert.ThrowsException<Exception>(action);
            @event.ContractEvent();
        }

        [TestMethod]
        public void ParseWithdrawEventTest()
        {
            Array array = new Array();
            array.Add(UInt160.Zero.ToArray());
            array.Add(1);
            array.Add(new byte[] { 0x01 });
            IContractEvent @event = WithdrawEvent.ParseWithdrawEvent(array);
            Assert.IsTrue(@event is WithdrawEvent);
            Assert.AreEqual(((WithdrawEvent)@event).Id.ToHexString(), new byte[] { 0x01 }.ToHexString());
            Assert.AreEqual(((WithdrawEvent)@event).UserAccount, UInt160.Zero);
            Assert.AreEqual(((WithdrawEvent)@event).Amount, (long)1);
            array.Add(1);
            Action action = () => WithdrawEvent.ParseWithdrawEvent(array);
            Assert.ThrowsException<Exception>(action);
            @event.ContractEvent();
        }

        [TestMethod]
        public void ParseConfigEventTest()
        {
            Array array = new Array();
            array.Add(new byte[] { 0x01 });
            array.Add(new byte[] { 0x02 });
            array.Add(new byte[] { 0x03 });
            IContractEvent @event = ConfigEvent.ParseConfigEvent(array);
            Assert.IsTrue(@event is ConfigEvent);
            Assert.AreEqual(((ConfigEvent)@event).Id.ToHexString(), new byte[] { 0x01 }.ToHexString());
            Assert.AreEqual(((ConfigEvent)@event).Key.ToHexString(), new byte[] { 0x02 }.ToHexString());
            Assert.AreEqual(((ConfigEvent)@event).Value.ToHexString(), new byte[] { 0x03 }.ToHexString());
            array.Add(1);
            Action action = () => ConfigEvent.ParseConfigEvent(array);
            Assert.ThrowsException<Exception>(action);
            @event.ContractEvent();
        }

        [TestMethod]
        public void ParseUpdateInnerRingTest()
        {
            List<ECPoint> keys = TestBlockchain.wallet.GetAccounts().ToArray().Select(p => p.GetKey().PublicKey).ToList();
            Array ecpints = new Array();
            foreach (ECPoint item in keys)
            {
                ecpints.Add(item.ToArray());
            }
            Array array = new Array();
            array.Add(ecpints);
            IContractEvent @event = UpdateInnerRingEvent.ParseUpdateInnerRingEvent(array);
            Assert.IsTrue(@event is UpdateInnerRingEvent);
            Assert.AreEqual(((UpdateInnerRingEvent)@event).Keys.Length, 7);
            array.Add(1);
            Action action = () => UpdateInnerRingEvent.ParseUpdateInnerRingEvent(array);
            Assert.ThrowsException<Exception>(action);
            @event.ContractEvent();
        }
    }
}
