using Google.Protobuf;
using Google.Protobuf.Collections;
using Neo.FileStorage.API.Refs;
using System.Collections.Generic;
using System.IO;

namespace Neo.FileStorage.Tests.Services.Object.Delete
{
    public static class Helper
    {
        public static RepeatedField<ContainerID> ToRepeatedField(this IEnumerable<ContainerID> list)
        {
            var repeated = new RepeatedField<ContainerID>();
            repeated.AddRange(list);
            return repeated;
        }

        public static byte[] ToByteArray(this RepeatedField<ContainerID> list)
        {
            using MemoryStream ms = new MemoryStream();
            CodedOutputStream output = new CodedOutputStream(ms);
            list.WriteTo(output, FieldCodec.ForMessage(10, ContainerID.Parser));
            output.Flush();
            return ms.ToArray();
        }
    }
}
