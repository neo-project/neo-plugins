using System;
using System.IO;
using Neo.IO;
using Neo.Network.P2P.Payloads;
using Neo.Persistence;


namespace Neo.FileStorage.Morph.Invoker
{
    public class FakeSigners : IVerifiable
    {
        private readonly UInt160[] _hashForVerify;
        Witness[] IVerifiable.Witnesses { get; set; }

        int ISerializable.Size => throw new NotImplementedException();

        void ISerializable.Deserialize(BinaryReader reader)
        {
            throw new NotImplementedException();
        }

        void IVerifiable.DeserializeUnsigned(BinaryReader reader)
        {
            throw new NotImplementedException();
        }

        public FakeSigners(params UInt160[] hashForVerify)
        {
            _hashForVerify = hashForVerify ?? new UInt160[0];
        }

        UInt160[] IVerifiable.GetScriptHashesForVerifying(DataCache snapshot)
        {
            return _hashForVerify;
        }

        void ISerializable.Serialize(BinaryWriter writer)
        {
            throw new NotImplementedException();
        }

        void IVerifiable.SerializeUnsigned(BinaryWriter writer)
        {
            throw new NotImplementedException();
        }
    }
}
