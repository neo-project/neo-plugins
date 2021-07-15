using Neo.FileStorage.API.Object;

namespace Neo.FileStorage.Storage.Services.ObjectManager.StorageGroup
{
    public static class Helper
    {
        public static SearchFilters SearchQuery()
        {
            SearchFilters filters = new();
            filters.AddTypeFilter(MatchType.StringEqual, ObjectType.StorageGroup);
            return filters;
        }
    }
}
