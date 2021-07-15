using Neo.FileStorage.API.Refs;

namespace Neo.FileStorage.Core.Object
{
    public interface IObjectDeleteHandler
    {
        void DeleteObjects(params Address[] addresses);
    }
}
