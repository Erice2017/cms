﻿using System.Collections.Generic;
using System.Collections.Specialized;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using Datory.Utils;
using SiteServer.CMS.Context.Atom.Atom.Core;
using SiteServer.CMS.Context.Atom.Atom.Core.Collections;
using SiteServer.Abstractions;
using SiteServer.CMS.Context.Enumerations;
using SiteServer.CMS.Core;
using SiteServer.CMS.DataCache;
using SiteServer.CMS.Repositories;

namespace SiteServer.CMS.ImportExport.Components
{
    internal class ContentIe
    {
        private readonly Site _site;
        private readonly string _siteContentDirectoryPath;

        public ContentIe(Site site, string siteContentDirectoryPath)
        {
            _siteContentDirectoryPath = siteContentDirectoryPath;
            _site = site;
        }

        public async Task ImportContentsAsync(string filePath, bool isOverride, Channel channel, int taxis, int importStart, int importCount, bool isChecked, int checkedLevel, string adminName)
        {
            if (!FileUtils.IsFileExists(filePath)) return;
            var feed = AtomFeed.Load(FileUtils.GetFileStreamReadOnly(filePath));

            await ImportContentsAsync(feed.Entries, channel, taxis, importStart, importCount, false, isChecked, checkedLevel, isOverride, adminName);
        }

        public async Task<List<int>> ImportContentsAsync(string filePath, bool isOverride, Channel channel, int taxis, bool isChecked, int checkedLevel, int adminId, int userId, int sourceId)
        {
            if (!FileUtils.IsFileExists(filePath)) return null;
            var feed = AtomFeed.Load(FileUtils.GetFileStreamReadOnly(filePath));

            return await ImportContentsAsync(feed.Entries, channel, taxis, false, isChecked, checkedLevel, isOverride, adminId, userId, sourceId);
        }

        public async Task<List<int>> ImportContentsAsync(AtomEntryCollection entries, Channel channel, int taxis, bool isOverride, string adminName)
        {
            return await ImportContentsAsync(entries, channel, taxis, 0, 0, true, true, 0, isOverride, adminName);
        }

