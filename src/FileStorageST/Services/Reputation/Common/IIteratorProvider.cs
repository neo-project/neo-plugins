namespace Neo.FileStorage.Storage.Services.Reputaion.Common
{
    public interface IIteratorProvider
    {
        IIterator InitIterator(ICommonContext context);
    }
}
