﻿using System.Collections.Generic;
using SiteServer.CMS.Api.Sys.Stl;
using SiteServer.Abstractions;
using SiteServer.CMS.Core;
using SiteServer.CMS.StlParser.Model;
using SiteServer.CMS.StlParser.Utility;
using System.Threading.Tasks;
using Datory.Utils;
using SiteServer.CMS.Context;
using SiteServer.CMS.Repositories;

namespace SiteServer.CMS.StlParser.StlEntity
{
    [StlElement(Title = "内容实体", Description = "通过 {content.} 实体在模板中显示内容值")]
    public class StlContentEntities
    {
        private StlContentEntities()
        {
        }

        public const string EntityName = "content";

        public const string Id = "Id";
        public const string Title = "Title";
        public const string FullTitle = "FullTitle";
        public const string NavigationUrl = "NavigationUrl";
        public const string ImageUrl = "ImageUrl";
        public const string VideoUrl = "VideoUrl";
        public const string FileUrl = "FileUrl";
        public const string DownloadUrl = "DownloadUrl";
        public const string AddDate = "AddDate";
        public const string LastEditDate = "LastEditDate";
        public const string Content = "Body";
        public const string Group = "Group";
        public const string Tags = "Tags";
        public const string ItemIndex = "ItemIndex";

        public static SortedList<string, string> AttributeList => new SortedList<string, string>
        {
            {Id, "内容ID"},
            {Title, "内容标题"},
            {FullTitle, "内容标题全称"},
            {Content, "内容正文"},
            {NavigationUrl, "内容链接地址"},
            {ImageUrl, "内容图片地址"},
            {VideoUrl, "内容视频地址"},
            {FileUrl, "内容附件地址"},
            {DownloadUrl, "内容附件地址(可统计下载量)"},
            {AddDate, "内容添加日期"},
            {LastEditDate, "内容最后修改日期"},
            {Group, "内容组别"},
            {Tags, "内容标签"},
            {ItemIndex, "内容排序"}
        };