        // 内部消化掉错误
        private async Task<List<int>> ImportContentsAsync(AtomEntryCollection entries, Channel channel, int taxis, int importStart, int importCount, bool isCheckedBySettings, bool isChecked, int checkedLevel, bool isOverride, string adminName)
        {
            if (importStart > 1 || importCount > 0)
            {
                var theEntries = new AtomEntryCollection();

                if (importStart == 0)
                {
                    importStart = 1;
                }
                if (importCount == 0)
                {
                    importCount = entries.Count;
                }

                var firstIndex = entries.Count - importStart - importCount + 1;
                if (firstIndex <= 0)
                {
                    firstIndex = 0;
                }

                var addCount = 0;
                for (var i = 0; i < entries.Count; i++)
                {
                    if (addCount >= importCount) break;
                    if (i >= firstIndex)
                    {
                        theEntries.Add(entries[i]);
                        addCount++;
                    }
                }

                entries = theEntries;
            }

            var tableName = await DataProvider.ChannelRepository.GetTableNameAsync(_site, channel);
            var contentIdList = new List<int>();

            foreach (AtomEntry entry in entries)
            {
                try
                {
                    taxis++;
                    var lastEditDate = AtomUtility.GetDcElementContent(entry.AdditionalElements, ContentAttribute.LastEditDate);
                    var groupNameCollection = AtomUtility.GetDcElementContent(entry.AdditionalElements, new List<string>{ nameof(Content.GroupNames), "GroupNameCollection", "ContentGroupNameCollection" });
                    if (isCheckedBySettings)
                    {
                        isChecked = TranslateUtils.ToBool(AtomUtility.GetDcElementContent(entry.AdditionalElements,
                            new List<string> {nameof(ContentAttribute.Checked), "IsChecked"}));
                        checkedLevel = TranslateUtils.ToInt(AtomUtility.GetDcElementContent(entry.AdditionalElements, ContentAttribute.CheckedLevel));
                    }
                    var hits = TranslateUtils.ToInt(AtomUtility.GetDcElementContent(entry.AdditionalElements, ContentAttribute.Hits));
                    var hitsByDay = TranslateUtils.ToInt(AtomUtility.GetDcElementContent(entry.AdditionalElements, ContentAttribute.HitsByDay));
                    var hitsByWeek = TranslateUtils.ToInt(AtomUtility.GetDcElementContent(entry.AdditionalElements, ContentAttribute.HitsByWeek));
                    var hitsByMonth = TranslateUtils.ToInt(AtomUtility.GetDcElementContent(entry.AdditionalElements, ContentAttribute.HitsByMonth));
                    var lastHitsDate = AtomUtility.GetDcElementContent(entry.AdditionalElements, ContentAttribute.LastHitsDate);
                    var downloads = TranslateUtils.ToInt(AtomUtility.GetDcElementContent(entry.AdditionalElements, ContentAttribute.Downloads));
                    var title = AtomUtility.GetDcElementContent(entry.AdditionalElements, ContentAttribute.Title);
                    var isTop = TranslateUtils.ToBool(AtomUtility.GetDcElementContent(entry.AdditionalElements,
                        new List<string> {nameof(Content.Top), "IsTop"}));
                    var isRecommend = TranslateUtils.ToBool(AtomUtility.GetDcElementContent(entry.AdditionalElements,
                        new List<string> {nameof(Content.Recommend), "IsRecommend"}));
                    var isHot = TranslateUtils.ToBool(AtomUtility.GetDcElementContent(entry.AdditionalElements,
                        new List<string> {nameof(Content.Hot), "IsHot"}));
                    var isColor = TranslateUtils.ToBool(AtomUtility.GetDcElementContent(entry.AdditionalElements,
                        new List<string> {nameof(Content.Color), "IsColor"}));
                    var linkUrl = AtomUtility.Decrypt(AtomUtility.GetDcElementContent(entry.AdditionalElements, ContentAttribute.LinkUrl));
                    var addDate = AtomUtility.GetDcElementContent(entry.AdditionalElements, ContentAttribute.AddDate);

                    var topTaxis = 0;
                    if (isTop)
                    {
                        topTaxis = taxis - 1;
                        taxis = await DataProvider.ContentRepository.GetMaxTaxisAsync(_site, channel, true) + 1;
                    }
                    var tags = AtomUtility.Decrypt(AtomUtility.GetDcElementContent(entry.AdditionalElements, new List<string> { nameof(Content.TagNames), "Tags" }));

                    var contentInfo = new Content
                    {
                        SiteId = _site.Id,
                        ChannelId = channel.Id,
                        AddDate = TranslateUtils.ToDateTime(addDate),
                        LastEditDate = TranslateUtils.ToDateTime(lastEditDate),
                        GroupNames = Utilities.GetStringList(groupNameCollection),
                        TagNames = Utilities.GetStringList(tags),
                        Checked = isChecked,
                        CheckedLevel = checkedLevel,
                        Hits = hits,
                        HitsByDay = hitsByDay,
                        HitsByWeek = hitsByWeek,
                        HitsByMonth = hitsByMonth,
                        LastHitsDate = TranslateUtils.ToDateTime(lastHitsDate),
                        Downloads = downloads,
                        Title = AtomUtility.Decrypt(title),
                        Top = isTop,
                        Recommend = isRecommend,
                        Hot = isHot,
                        Color = isColor,
                        LinkUrl = linkUrl
                    };

                    var attributes = AtomUtility.GetDcElementNameValueCollection(entry.AdditionalElements);
                    foreach (string attributeName in attributes.Keys)
                    {
                        if (!contentInfo.ContainsKey(attributeName.ToLower()))
                        {
                            contentInfo.Set(attributeName, AtomUtility.Decrypt(attributes[attributeName]));
                        }
                    }

                    var isInsert = false;
                    if (isOverride)
                    {
                        var existsIDs = DataProvider.ContentRepository.GetIdListBySameTitle(tableName, contentInfo.ChannelId, contentInfo.Title);
                        if (existsIDs.Count > 0)
                        {
                            foreach (var id in existsIDs)
                            {
                                contentInfo.Id = id;
                                await DataProvider.ContentRepository.UpdateAsync(_site, channel, contentInfo);
                            }
                        }
                        else
                        {
                            isInsert = true;
                        }
                    }
                    else
                    {
                        isInsert = true;
                    }

                    if (isInsert)
                    {
                        var contentId = await DataProvider.ContentRepository.InsertWithTaxisAsync(_site, channel, contentInfo, taxis);
                        contentIdList.Add(contentId);

                        if (!string.IsNullOrEmpty(tags))
                        {
                            foreach (var tagName in Utilities.GetStringList(tags))
                            {
                                await DataProvider.ContentTagRepository.InsertAsync(_site.Id, tagName);
                            }
                        }
                    }

                    if (isTop)
                    {
                        taxis = topTaxis;
                    }
                }
                catch
                {
                    // ignored
                }
            }

            return contentIdList;
        }

