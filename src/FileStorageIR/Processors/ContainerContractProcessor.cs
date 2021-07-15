using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Akka.Actor;
using Google.Protobuf;
using Neo.Cryptography;
using Neo.FileStorage.API.Acl;
using Neo.FileStorage.API.Client;
using Neo.FileStorage.API.Container;
using Neo.FileStorage.API.Cryptography;
using Neo.FileStorage.API.Refs;
using Neo.FileStorage.API.Session;
using Neo.FileStorage.InnerRing.Invoker;
using Neo.FileStorage.Morph.Event;
using Neo.FileStorage.Morph.Invoker;
using Neo.IO;
using static Neo.FileStorage.Morph.Event.MorphEvent;
using static Neo.FileStorage.Utils.WorkerPool;
using ECPoint = Neo.Cryptography.ECC.ECPoint;

namespace Neo.FileStorage.InnerRing.Processors
{
    public class ContainerContractProcessor : BaseProcessor
    {
        public override string Name => "ContainerContractProcessor";
        private const string PutNotification = "containerPut";
        private const string DeleteNotification = "containerDelete";
        private const string SetEACLNotification = "setEACL";

        public override HandlerInfo[] ListenerHandlers()
        {
            HandlerInfo putHandler = new HandlerInfo();
            putHandler.ScriptHashWithType = new ScriptHashWithType() { Type = PutNotification, ScriptHashValue = ContainerContractHash };
            putHandler.Handler = HandlePut;
            HandlerInfo deleteHandler = new HandlerInfo();
            deleteHandler.ScriptHashWithType = new ScriptHashWithType() { Type = DeleteNotification, ScriptHashValue = ContainerContractHash };
            deleteHandler.Handler = HandleDelete;
            HandlerInfo setEACLHandler = new HandlerInfo();
            setEACLHandler.ScriptHashWithType = new ScriptHashWithType() { Type = SetEACLNotification, ScriptHashValue = ContainerContractHash };
            setEACLHandler.Handler = HandleSetEACL;
            return new HandlerInfo[] { putHandler, deleteHandler, setEACLHandler };
        }

        public override ParserInfo[] ListenerParsers()
        {
            //container put event
            ParserInfo putParser = new ParserInfo();
            putParser.ScriptHashWithType = new ScriptHashWithType() { Type = PutNotification, ScriptHashValue = ContainerContractHash };
            putParser.Parser = ContainerPutEvent.ParseContainerPutEvent;
            //container delete event
            ParserInfo deleteParser = new ParserInfo();
            deleteParser.ScriptHashWithType = new ScriptHashWithType() { Type = DeleteNotification, ScriptHashValue = ContainerContractHash };
            deleteParser.Parser = ContainerDeleteEvent.ParseContainerDeleteEvent;
            //container setEACL event
            ParserInfo setEACLParser = new ParserInfo();
            setEACLParser.ScriptHashWithType = new ScriptHashWithType() { Type = SetEACLNotification, ScriptHashValue = ContainerContractHash };
            setEACLParser.Parser = ContainerSetEACLEvent.ParseContainerSetEACLEvent;
            return new ParserInfo[] { putParser, deleteParser, setEACLParser };
        }

        public void HandlePut(IContractEvent morphEvent)
        {
            ContainerPutEvent putEvent = (ContainerPutEvent)morphEvent;
            var id = putEvent.RawContainer.Sha256();
            Utility.Log(Name, LogLevel.Info, string.Format("notification:type:container put,id:{0}", Base58.Encode(id)));
            WorkPool.Tell(new NewTask() { Process = Name, Task = new Task(() => ProcessContainerPut(putEvent)) });
        }

        public void HandleDelete(IContractEvent morphEvent)
        {
            ContainerDeleteEvent deleteEvent = (ContainerDeleteEvent)morphEvent;
            Utility.Log(Name, LogLevel.Info, string.Format("notification:type:container delete,id:{0}", Base58.Encode(deleteEvent.ContainerID)));
            WorkPool.Tell(new NewTask() { Process = Name, Task = new Task(() => ProcessContainerDelete(deleteEvent)) });
        }

        public void HandleSetEACL(IContractEvent morphEvent)
        {
            ContainerSetEACLEvent setEACLEvent = (ContainerSetEACLEvent)morphEvent;
            Utility.Log(Name, LogLevel.Info, "notification:type:set EACL");
            WorkPool.Tell(new NewTask() { Process = Name, Task = new Task(() => ProcessContainerSetEACL(setEACLEvent)) });
        }

