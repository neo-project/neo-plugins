using System;
using System.Collections.Generic;
using System.Linq;
using Google.Protobuf;
using Neo.FileStorage.API.Acl;
using Neo.FileStorage.API.Client;
using Neo.FileStorage.API.Refs;
using Neo.FileStorage.API.Session;
using Neo.VM.Types;
using static Neo.FileStorage.API.Container.AnnounceUsedSpaceRequest.Types.Body.Types;
using Array = Neo.VM.Types.Array;
using FSContainer = Neo.FileStorage.API.Container.Container;
using GByteString = Google.Protobuf.ByteString;

namespace Neo.FileStorage.Morph.Invoker
{
    public partial class MorphInvoker
    {
        private const string PutMethod = "put";
        private const string DeleteMethod = "delete";
        private const string GetMethod = "get";
        private const string ListMethod = "list";
        private const string EACLMethod = "eACL";
        private const string SetEACLMethod = "setEACL";
        private const string PutSizeMethod = "putContainerSize";
        private const string ListSizesMethod = "listContainerSizes";
        private const string GetSizeMethod = "getContainerSize";
        private const string StartEstimationMethod = "startContainerEstimation";
        private const string StopEstimationMethod = "stopContainerEstimation";

        public bool PutContainer(FSContainer cnr, Signature sig, SessionToken token)
        {
            return Invoke(out _, ContainerContractHash, PutMethod, SideChainFee, cnr.ToByteArray(), sig.Sign.ToByteArray(), sig.Key.ToByteArray(), token.ToByteArray());
        }

        public bool SetEACL(EACLTable eacl, Signature sig, SessionToken token)
        {
            return Invoke(out _, ContainerContractHash, SetEACLMethod, SideChainFee, eacl.ToByteArray(), sig.Key.ToByteArray(), sig.Sign.ToByteArray(), token.ToByteArray());
        }

        public bool DeleteContainer(ContainerID cid, byte[] sig, SessionToken token)
        {
            return Invoke(out _, ContainerContractHash, DeleteMethod, SideChainFee, cid.Value.ToByteArray(), sig, token.ToByteArray());
        }

        public EAclWithSignature GetEACL(ContainerID containerID)
        {
            InvokeResult result = TestInvoke(ContainerContractHash, EACLMethod, containerID.Value.ToByteArray());
            if (result.State != VM.VMState.HALT) throw new Exception("could not invoke method (EACL)");
            Array array = (Array)result.ResultStack[0];
            if (array.Count != 4) throw new InvalidOperationException($"unexpected eacl stack item count {EACLMethod}: {array.Count}");
            return new()
            {
                Table = EACLTable.Parser.ParseFrom(array[0].GetSpan().ToArray()),
                Signature = new()
                {
                    Key = GByteString.CopyFrom(array[2].GetSpan().ToArray()),
                    Sign = GByteString.CopyFrom(array[1].GetSpan().ToArray()),
                },
                SessionToken = array[3] is VM.Types.Null ? null : SessionToken.Parser.ParseFrom(array[3].GetSpan().ToArray())
            };
        }

        public ContainerWithSignature GetContainer(ContainerID containerID)
        {
            InvokeResult result = TestInvoke(ContainerContractHash, GetMethod, containerID.Value.ToByteArray());
            if (result.State != VM.VMState.HALT) throw new Exception("could not invoke method (Get)");
            Array array = (Array)result.ResultStack[0];
            if (array.Count != 4) throw new InvalidOperationException($"unexpected container stack item count: {array.Count}");
            ContainerWithSignature cnr = new()
            {
                Container = FSContainer.Parser.ParseFrom(array[0].GetSpan().ToArray()),
                Signature = new()
                {
                    Sign = GByteString.CopyFrom(array[1].GetSpan().ToArray()),
                    Key = GByteString.CopyFrom(array[2].GetSpan().ToArray()),
                },
                SessionToken = array[3] is VM.Types.Null ? null : SessionToken.Parser.ParseFrom(array[3].GetSpan().ToArray())
            };
            return cnr;
        }

        public List<ContainerID> ListContainers(OwnerID ownerID)
        {
            InvokeResult result = TestInvoke(ContainerContractHash, ListMethod, ownerID.Value.ToByteArray());
            if (result.State != VM.VMState.HALT) throw new Exception("could not invoke method (List)");
            if (result.ResultStack[0] is Null) return new List<ContainerID>();
            Array array = (Array)result.ResultStack[0];
            IEnumerator<StackItem> enumerator = array.GetEnumerator();
            List<byte[]> resultArray = new();
            while (enumerator.MoveNext())
            {
                resultArray.Add(enumerator.Current.GetSpan().ToArray());
            }
            return resultArray.Select(p => ContainerID.FromSha256Bytes(p)).ToList();
        }

        public bool AnnounceLoad(Announcement announcement, byte[] key)
        {
            return Invoke(out _, ContainerContractHash, PutSizeMethod, SideChainFee, announcement.Epoch, announcement.ContainerId.Value.ToByteArray(), announcement.UsedSpace, key);
        }

        public Estimations InvokeGetContainerSize(ContainerID containerID)
        {
            InvokeResult result = TestInvoke(ContainerContractHash, GetSizeMethod, containerID.Value.ToByteArray());
            if (result.State != VM.VMState.HALT) throw new Exception("could not invoke method (GetContainerSize)");
            Array prms = (Array)result.ResultStack[0];
            Estimations es = new();
            es.ContainerID = ContainerID.FromSha256Bytes(prms[0].GetSpan().ToArray());
            List<Estimation> estimations = new();
            prms = (Array)prms[1];
            foreach (var item in prms)
            {
                Array array = (Array)item;
                Estimation e = new();
                e.Reporter = array[0].GetSpan().ToArray();
                e.Size = (ulong)array[1].GetInteger();
                estimations.Add(e);
            }
            es.AllEstimation = estimations;
            return es;
        }

        public List<byte[]> ListSizes(ulong epoch)
        {
            InvokeResult result = TestInvoke(ContainerContractHash, ListSizesMethod, epoch);
            if (result.State != VM.VMState.HALT) throw new Exception("could not invoke method (ListSizes)");
            if (result.ResultStack[0] is Null) return new List<byte[]>();
            Array prms = (Array)result.ResultStack[0];
            List<byte[]> ids = new();
            foreach (var item in prms)
            {
                var id = item.GetSpan().ToArray();
                ids.Add(id);
            }
            return ids;
        }

        public bool StartEstimation(long epoch)
        {
            return Invoke(out _, ContainerContractHash, StartEstimationMethod, SideChainFee, epoch);
        }

        public bool StopEstimation(long epoch)
        {
            return Invoke(out _, ContainerContractHash, StopEstimationMethod, SideChainFee, epoch);
        }

        public bool RegisterContainer(byte[] key, byte[] container, byte[] signature, byte[] token)
        {
            return Invoke(out _, ContainerContractHash, PutMethod, SideChainFee, container, signature, key, token);
        }

        public bool RemoveContainer(byte[] containerID, byte[] signature, byte[] token)
        {
            return Invoke(out _, ContainerContractHash, DeleteMethod, SideChainFee, containerID, signature, token);
        }
    }
}