        private async Task<List<int>> ImportContentsAsync(AtomEntryCollection entries, Channel channel, int taxis, bool isCheckedBySettings, bool isChecked, int checkedLevel, bool isOverride, int adminId, int userId, int sourceId)
        {
            var tableName = await DataProvider.ChannelRepository.GetTableNameAsync(_site, channel);
            var contentIdList = new List<int>();

            foreach (AtomEntry entry in entries)
            {
                try
                {
                    taxis++;
                    var lastEditDate = AtomUtility.GetDcElementContent(entry.AdditionalElements, ContentAttribute.LastEditDate);
                    var groupNameCollection = AtomUtility.GetDcElementContent(entry.AdditionalElements, new List<string> { nameof(Content.GroupNames), "GroupNameCollection", "ContentGroupNameCollection" });
                    if (isCheckedBySettings)
                    {
                        isChecked = TranslateUtils.ToBool(AtomUtility.GetDcElementContent(entry.AdditionalElements, new List<string>{nameof(Content.Checked), "IsChecked" }));
                        checkedLevel = TranslateUtils.ToInt(AtomUtility.GetDcElementContent(entry.AdditionalElements, ContentAttribute.CheckedLevel));
                    }
                    var hits = TranslateUtils.ToInt(AtomUtility.GetDcElementContent(entry.AdditionalElements, ContentAttribute.Hits));
                    var hitsByDay = TranslateUtils.ToInt(AtomUtility.GetDcElementContent(entry.AdditionalElements, ContentAttribute.HitsByDay));
                    var hitsByWeek = TranslateUtils.ToInt(AtomUtility.GetDcElementContent(entry.AdditionalElements, ContentAttribute.HitsByWeek));
                    var hitsByMonth = TranslateUtils.ToInt(AtomUtility.GetDcElementContent(entry.AdditionalElements, ContentAttribute.HitsByMonth));
                    var lastHitsDate = AtomUtility.GetDcElementContent(entry.AdditionalElements, ContentAttribute.LastHitsDate);
                    var downloads = TranslateUtils.ToInt(AtomUtility.GetDcElementContent(entry.AdditionalElements, ContentAttribute.Downloads));
                    var title = AtomUtility.GetDcElementContent(entry.AdditionalElements, ContentAttribute.Title);
                    var isTop = TranslateUtils.ToBool(AtomUtility.GetDcElementContent(entry.AdditionalElements, new List<string> { nameof(Content.Top), "IsTop" }));
                    var isRecommend = TranslateUtils.ToBool(AtomUtility.GetDcElementContent(entry.AdditionalElements, new List<string> { nameof(Content.Recommend), "IsRecommend" }));
                    var isHot = TranslateUtils.ToBool(AtomUtility.GetDcElementContent(entry.AdditionalElements, new List<string> { nameof(Content.Hot), "IsHot" }));
                    var isColor = TranslateUtils.ToBool(AtomUtility.GetDcElementContent(entry.AdditionalElements, new List<string> { nameof(Content.Color), "IsColor" }));
                    var linkUrl = AtomUtility.Decrypt(AtomUtility.GetDcElementContent(entry.AdditionalElements, ContentAttribute.LinkUrl));
                    var addDate = AtomUtility.GetDcElementContent(entry.AdditionalElements, ContentAttribute.AddDate);

                    var topTaxis = 0;
                    if (isTop)
                    {
                        topTaxis = taxis - 1;
                        taxis = await DataProvider.ContentRepository.GetMaxTaxisAsync(_site, channel, true) + 1;
                    }
                    var tags = AtomUtility.Decrypt(AtomUtility.GetDcElementContent(entry.AdditionalElements, new List<string> { nameof(Content.TagNames), "Tags" }));

                    var contentInfo = new Content
                    {
                        SiteId = _site.Id,
                        ChannelId = channel.Id,
                        AdminId = adminId,
                        LastEditAdminId = adminId,
                        UserId = userId,
                        SourceId = sourceId,
                        AddDate = TranslateUtils.ToDateTime(addDate),
                        LastEditDate = TranslateUtils.ToDateTime(lastEditDate),
                        GroupNames = Utilities.GetStringList(groupNameCollection),
                        TagNames = Utilities.GetStringList(tags),
                        Checked = isChecked,
                        CheckedLevel = checkedLevel,
                        Hits = hits,
                        HitsByDay = hitsByDay,
                        HitsByWeek = hitsByWeek,
                        HitsByMonth = hitsByMonth,
                        LastHitsDate = TranslateUtils.ToDateTime(lastHitsDate),
                        Downloads = downloads,
                        Title = AtomUtility.Decrypt(title),
                        Top = isTop,
                        Recommend = isRecommend,
                        Hot = isHot,
                        Color = isColor,
                        LinkUrl = linkUrl
                    };

                    var attributes = AtomUtility.GetDcElementNameValueCollection(entry.AdditionalElements);
                    foreach (string attributeName in attributes.Keys)
                    {
                        if (!contentInfo.ContainsKey(attributeName.ToLower()))
                        {
                            contentInfo.Set(attributeName, AtomUtility.Decrypt(attributes[attributeName]));
                        }
                    }

                    var isInsert = false;
                    if (isOverride)
                    {
                        var existsIDs = DataProvider.ContentRepository.GetIdListBySameTitle(tableName, contentInfo.ChannelId, contentInfo.Title);
                        if (existsIDs.Count > 0)
                        {
                            foreach (int id in existsIDs)
                            {
                                contentInfo.Id = id;
                                await DataProvider.ContentRepository.UpdateAsync(_site, channel, contentInfo);
                            }
                        }
                        else
                        {
                            isInsert = true;
                        }
                    }
                    else
                    {
                        isInsert = true;
                    }

                    if (isInsert)
                    {
                        var contentId = await DataProvider.ContentRepository.InsertWithTaxisAsync(_site, channel, contentInfo, taxis);

                        contentIdList.Add(contentId);

                        if (!string.IsNullOrEmpty(tags))
                        {
                            foreach (var tagName in Utilities.GetStringList(tags))
                            {
                                await DataProvider.ContentTagRepository.InsertAsync(_site.Id, tagName);
                            }
                        }
                    }

                    if (isTop)
                    {
                        taxis = topTaxis;
                    }
                }
                catch
                {
                    // ignored
                }
            }

            return contentIdList;
        }