        public void ProcessContainerPut(ContainerPutEvent putEvent)
        {
            Console.WriteLine("ProcessContainerPut----step1");
            if (!State.IsAlphabet())
            {
                Utility.Log(Name, LogLevel.Info, "non alphabet mode, ignore container put");
                return;
            }
            Console.WriteLine("ProcessContainerPut----step2");
            var cnrData = putEvent.RawContainer;
            var key = ECPoint.DecodePoint(putEvent.PublicKey, Cryptography.ECC.ECCurve.Secp256r1);
            Container container = null;
            try
            {
                container = Container.Parser.ParseFrom(cnrData);
            }
            catch (Exception e)
            {
                Utility.Log(Name, LogLevel.Error, string.Format("could not unmarshal container structure:{0}", e.Message));
                return;
            }
            Console.WriteLine("ProcessContainerPut----step3");
            if (!key.EncodePoint(true).LoadPublicKey().VerifyData(cnrData, putEvent.Signature, System.Security.Cryptography.HashAlgorithmName.SHA256))
            {
                Utility.Log(Name, LogLevel.Error, "invalid signature");
                return;
            }
            Console.WriteLine("ProcessContainerPut----step4");
            try
            {
                CheckFormat(container);
            }
            catch (Exception e)
            {
                Utility.Log(Name, LogLevel.Error, string.Format("container with incorrect format detected:{0}", e.Message));
            }
            Console.WriteLine("ProcessContainerPut----step5");
            SessionToken token = null;
            if (putEvent.token != null && !putEvent.token.SequenceEqual(new byte[0]))
            {
                try
                {
                    Console.WriteLine("ProcessContainerPut----step5-1");
                    token = SessionToken.Parser.ParseFrom(putEvent.token);
                    Console.WriteLine("ProcessContainerPut----step5-2");
                    CheckTokenContext(token, (ContainerSessionContext c) => { return c.Verb == ContainerSessionContext.Types.Verb.Put; });
                }
                catch (Exception e)
                {
                    Utility.Log(Name, LogLevel.Error, string.Format("container checkToken context fault,error:{0}", e.Message));
                    return;
                }
            }
            Console.WriteLine("ProcessContainerPut----step6");
            ContainerWithSignature cnr = new()
            {
                Container = container,
                Signature = new Signature()
                {
                    Key = ByteString.CopyFrom(key.ToArray()),
                    Sign = ByteString.CopyFrom(putEvent.Signature)
                },
                SessionToken = token
            };
            Console.WriteLine("ProcessContainerPut----step7");
            CheckKeyOwnership(cnr, key);
            Console.WriteLine("ProcessContainerPut----step8");
            try
            {
                var r = MorphCli.RegisterContainer(putEvent.PublicKey, putEvent.RawContainer, putEvent.Signature, putEvent.token);
                Console.WriteLine("ProcessContainerPut----step9:" + r);
            }
            catch (Exception e)
            {
                Utility.Log(Name, LogLevel.Error, string.Format("can't invoke new container:{0}", e.Message));
            }
        }

        public void ProcessContainerDelete(ContainerDeleteEvent deleteEvent)
        {
            if (!State.IsAlphabet())
            {
                Utility.Log(Name, LogLevel.Info, "non alphabet mode, ignore container put");
                return;
            }
            var checkKeys = new List<ECPoint>();
            try
            {
                var cnr = MorphCli.GetContainer(ContainerID.FromSha256Bytes(deleteEvent.ContainerID));
                SessionToken token = null;
                if (deleteEvent.token != null && !deleteEvent.token.SequenceEqual(new byte[0]))
                {
                    token = SessionToken.Parser.ParseFrom(deleteEvent.token);
                    var containerId = ContainerID.FromSha256Bytes(deleteEvent.ContainerID);
                    CheckTokenContextWithCID(token, containerId, (ContainerSessionContext c) => { return c.Verb == ContainerSessionContext.Types.Verb.Delete; });
                    var key = ECPoint.DecodePoint(token.Body.SessionKey.ToByteArray(), Cryptography.ECC.ECCurve.Secp256r1);
                    CheckKeyOwnershipWithToken(cnr, key, token);
                    checkKeys.Add(key);
                }
                else
                {
                    ECPoint[] keys = MorphCli.AccountKeys(cnr.Container.OwnerId.Value.ToByteArray());
                    checkKeys.AddRange(keys);
                }
                var cidHash = ContainerID.FromSha256Bytes(deleteEvent.ContainerID).Sha256Checksum().Sum.ToByteArray();
                var sig = deleteEvent.Signature;
                if (!checkKeys.Any(p => p.EncodePoint(true).LoadPublicKey().VerifyData(cidHash, sig))) throw new Exception("signature verification failed on all owner keys");
                MorphCli.RemoveContainer(deleteEvent.ContainerID, deleteEvent.Signature, deleteEvent.token);
            }
            catch (Exception e)
            {
                Utility.Log(Name, LogLevel.Error, string.Format("can't invoke delete container:{0}", e.Message));
            }
        }

