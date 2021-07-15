using Neo.FileStorage.Storage.Services.Reputaion.Common;
using Neo.FileStorage.Storage.Services.Reputaion.EigenTrust.Storage.Daughters;

namespace Neo.FileStorage.Storage.Services.Reputaion.Intermediate
{
    public class DaughterStorageWriterProvider : IWriterProvider
    {
        public DaughtersStorage Storage { get; init; }

        public IWriter InitWriter(ICommonContext context)
        {
            return new DaughterStorageWriter
            {
                Context = context,
                Storage = Storage,
            };
        }
    }
}
