using Akka.Actor;
using Neo.Cryptography.ECC;
using Neo.FileStorage.InnerRing.Invoker;
using Neo.FileStorage.Morph.Event;
using Neo.SmartContract;
using Neo.SmartContract.Native;
using Neo.Wallets;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using static Neo.FileStorage.InnerRing.Events.MorphEvent;
using static Neo.FileStorage.Utils.WorkerPool;

namespace Neo.FileStorage.InnerRing.Processors
{
    public class GovernanceProcessor : BaseProcessor
    {
        private const string AlphabetUpdateIDPrefix = "AlphabetUpdate";
        public override string Name => "GovernanceProcessor";
        private const string DesignationNotification = "Designation";

        public override HandlerInfo[] ListenerHandlers()
        {
            HandlerInfo designateHandler = new HandlerInfo();
            designateHandler.ScriptHashWithType = new ScriptHashWithType() { Type = DesignationNotification, ScriptHashValue = NativeContract.RoleManagement.Hash };
            designateHandler.Handler = HandleAlphabetSync;
            return new HandlerInfo[] { designateHandler };
        }

        public override ParserInfo[] ListenerParsers()
        {
            //deposit event
            ParserInfo designateParser = new ParserInfo();
            designateParser.ScriptHashWithType = new ScriptHashWithType() { Type = DesignationNotification, ScriptHashValue = NativeContract.RoleManagement.Hash };
            designateParser.Parser = DesignateEvent.ParseDesignateEvent;
            return new ParserInfo[] { designateParser };
        }

        public void HandleAlphabetSync(IContractEvent morphEvent)
        {
            string type;
            if (morphEvent is SyncEvent) type = "sync";
            else if (morphEvent is DesignateEvent)
            {
                if (((DesignateEvent)morphEvent).role != (byte)Role.NeoFSAlphabetNode) return;
                type = "designation";
            }
            else return;
            Utility.Log(Name, LogLevel.Info, string.Format("new event,type:{0}", type));
            WorkPool.Tell(new NewTask() { Process = Name, Task = new Task(() => ProcessAlphabetSync()) });
        }

        public void ProcessAlphabetSync()
        {
            if (!State.IsAlphabet())
            {
                Utility.Log(Name, LogLevel.Info, "non alphabet mode, ignore alphabet sync");
                return;
            }
            ECPoint[] mainnetAlphabet;
            try
            {
                mainnetAlphabet = MainCli.NeoFSAlphabetList();
            }
            catch (Exception e)
            {
                Utility.Log(Name, LogLevel.Error, string.Format("can't fetch alphabet list from main net,error:{0}", e.Message));
                return;
            }
            ECPoint[] sidechainAlphabet;
            try
            {
                sidechainAlphabet = MorphCli.Committee();
            }
            catch (Exception e)
            {
                Utility.Log(Name, LogLevel.Error, string.Format("can't fetch alphabet list from side chain,error:{0}", e.Message));
                return;
            }
            ECPoint[] newAlphabet;
            try
            {
                newAlphabet = NewAlphabetList(sidechainAlphabet, mainnetAlphabet);
            }
            catch (Exception e)
            {
                Utility.Log(Name, LogLevel.Error, string.Format("can't merge alphabet lists from main net and side chain,error:{0}", e.Message));
                return;
            }
            if (newAlphabet is null)
            {
                Utility.Log(Name, LogLevel.Info, "no governance update, alphabet list has not been changed");
                return;
            }
            Utility.Log(Name, LogLevel.Info, "alphabet list has been changed, starting update");
            Array.Sort(newAlphabet);
            try
            {
                State.VoteForSidechainValidator(newAlphabet);
            }
            catch (Exception e)
            {
                Utility.Log(Name, LogLevel.Info, string.Format("can't vote for side chain committee,error:{0}", e.Message));
                return;
            }
            ECPoint[] innerRing;
            try
            {
                innerRing = MorphCli.NeoFSAlphabetList();
                ECPoint[] newInnerRing;
                try
                {
                    newInnerRing = UpdateInnerRing(innerRing, sidechainAlphabet, newAlphabet);
                    Array.Sort(newInnerRing);
                    MorphCli.SetInnerRing(newInnerRing);
                }
                catch (Exception e)
                {
                    Utility.Log(Name, LogLevel.Info, string.Format("can't create new inner ring list with new alphabet keysn,error:{0}", e.Message));
                }
            }
            catch (Exception e)
            {
                Utility.Log(Name, LogLevel.Info, string.Format("can't fetch inner ring list from side chain,error:{0}", e.Message));
                return;
            }
            var epoch = State.EpochCounter();
            var id = System.Text.Encoding.UTF8.GetBytes(AlphabetUpdateIDPrefix).Concat(BitConverter.GetBytes(epoch)).ToArray();
            try
            {
                MainCli.AlphabetUpdate(id, newAlphabet);
            }
            catch (Exception e)
            {
                Utility.Log(Name, LogLevel.Info, string.Format("can't update list of alphabet nodes in neofs contract,error:{0}", e.Message));
            }
            Utility.Log(Name, LogLevel.Info, "finished alphabet list update");
        }

        public ECPoint[] NewAlphabetList(ECPoint[] sidechain, ECPoint[] mainnet)
        {
            var ln = sidechain.Length;
            if (ln == 0) throw new Exception("sidechain list is empty");
            if (mainnet.Length < ln) throw new Exception(string.Format("alphabet list in mainnet is too short,expecting {0} keys", ln));
            var hmap = new Dictionary<string, bool>();
            var result = new List<ECPoint>();
            foreach (var node in sidechain) hmap[node.EncodePoint(true).ToScriptHash().ToAddress(ProtocolSettings.AddressVersion)] = false;
            var newNodes = 0;
            var newNodeLimit = (ln - 1) / 3;
            for (int i = 0; i < mainnet.Length; i++)
            {
                if (result.Count == ln) break;
                var mainnetAddr = mainnet[i].EncodePoint(true).ToScriptHash().ToAddress(ProtocolSettings.AddressVersion);
                if (!hmap.TryGetValue(mainnetAddr, out _)) {
                    if (newNodes == newNodeLimit) continue;
                    newNodes++;
                }
                else hmap[mainnetAddr] = true;
                result.Add(mainnet[i]);
            }
            if (newNodes == 0) return null;
            foreach (var node in sidechain)
            {
                if (result.Count == ln) break;
                if (!hmap[node.EncodePoint(true).ToScriptHash().ToAddress(ProtocolSettings.AddressVersion)]) result.Add(node);
            }
            result.Sort();
            return result.ToArray();
        }

        private ECPoint[] UpdateInnerRing(ECPoint[] innerRing, ECPoint[] before, ECPoint[] after)
        {
            if (before.Length != after.Length) throw new Exception("old and new alphabet lists have different length");
            var result = new List<ECPoint>();
            for (int i = 0; i < innerRing.Length; i++)
            {
                bool loopFlag = false;
                for (int j = 0; j < before.Length; j++)
                {
                    if (innerRing[i].Equals(before[j]))
                    {
                        result.Add(after[j]);
                        loopFlag = true;
                    }
                    if (loopFlag) break;
                }
                if (loopFlag) continue;
                result.Add(innerRing[i]);
            }
            return result.ToArray();
        }
    }
}
