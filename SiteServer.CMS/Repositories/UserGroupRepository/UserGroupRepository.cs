﻿using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Datory;
using SiteServer.Abstractions;
using SiteServer.CMS.Core;

namespace SiteServer.CMS.Repositories
{
    public partial class UserGroupRepository : IRepository
    {
        private readonly Repository<UserGroup> _repository;

        public UserGroupRepository()
        {
            _repository = new Repository<UserGroup>(new Database(WebConfigUtils.DatabaseType, WebConfigUtils.ConnectionString), new Redis(WebConfigUtils.RedisConnectionString));
        }

        public IDatabase Database => _repository.Database;

        public string TableName => _repository.TableName;

        public List<TableColumn> TableColumns => _repository.TableColumns;

        private string CacheKey => Caching.GetListKey(_repository.TableName);

        public async Task<int> InsertAsync(UserGroup group)
        {
            return await _repository.InsertAsync(group, Q.CachingRemove(CacheKey));
        }

        public async Task UpdateAsync(UserGroup group)
        {
            await _repository.UpdateAsync(group, Q.CachingRemove(CacheKey));
        }

        public async Task DeleteAsync(int groupId)
        {
            await _repository.DeleteAsync(groupId, Q.CachingRemove(CacheKey));
        }

        public async Task<List<UserGroup>> GetUserGroupListAsync()
        {
            var config = await DataProvider.ConfigRepository.GetAsync();

            var list = (await _repository.GetAllAsync(Q
                .OrderBy(nameof(UserGroup.Id))
                .CachingGet(CacheKey)
            )).ToList();

            list.Insert(0, new UserGroup
            {
                Id = 0,
                GroupName = "默认用户组",
                AdminName = config.UserDefaultGroupAdminName
            });

            return list;
        }
    }
}
