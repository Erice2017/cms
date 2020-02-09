using System.Collections.Generic;
using System.Threading.Tasks;
using Datory;
using Datory.Caching;
using SiteServer.Abstractions;
using SiteServer.CMS.Core;

namespace SiteServer.CMS.Repositories
{
    public partial class ContentGroupRepository 
    {
        private string GetCacheKey(int siteId)
        {
            return Caching.GetListKey(TableName, siteId);
        }

        public async Task<List<string>> GetGroupNamesAsync(int siteId)
        {
            return await _repository.GetAllAsync<string>(Q
                .Select(nameof(ContentGroup.GroupName))
                .Where(nameof(ContentGroup.SiteId), siteId)
                .OrderByDesc(nameof(ContentGroup.Taxis))
                .OrderBy(nameof(ContentGroup.GroupName))
                .CachingGet(GetCacheKey(siteId))
            );
        }
    }
}