        internal static async Task<string> ParseAsync(string stlEntity, PageInfo pageInfo, ContextInfo contextInfo)
        {
            var parsedContent = string.Empty;

            if (contextInfo.ContentId != 0)
            {
                try
                {
                    var contentInfo = await contextInfo.GetContentAsync();

                    if (contentInfo != null && contentInfo.ReferenceId > 0 && contentInfo.SourceId > 0 && TranslateContentType.ReferenceContent == contentInfo.TranslateContentType)
                    {
                        var targetChannelId = contentInfo.SourceId;
                        var targetSiteId = await DataProvider.ChannelRepository.GetSiteIdAsync(targetChannelId);
                        var targetSite = await DataProvider.SiteRepository.GetAsync(targetSiteId);
                        var targetNodeInfo = await DataProvider.ChannelRepository.GetAsync(targetChannelId);

                        var targetContentInfo = await DataProvider.ContentRepository.GetAsync(targetSite, targetNodeInfo, contentInfo.ReferenceId);
                        if (targetContentInfo != null && targetContentInfo.ChannelId > 0)
                        {
                            //标题可以使用自己的
                            targetContentInfo.Title = contentInfo.Title;

                            contentInfo = targetContentInfo;
                        }
                    }

                    var entityName = StlParserUtility.GetNameFromEntity(stlEntity);
                    var attributeName = entityName.Substring(9, entityName.Length - 10);

                    if (StringUtils.EqualsIgnoreCase(ContentAttribute.Id, attributeName))//内容ID
                    {
                        if (contentInfo != null)
                        {
                            parsedContent = contentInfo.ReferenceId > 0 ? contentInfo.ReferenceId.ToString() : contentInfo.Id.ToString();
                        }
                        else
                        {
                            var tableName = await DataProvider.ChannelRepository.GetTableNameAsync(pageInfo.Site, await DataProvider.ChannelRepository.GetAsync(contextInfo.ChannelId));
                            parsedContent = await DataProvider.ContentRepository.GetValueAsync(tableName, contextInfo.ContentId, ContentAttribute.Id);
                        }
                    }
                    else if (StringUtils.EqualsIgnoreCase(Title, attributeName))//内容标题
                    {
                        if (contentInfo != null)
                        {
                            parsedContent = contentInfo.Title;
                        }
                        else
                        {
                            var tableName = await DataProvider.ChannelRepository.GetTableNameAsync(pageInfo.Site, await DataProvider.ChannelRepository.GetAsync(contextInfo.ChannelId));
                            parsedContent = await DataProvider.ContentRepository.GetValueAsync(tableName, contextInfo.ContentId, ContentAttribute.Title);
                        }
                    }
                    else if (StringUtils.EqualsIgnoreCase(FullTitle, attributeName))//内容标题全称
                    {
                        if (contentInfo != null)
                        {
                            parsedContent = contentInfo.Title;
                        }
                        else
                        {
                            var tableName = await DataProvider.ChannelRepository.GetTableNameAsync(pageInfo.Site, await DataProvider.ChannelRepository.GetAsync(contextInfo.ChannelId));
                            parsedContent = await DataProvider.ContentRepository.GetValueAsync(tableName, contextInfo.ContentId, ContentAttribute.Title);
                        }
                    }
                    else if (StringUtils.EqualsIgnoreCase(NavigationUrl, attributeName))//内容链接地址
                    {
                        if (contentInfo != null)
                        {
                            parsedContent = await PageUtility.GetContentUrlAsync(pageInfo.Site, contentInfo, pageInfo.IsLocal);
                        }
                        else
                        {
                            var nodeInfo = await DataProvider.ChannelRepository.GetAsync(contextInfo.ChannelId);
                            parsedContent = await PageUtility.GetContentUrlAsync(pageInfo.Site, nodeInfo, contextInfo.ContentId, pageInfo.IsLocal);
                        }
                    }
                    else if (StringUtils.EqualsIgnoreCase(ImageUrl, attributeName))//内容图片地址
                    {
                        if (contentInfo != null)
                        {
                            parsedContent = contentInfo.Get<string>(ContentAttribute.ImageUrl);
                        }
                        else
                        {
                            var tableName = await DataProvider.ChannelRepository.GetTableNameAsync(pageInfo.Site, await DataProvider.ChannelRepository.GetAsync(contextInfo.ChannelId));
                            parsedContent = await DataProvider.ContentRepository.GetValueAsync(tableName, contextInfo.ContentId, ContentAttribute.ImageUrl);
                        }

                        if (!string.IsNullOrEmpty(parsedContent))
                        {
                            parsedContent = PageUtility.ParseNavigationUrl(pageInfo.Site, parsedContent, pageInfo.IsLocal);
                        }
                    }
                    else if (StringUtils.EqualsIgnoreCase(VideoUrl, attributeName))//内容视频地址
                    {
                        if (contentInfo != null)
                        {
                            parsedContent = contentInfo.Get<string>(ContentAttribute.VideoUrl);
                        }
                        else
                        {
                            var tableName = await DataProvider.ChannelRepository.GetTableNameAsync(pageInfo.Site, await DataProvider.ChannelRepository.GetAsync(contextInfo.ChannelId));
                            parsedContent = await DataProvider.ContentRepository.GetValueAsync(tableName, contextInfo.ContentId, ContentAttribute.VideoUrl);
                        }

                        if (!string.IsNullOrEmpty(parsedContent))
                        {
                            parsedContent = PageUtility.ParseNavigationUrl(pageInfo.Site, parsedContent, pageInfo.IsLocal);
                        }
                    }
                    else if (StringUtils.EqualsIgnoreCase(FileUrl, attributeName))//内容附件地址
                    {
                        if (contentInfo != null)
                        {
                            parsedContent = contentInfo.Get<string>(ContentAttribute.FileUrl);
                        }
                        else
                        {
                            var tableName = await DataProvider.ChannelRepository.GetTableNameAsync(pageInfo.Site, await DataProvider.ChannelRepository.GetAsync(contextInfo.ChannelId));
                            parsedContent = await DataProvider.ContentRepository.GetValueAsync(tableName, contextInfo.ContentId, ContentAttribute.FileUrl);
                        }

                        if (!string.IsNullOrEmpty(parsedContent))
                        {
                            parsedContent = PageUtility.ParseNavigationUrl(pageInfo.Site, parsedContent, pageInfo.IsLocal);
                        }
                    }
                    else if (StringUtils.EqualsIgnoreCase(DownloadUrl, attributeName))//内容附件地址(可统计下载量)
                    {
                        if (contentInfo != null)
                        {
                            parsedContent = contentInfo.Get<string>(ContentAttribute.FileUrl);
                        }
                        else
                        {
                            var tableName = await DataProvider.ChannelRepository.GetTableNameAsync(pageInfo.Site, await DataProvider.ChannelRepository.GetAsync(contextInfo.ChannelId));
                            parsedContent = await DataProvider.ContentRepository.GetValueAsync(tableName, contextInfo.ContentId, ContentAttribute.FileUrl);
                        }

                        if (!string.IsNullOrEmpty(parsedContent))
                        {
                            parsedContent = ApiRouteActionsDownload.GetUrl(pageInfo.ApiUrl, pageInfo.SiteId, contextInfo.ChannelId, contextInfo.ContentId, parsedContent);
                        }
                    }
                    else if (StringUtils.EqualsIgnoreCase(AddDate, attributeName))//内容添加日期
                    {
                        if (contentInfo != null)
                        {
                            parsedContent = DateUtils.Format(contentInfo.AddDate, string.Empty);
                        }
                    }
                    else if (StringUtils.EqualsIgnoreCase(LastEditDate, attributeName))//替换最后修改日期
                    {
                        if (contentInfo != null)
                        {
                            parsedContent = DateUtils.Format(contentInfo.LastEditDate, string.Empty);
                        }
                    }
                    else if (StringUtils.EqualsIgnoreCase(Content, attributeName))//内容正文
                    {
                        if (contentInfo != null)
                        {
                            parsedContent = contentInfo.Get<string>(ContentAttribute.Content);
                        }
                        else
                        {
                            var tableName = await DataProvider.ChannelRepository.GetTableNameAsync(pageInfo.Site, await DataProvider.ChannelRepository.GetAsync(contextInfo.ChannelId));
                            parsedContent = await DataProvider.ContentRepository.GetValueAsync(tableName, contextInfo.ContentId, ContentAttribute.Content);
                        }

                        parsedContent = ContentUtility.TextEditorContentDecode(pageInfo.Site, parsedContent, pageInfo.IsLocal);
                    }
                    else if (StringUtils.EqualsIgnoreCase(Group, attributeName))//内容组别
                    {
                        if (contentInfo != null)
                        {
                            parsedContent = Utilities.ToString(contentInfo.GroupNames);
                        }
                        else
                        {
                            var tableName = await DataProvider.ChannelRepository.GetTableNameAsync(pageInfo.Site, await DataProvider.ChannelRepository.GetAsync(contextInfo.ChannelId));
                            parsedContent = await DataProvider.ContentRepository.GetValueAsync(tableName, contextInfo.ContentId, nameof(Abstractions.Content.GroupNames));
                        }
                    }
                    else if (StringUtils.EqualsIgnoreCase(Tags, attributeName))//标签
                    {
                        if (contentInfo != null)
                        {
                            parsedContent = Utilities.ToString(contentInfo.TagNames);
                        }
                        else
                        {
                            var tableName = await DataProvider.ChannelRepository.GetTableNameAsync(pageInfo.Site, await DataProvider.ChannelRepository.GetAsync(contextInfo.ChannelId));
                            parsedContent = await DataProvider.ContentRepository.GetValueAsync(tableName, contextInfo.ContentId, nameof(Abstractions.Content.TagNames));
                        }
                    }
                    else if (StringUtils.StartsWithIgnoreCase(attributeName, StlParserUtility.ItemIndex) && contextInfo.ItemContainer?.ContentItem != null)
                    {
                        parsedContent = StlParserUtility.ParseItemIndex(contextInfo.ItemContainer.ContentItem.ItemIndex, attributeName, contextInfo).ToString();
                    }
                    else
                    {
                        int contentChannelId;
                        if (contentInfo != null)
                        {
                            contentChannelId = contentInfo.ChannelId;
                            if (contentInfo.ContainsKey(attributeName))
                            {
                                parsedContent = contentInfo.Get<string>(attributeName);
                            }
                        }
                        else
                        {
                            var tableName = await DataProvider.ChannelRepository.GetTableNameAsync(pageInfo.Site, contextInfo.ChannelId);
                            contentChannelId = DataProvider.ContentRepository.GetChannelId(tableName, contextInfo.ContentId);
                            tableName = await DataProvider.ChannelRepository.GetTableNameAsync(pageInfo.Site, await DataProvider.ChannelRepository.GetAsync(contentChannelId));
                            parsedContent = await DataProvider.ContentRepository.GetValueAsync(tableName, contextInfo.ContentId, attributeName);
                        }

                        if (!string.IsNullOrEmpty(parsedContent))
                        {
                            var channelInfo = await DataProvider.ChannelRepository.GetAsync(contentChannelId);
                            var tableName = await DataProvider.ChannelRepository.GetTableNameAsync(pageInfo.Site, channelInfo);
                            var relatedIdentities = DataProvider.TableStyleRepository.GetRelatedIdentities(channelInfo);
                            var styleInfo = await DataProvider.TableStyleRepository.GetTableStyleAsync(tableName, attributeName, relatedIdentities);

                            //styleInfo.IsVisible = false 表示此字段不需要显示 styleInfo.TableStyleId = 0 不能排除，因为有可能是直接辅助表字段没有添加显示样式
                            parsedContent = await InputParserUtility.GetContentByTableStyleAsync(parsedContent, ",", pageInfo.Site, styleInfo, string.Empty, null, string.Empty, true);
                        }

                    }
                }
                catch
                {
                    // ignored
                }
            }

            parsedContent = parsedContent.Replace(ContentUtility.PagePlaceHolder, string.Empty);

            return parsedContent;
        }
    }
}
