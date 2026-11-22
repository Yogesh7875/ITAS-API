using Project.Core.Domain.Users;
using Project.Services.Caching;

namespace Project.Services.Directory.Caching;

public class CompanyCacheEventConsumer : CacheEventConsumer<Company>
{
    /// <summary>
    /// Clear cache data
    /// </summary>
    /// <param name="entity">Entity</param>
    /// <returns>A task that represents the asynchronous operation</returns>
    protected override async Task ClearCacheAsync(Company entity)
    {
        await RemoveByPrefixAsync(DirectoryDefault.CompanySelectDataAllCacheKeyPrefix);
    }
}
