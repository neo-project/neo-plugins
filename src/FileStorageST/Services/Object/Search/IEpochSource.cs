namespace Neo.FileStorage.Storage.Services.Object.Search
{
    public interface IEpochSource
    {
        ulong CurrentEpoch();
    }
}
