﻿using System;
using System.Collections.Generic;
using System.Data;
using System.Web.UI.WebControls;
using SiteServer.CMS.DataCache;
using SiteServer.Abstractions;
using SiteServer.CMS.StlParser.Model;
using SiteServer.CMS.StlParser.Utility;
using System.Threading.Tasks;
using SiteServer.CMS.Context;
using SiteServer.CMS.Context.Enumerations;
using SiteServer.CMS.Repositories;

namespace SiteServer.CMS.StlParser.StlElement
{
    [StlElement(Title = "栏目列表", Description = "通过 stl:channels 标签在模板中显示栏目列表")]
    public class StlChannels : StlListBase
    {
        public const string ElementName = "stl:channels";

        [StlAttribute(Title = "从所有栏目中选择")]
        public const string IsTotal = nameof(IsTotal);

        [StlAttribute(Title = "显示所有级别的子栏目")]
        public const string IsAllChildren = nameof(IsAllChildren);

        public static async Task<object> ParseAsync(PageInfo pageInfo, ContextInfo contextInfo)
        {
            var listInfo = await ListInfo.GetListInfoAsync(pageInfo, contextInfo, EContextType.Channel);

            var dataSource = await GetDataSourceAsync(pageInfo, contextInfo, listInfo);

            if (contextInfo.IsStlEntity)
            {
                return await ParseEntityAsync(pageInfo, dataSource);
            }

            return ParseElement(pageInfo, contextInfo, listInfo, dataSource);
        }

        public static async Task<DataSet> GetDataSourceAsync(PageInfo pageInfo, ContextInfo contextInfo, ListInfo listInfo)
        {
            var channelId = await StlDataUtility.GetChannelIdByLevelAsync(pageInfo.SiteId, contextInfo.ChannelId, listInfo.UpLevel, listInfo.TopLevel);

            channelId = await StlDataUtility.GetChannelIdByChannelIdOrChannelIndexOrChannelNameAsync(pageInfo.SiteId, channelId, listInfo.ChannelIndex, listInfo.ChannelName);

            var isTotal = TranslateUtils.ToBool(listInfo.Others.Get(IsTotal));

            if (TranslateUtils.ToBool(listInfo.Others.Get(IsAllChildren)))
            {
                listInfo.Scope = EScopeType.Descendant;
            }

            return await StlDataUtility.GetChannelsDataSourceAsync(pageInfo.SiteId, channelId, listInfo.GroupChannel, listInfo.GroupChannelNot, listInfo.IsImageExists, listInfo.IsImage, listInfo.StartNum, listInfo.TotalNum, listInfo.OrderByString, listInfo.Scope, isTotal, listInfo.Where);
        }

        private static string ParseElement(PageInfo pageInfo, ContextInfo contextInfo, ListInfo listInfo, DataSet dataSource)
        {
            var parsedContent = string.Empty;

            if (listInfo.Layout == ELayout.None)
            {
                var rptContents = new Repeater();

                if (!string.IsNullOrEmpty(listInfo.HeaderTemplate))
                {
                    rptContents.HeaderTemplate = new SeparatorTemplate(listInfo.HeaderTemplate);
                }
                if (!string.IsNullOrEmpty(listInfo.FooterTemplate))
                {
                    rptContents.FooterTemplate = new SeparatorTemplate(listInfo.FooterTemplate);
                }
                if (!string.IsNullOrEmpty(listInfo.SeparatorTemplate))
                {
                    rptContents.SeparatorTemplate = new SeparatorTemplate(listInfo.SeparatorTemplate);
                }
                if (!string.IsNullOrEmpty(listInfo.AlternatingItemTemplate))
                {
                    rptContents.AlternatingItemTemplate = new RepeaterTemplate(listInfo.AlternatingItemTemplate, listInfo.SelectedItems, listInfo.SelectedValues, listInfo.SeparatorRepeatTemplate, listInfo.SeparatorRepeat, pageInfo, EContextType.Channel, contextInfo);
                }

                rptContents.ItemTemplate = new RepeaterTemplate(listInfo.ItemTemplate, listInfo.SelectedItems,
                    listInfo.SelectedValues, listInfo.SeparatorRepeatTemplate, listInfo.SeparatorRepeat,
                    pageInfo, EContextType.Channel, contextInfo);

                rptContents.DataSource = dataSource;
                rptContents.DataBind();

                if (rptContents.Items.Count > 0)
                {
                    parsedContent = ControlUtils.GetControlRenderHtml(rptContents);
                }
            }
            else
            {
                var pdlContents = new ParsedDataList();

                //设置显示属性
                TemplateUtility.PutListInfoToMyDataList(pdlContents, listInfo);

                //设置列表模板
                pdlContents.ItemTemplate = new DataListTemplate(listInfo.ItemTemplate, listInfo.SelectedItems, listInfo.SelectedValues, listInfo.SeparatorRepeatTemplate, listInfo.SeparatorRepeat, pageInfo, EContextType.Channel, contextInfo);
                if (!string.IsNullOrEmpty(listInfo.HeaderTemplate))
                {
                    pdlContents.HeaderTemplate = new SeparatorTemplate(listInfo.HeaderTemplate);
                }
                if (!string.IsNullOrEmpty(listInfo.FooterTemplate))
                {
                    pdlContents.FooterTemplate = new SeparatorTemplate(listInfo.FooterTemplate);
                }
                if (!string.IsNullOrEmpty(listInfo.SeparatorTemplate))
                {
                    pdlContents.SeparatorTemplate = new SeparatorTemplate(listInfo.SeparatorTemplate);
                }
                if (!string.IsNullOrEmpty(listInfo.AlternatingItemTemplate))
                {
                    pdlContents.AlternatingItemTemplate = new DataListTemplate(listInfo.AlternatingItemTemplate, listInfo.SelectedItems, listInfo.SelectedValues, listInfo.SeparatorRepeatTemplate, listInfo.SeparatorRepeat, pageInfo, EContextType.Channel, contextInfo);
                }

                pdlContents.DataSource = dataSource;
                pdlContents.DataKeyField = nameof(Channel.Id);
                pdlContents.DataBind();

                if (pdlContents.Items.Count > 0)
                {
                    parsedContent = ControlUtils.GetControlRenderHtml(pdlContents);
                }
            }

            return parsedContent;
        }

        private static async Task<object> ParseEntityAsync(PageInfo pageInfo, DataSet dataSource)
        {
            var channelInfoList = new List<IDictionary<string,object>>();
            var table = dataSource.Tables[0];
            foreach (DataRow row in table.Rows)
            {
                var channelId = Convert.ToInt32(row[nameof(ContentAttribute.Id)]);

                var channelInfo = await DataProvider.ChannelRepository.GetAsync(channelId);
                if (channelInfo != null)
                {
                    channelInfoList.Add(channelInfo.ToDictionary());
                }
            }

            return channelInfoList;
        }
    }
}
