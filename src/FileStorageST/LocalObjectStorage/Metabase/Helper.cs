using System.Collections.Generic;
using System.IO;
using Google.Protobuf;
using Neo.FileStorage.API.Refs;
using Neo.IO;

namespace Neo.FileStorage.Storage.LocalObjectStorage.Metabase
{
    public static class Helper
    {
        public static byte[] EncodeObjectIDList(List<ObjectID> oids)
        {
            using MemoryStream ms = new();
            using BinaryWriter writer = new(ms);
            writer.WriteVarInt(oids.Count);
            foreach (var oid in oids)
                writer.Write(oid.Value.ToByteArray());
            writer.Flush();
            return ms.ToArray();
        }

        public static List<ObjectID> DecodeObjectIDList(byte[] data)
        {
            List<ObjectID> oids = new();
            using MemoryStream ms = new(data);
            using BinaryReader reader = new(ms);
            int count = (int)reader.ReadVarInt(uint.MaxValue);
            for (int i = 0; i < count; i++)
                oids.Add(new()
                {
                    Value = ByteString.CopyFrom(reader.ReadFixedBytes(ObjectID.ValueSize))
                });
            return oids;
        }
    }
}
