namespace Neo.FileStorage.Storage.Services.Reputaion.Common
{
    public class SimpleWriterProvider : IWriterProvider
    {
        private IWriter writer;

        public SimpleWriterProvider(IWriter w)
        {
            writer = w;
        }

        public IWriter InitWriter(ICommonContext _)
        {
            return writer;
        }
    }
}