        public void ProcessContainerSetEACL(ContainerSetEACLEvent setEACLEvent)
        {
            if (!State.IsAlphabet())
            {
                Utility.Log(Name, LogLevel.Info, "non alphabet mode, ignore container put");
                return;
            }
            try
            {
                var table = EACLTable.Parser.ParseFrom(setEACLEvent.Table);
                var signature = Signature.Parser.ParseFrom(setEACLEvent.Signature);
                var sessionToken = SessionToken.Parser.ParseFrom(setEACLEvent.Token);
                var key = ECPoint.DecodePoint(setEACLEvent.PublicKey, Cryptography.ECC.ECCurve.Secp256r1);
                if (!key.EncodePoint(true).LoadPublicKey().VerifyHash(table.Sha256Checksum().Sum.ToByteArray(), setEACLEvent.Signature))
                {
                    Utility.Log(Name, LogLevel.Error, "invalid signature");
                    return;
                }
                var cnr = MorphCli.GetContainer(table.ContainerId);
                if (sessionToken != null && !setEACLEvent.Token.SequenceEqual(new byte[0]))
                {
                    CheckTokenContextWithCID(sessionToken, table.ContainerId, (ContainerSessionContext c) => { return c.Verb == ContainerSessionContext.Types.Verb.Seteacl; });
                }
                CheckKeyOwnership(cnr, key);
                MorphCli.SetEACL(table, signature, sessionToken);
            }
            catch (Exception e)
            {
                Utility.Log(Name, LogLevel.Error, string.Format("could not approve set EACL,error:{0}", e.Message));
            }
        }

        public void CheckFormat(Container container)
        {
            Console.WriteLine("ProcessContainerPut----step4-1");
            if (container.PlacementPolicy is null) throw new Exception("placement policy is nil");
            Console.WriteLine("ProcessContainerPut----step4-2");
            if (!API.Refs.Version.IsSupportedVersion(container.Version)) throw new Exception("incorrect version");
            Console.WriteLine("ProcessContainerPut----step4-3");
            if (container.OwnerId.Value.Length != 25) throw new Exception(string.Format("incorrect owner identifier:expected length {0}!={1}", 25, container.OwnerId.Value.Length));
            Console.WriteLine("ProcessContainerPut----step4-4");
            if (container.Nonce.ToByteArray().Length != 16) throw new Exception("incorrect nonce");
        }

        public void CheckKeyOwnership(ContainerWithSignature ownerIDSource, ECPoint key)
        {
            Console.WriteLine("ProcessContainerPut----step7-1");
            var token = ownerIDSource.SessionToken;
            if (token is not null)
            {
                CheckKeyOwnershipWithToken(ownerIDSource, key, token);
                return;
            }
            Console.WriteLine("ProcessContainerPut----step7-2");
            if (ownerIDSource.Container.OwnerId.Equals(key.EncodePoint(true).PublicKeyToOwnerID())) return;
            var ownerKeys = MorphCli.AccountKeys(ownerIDSource.Container.OwnerId.Value.ToByteArray());
            Console.WriteLine("ProcessContainerPut----step7-3");
            if (ownerKeys is null) throw new Exception("could not received owner keys");
            if (!ownerKeys.Any(p => p.Equals(key))) throw new Exception(string.Format("key {0} is not tied to the owner of the container", key));
            Console.WriteLine("ProcessContainerPut----step7-4");
        }

        public void CheckKeyOwnershipWithToken(ContainerWithSignature ownerIDSource, ECPoint key, SessionToken token)
        {
            if (!key.EncodePoint(true).SequenceEqual(token.Body.SessionKey.ToByteArray())) throw new Exception("signed with a non-session key");
            if (!token.Body.OwnerId.Equals(ownerIDSource.Container.OwnerId)) throw new Exception("owner differs with token owner");
            CheckSessionToken(token);
        }

        public void CheckSessionToken(SessionToken token)
        {
            // verify signature
            if (!token.VerifySignature()) throw new Exception("invalid signature");
            // check lifetime
            var curEpoch = state.EpochCounter();
            var nbf = token.Body.Lifetime.Nbf;
            if (curEpoch < nbf) throw new Exception(string.Format("token is not valid yet: nbf {0}, cur {1}", nbf, curEpoch));
            var iat = token.Body.Lifetime.Iat;
            if (curEpoch < iat) throw new Exception(string.Format("token is issued in future: iat {0}, cur {1}", iat, curEpoch));
            var exp = token.Body.Lifetime.Exp;
            if (curEpoch < exp) throw new Exception(string.Format("token is expired: exp {0}, cur {1}", exp, curEpoch));
        }

        public ContainerSessionContext ContextWithVerifiedVerb(SessionToken token, Func<ContainerSessionContext, bool> verbAssert)
        {
            ContainerSessionContext c = token.Body.Container;
            if (c is null) throw new Exception("wrong session context");
            if (!verbAssert(c)) throw new Exception("wrong token verb");
            return c;
        }

        public void CheckTokenContext(SessionToken token, Func<ContainerSessionContext, bool> verbAssert)
        {
            Console.WriteLine("ProcessContainerPut----step5-3");
            ContextWithVerifiedVerb(token, verbAssert);
        }

        public void CheckTokenContextWithCID(SessionToken token, ContainerID id, Func<ContainerSessionContext, bool> verbAssert)
        {
            var c = ContextWithVerifiedVerb(token, verbAssert);
            var tokCID = c.ContainerId;
            if (tokCID is not null && !tokCID.Equals(id)) throw new Exception("wrong container ID");
        }
    }
}
