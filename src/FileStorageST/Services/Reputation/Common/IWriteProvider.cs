namespace Neo.FileStorage.Storage.Services.Reputaion.Common
{
    public interface IWriterProvider
    {
        IWriter InitWriter(ICommonContext context);
    }
}