        public async Task<bool> ExportContentsAsync(Site site, int channelId, IEnumerable<int> contentIdList, bool isPeriods, string dateFrom, string dateTo, ETriState checkedState)
        {
            var filePath = _siteContentDirectoryPath + PathUtils.SeparatorChar + "contents.xml";
            var channelInfo = await DataProvider.ChannelRepository.GetAsync(channelId);
            var feed = AtomUtility.GetEmptyFeed();

            if (contentIdList == null)
            {
                var tableName = await DataProvider.ChannelRepository.GetTableNameAsync(site, channelInfo);
                contentIdList = await DataProvider.ContentRepository.GetContentIdListAsync(tableName, channelId, isPeriods, dateFrom, dateTo, checkedState);
            }
            if (!contentIdList.Any()) return false;

            var collection = new NameValueCollection();

            foreach (var contentId in contentIdList)
            {
                var contentInfo = await DataProvider.ContentRepository.GetAsync(site, channelInfo, contentId);
                try
                {
                    ContentUtility.PutImagePaths(site, contentInfo, collection);
                }
                catch
                {
                    // ignored
                }
                var entry = ExportContentInfo(contentInfo);
                feed.Entries.Add(entry);
            }
            feed.Save(filePath);

            foreach (string imageUrl in collection.Keys)
            {
                var sourceFilePath = collection[imageUrl];
                var destFilePath = PathUtility.MapPath(_siteContentDirectoryPath, imageUrl);
                DirectoryUtils.CreateDirectoryIfNotExists(destFilePath);
                FileUtils.MoveFile(sourceFilePath, destFilePath, true);
            }

            return true;
        }

        public bool ExportContents(Site site, List<Content> contentInfoList)
        {
            var filePath = _siteContentDirectoryPath + PathUtils.SeparatorChar + "contents.xml";
            var feed = AtomUtility.GetEmptyFeed();

            var collection = new NameValueCollection();

            foreach (var contentInfo in contentInfoList)
            {
                try
                {
                    ContentUtility.PutImagePaths(site, contentInfo, collection);
                }
                catch
                {
                    // ignored
                }
                var entry = ExportContentInfo(contentInfo);
                feed.Entries.Add(entry);
            }

            feed.Save(filePath);

            foreach (string imageUrl in collection.Keys)
            {
                var sourceFilePath = collection[imageUrl];
                var destFilePath = PathUtility.MapPath(_siteContentDirectoryPath, imageUrl);
                DirectoryUtils.CreateDirectoryIfNotExists(destFilePath);
                FileUtils.MoveFile(sourceFilePath, destFilePath, true);
            }

            return true;
        }

        public AtomEntry ExportContentInfo(Content content)
        {
            var entry = AtomUtility.GetEmptyEntry();

            AtomUtility.AddDcElement(entry.AdditionalElements, ContentAttribute.Id, content.Id.ToString());
            AtomUtility.AddDcElement(entry.AdditionalElements, new List<string>{ ContentAttribute.ChannelId, "NodeId" }, content.ChannelId.ToString());
            AtomUtility.AddDcElement(entry.AdditionalElements, new List<string> { ContentAttribute.SiteId, "PublishmentSystemId" }, content.SiteId.ToString());
            if (content.LastEditDate.HasValue)
            {
                AtomUtility.AddDcElement(entry.AdditionalElements, ContentAttribute.LastEditDate, content.LastEditDate.Value.ToString(CultureInfo.InvariantCulture));
            }
            AtomUtility.AddDcElement(entry.AdditionalElements, ContentAttribute.Taxis, content.Taxis.ToString());
            AtomUtility.AddDcElement(entry.AdditionalElements, new List<string>{ nameof(Content.GroupNames), "GroupNameCollection", "ContentGroupNameCollection" }, Utilities.ToString(content.GroupNames));
            AtomUtility.AddDcElement(entry.AdditionalElements, new List<string> { nameof(Content.TagNames), "Tags" }, AtomUtility.Encrypt(Utilities.ToString(content.TagNames)));
            AtomUtility.AddDcElement(entry.AdditionalElements, ContentAttribute.SourceId, content.SourceId.ToString());
            AtomUtility.AddDcElement(entry.AdditionalElements, ContentAttribute.ReferenceId, content.ReferenceId.ToString());
            AtomUtility.AddDcElement(entry.AdditionalElements, new List<string> { nameof(ContentAttribute.Checked), "IsChecked" }, content.Checked.ToString());
            AtomUtility.AddDcElement(entry.AdditionalElements, ContentAttribute.CheckedLevel, content.CheckedLevel.ToString());
            AtomUtility.AddDcElement(entry.AdditionalElements, ContentAttribute.Hits, content.Hits.ToString());
            AtomUtility.AddDcElement(entry.AdditionalElements, ContentAttribute.HitsByDay, content.HitsByDay.ToString());
            AtomUtility.AddDcElement(entry.AdditionalElements, ContentAttribute.HitsByWeek, content.HitsByWeek.ToString());
            AtomUtility.AddDcElement(entry.AdditionalElements, ContentAttribute.HitsByMonth, content.HitsByMonth.ToString());
            if (content.LastHitsDate.HasValue)
            {
                AtomUtility.AddDcElement(entry.AdditionalElements, ContentAttribute.LastHitsDate,
                    content.LastHitsDate.Value.ToString(CultureInfo.InvariantCulture));
            }

            AtomUtility.AddDcElement(entry.AdditionalElements, ContentAttribute.Downloads, content.Downloads.ToString());
            AtomUtility.AddDcElement(entry.AdditionalElements, ContentAttribute.Title, AtomUtility.Encrypt(content.Title));
            AtomUtility.AddDcElement(entry.AdditionalElements, new List<string> { nameof(Content.Top), "IsTop" }, content.Top.ToString());
            AtomUtility.AddDcElement(entry.AdditionalElements, new List<string> { nameof(Content.Recommend), "IsRecommend" }, content.Recommend.ToString());
            AtomUtility.AddDcElement(entry.AdditionalElements, new List<string> { nameof(Content.Hot), "IsHot" }, content.Hot.ToString());
            AtomUtility.AddDcElement(entry.AdditionalElements, new List<string> { nameof(Content.Color), "IsColor" }, content.Color.ToString());
            AtomUtility.AddDcElement(entry.AdditionalElements, ContentAttribute.LinkUrl, AtomUtility.Encrypt(content.LinkUrl));
            if (content.AddDate.HasValue)
            {
                AtomUtility.AddDcElement(entry.AdditionalElements, ContentAttribute.AddDate,
                    content.AddDate.Value.ToString(CultureInfo.InvariantCulture));
            }

            foreach (var attributeName in content.ToDictionary().Keys)
            {
                if (!StringUtils.ContainsIgnoreCase(ContentAttribute.AllAttributes.Value, attributeName))
                {
                    AtomUtility.AddDcElement(entry.AdditionalElements, attributeName, AtomUtility.Encrypt(content.Get<string>(attributeName)));
                }
            }

            return entry;
        }
    }
}
