﻿using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Threading.Tasks;
using System.Web.UI.WebControls;
using Datory.Utils;
using SiteServer.Abstractions;
using SiteServer.CMS.Api;
using SiteServer.CMS.Api.Sys.Stl;
using SiteServer.CMS.Context;
using SiteServer.CMS.Context.Enumerations;
using SiteServer.CMS.Core.Office;
using SiteServer.CMS.DataCache;
using SiteServer.CMS.ImportExport;
using SiteServer.CMS.Repositories;

namespace SiteServer.BackgroundPages.Cms
{
    public class ModalExportMessage : BasePageCms
    {
        public const int Width = 580;
        public const int Height = 250;
        public const string ExportTypeContentZip = "ContentZip";
        public const string ExportTypeContentExcel = "ContentExcel";
        public const string ExportTypeContentTxtZip = "ContentTxtZip";
        public const string ExportTypeRelatedField = "RelatedField";
        public const string ExportTypeChannel = "Channel";
        public const string ExportTypeSingleTableStyle = "SingleTableStyle";

        private string _exportType;

        public static string GetRedirectUrlStringToExportContent(int siteId, int channelId,
            string exportType, string contentIdCollection, string displayAttributes, bool isPeriods,
            string startDate, string endDate, ETriState checkedState)
        {
            return PageUtils.GetCmsUrl(siteId, nameof(ModalExportMessage), new NameValueCollection
                    {
                        {"channelId", channelId.ToString()},
                        {"ExportType", exportType},
                        {"contentIdCollection", contentIdCollection},
                        {"DisplayAttributes", displayAttributes},
                        {"isPeriods", isPeriods.ToString()},
                        {"startDate", startDate},
                        {"endDate", endDate},
                        {"checkedState", ETriStateUtils.GetValue(checkedState)}
                    });
        }

        public static string GetOpenWindowStringToChannel(int siteId, string checkBoxId, string alertString)
        {
            return LayerUtils.GetOpenScriptWithCheckBoxValue("导出数据",
                PageUtils.GetCmsUrl(siteId, nameof(ModalExportMessage), new NameValueCollection
                {
                    {"ExportType", ExportTypeChannel}
                }), checkBoxId, alertString, Width, Height);
        }

        public static string GetOpenWindowStringToSingleTableStyle(string tableName, int siteId, int relatedIdentity)
        {
            return LayerUtils.GetOpenScript("导出数据",
                PageUtils.GetCmsUrl(siteId, nameof(ModalExportMessage), new NameValueCollection
                {
                    {"TableName", tableName},
                    {"ExportType", ExportTypeSingleTableStyle},
                    {"RelatedIdentity", relatedIdentity.ToString()}
                }), Width, Height);
        }

        public static string GetOpenWindowStringToRelatedField(int siteId, int relatedFieldId)
        {
            return LayerUtils.GetOpenScript("导出数据",
                PageUtils.GetCmsUrl(siteId, nameof(ModalExportMessage), new NameValueCollection
                {
                    {"RelatedFieldID", relatedFieldId.ToString()},
                    {"ExportType", ExportTypeRelatedField}
                }), Width, Height);
        }

        public static string GetOpenWindowStringToExport(int siteId, string exportType)
        {
            return LayerUtils.GetOpenScript("导出数据",
                PageUtils.GetCmsUrl(siteId, nameof(ModalExportMessage), new NameValueCollection
                {
                    {"ExportType", exportType}
                }), Width, Height);
        }

        public void Page_Load(object sender, EventArgs e)
        {
            if (IsForbidden) return;

            _exportType = AuthRequest.GetQueryString("ExportType");

            if (!IsPostBack)
            {
                var isExport = true;
                var fileName = string.Empty;
                try
                {
                    //if (_exportType == ExportTypeRelatedField)
                    //{
                    //    var relatedFieldId = AuthRequest.GetQueryInt("RelatedFieldID");
                    //    fileName = ExportRelatedFieldAsync(relatedFieldId).GetAwaiter().GetResult();
                    //}
                    if (_exportType == ExportTypeContentZip)
                    {
                        var channelId = AuthRequest.GetQueryInt("channelId");
                        var contentIdCollection = Utilities.GetIntList(AuthRequest.GetQueryString("contentIdCollection"));
                        var isPeriods = AuthRequest.GetQueryBool("isPeriods");
                        var startDate = AuthRequest.GetQueryString("startDate");
                        var endDate = AuthRequest.GetQueryString("endDate");
                        var checkedState = ETriStateUtils.GetEnumType(AuthRequest.GetQueryString("checkedState"));
                        isExport = ExportContentZip(channelId, contentIdCollection, isPeriods, startDate, endDate, checkedState, out fileName);
                    }
                    else if (_exportType == ExportTypeContentExcel)
                    {
                        var channelId = AuthRequest.GetQueryInt("channelId");
                        var contentIdCollection = Utilities.GetIntList(AuthRequest.GetQueryString("contentIdCollection"));
                        var displayAttributes = Utilities.GetStringList(AuthRequest.GetQueryString("DisplayAttributes"));
                        var isPeriods = AuthRequest.GetQueryBool("isPeriods");
                        var startDate = AuthRequest.GetQueryString("startDate");
                        var endDate = AuthRequest.GetQueryString("endDate");
                        var checkedState = ETriStateUtils.GetEnumType(AuthRequest.GetQueryString("checkedState"));
                        ExportContentExcel(channelId, contentIdCollection, displayAttributes, isPeriods, startDate, endDate, checkedState, out fileName);
                    }
                    else if (_exportType == ExportTypeChannel)
                    {
                        var channelIdList = Utilities.GetIntList(AuthRequest.GetQueryString("ChannelIDCollection"));
                        fileName = ExportChannel(channelIdList);
                    }
                    else if (_exportType == ExportTypeSingleTableStyle)
                    {
                        var tableName = AuthRequest.GetQueryString("TableName");
                        var relatedIdentity = AuthRequest.GetQueryInt("RelatedIdentity");
                        fileName = ExportSingleTableStyle(tableName, relatedIdentity);
                    }

                    if (isExport)
                    {
                        var link = new HyperLink();
                        var filePath = PathUtils.GetTemporaryFilesPath(fileName);
                        link.NavigateUrl = ApiRouteActionsDownload.GetUrl(ApiManager.InnerApiUrl, filePath);
                        link.Text = "下载";
                        var successMessage = "成功导出文件！&nbsp;&nbsp;" + ControlUtils.GetControlRenderHtml(link);
                        SuccessMessage(successMessage);
                    }
                    else
                    {
                        FailMessage("导出失败，所选条件没有匹配内容，请重新选择条件导出内容");
                    }
                }
                catch (Exception ex)
                {
                    var failedMessage = "文件导出失败！<br/><br/>原因为：" + ex.Message;
                    FailMessage(ex, failedMessage);
                }
            }
        }

        //private async Task<string> ExportRelatedFieldAsync(int relatedFieldId)
        //{
        //    var exportObject = new ExportObject(SiteId, AuthRequest.AdminName);
        //    return await exportObject.ExportRelatedFieldAsync(relatedFieldId);
        //}

        private bool ExportContentZip(int channelId, List<int> contentIdArrayList, bool isPeriods, string dateFrom, string dateTo, ETriState checkedState, out string fileName)
        {
            var nodeInfo = DataProvider.ChannelRepository.GetAsync(channelId).GetAwaiter().GetResult();
            fileName = $"{nodeInfo.ChannelName}.zip";
            var filePath = PathUtils.GetTemporaryFilesPath(fileName);
            var exportObject = new ExportObject(SiteId, AuthRequest.AdminName);
            return exportObject.ExportContentsAsync(filePath, channelId, contentIdArrayList, isPeriods, dateFrom, dateTo, checkedState).GetAwaiter().GetResult();
        }

        private void ExportContentExcel(int channelId, List<int> contentIdList, List<string> displayAttributes, bool isPeriods, string dateFrom, string dateTo, ETriState checkedState, out string fileName)
        {
            var nodeInfo = DataProvider.ChannelRepository.GetAsync(channelId).GetAwaiter().GetResult();

            fileName = $"{nodeInfo.ChannelName}.csv";
            var filePath = PathUtils.GetTemporaryFilesPath(fileName);
            ExcelObject.CreateExcelFileForContentsAsync(filePath, Site, nodeInfo, contentIdList, displayAttributes, isPeriods, dateFrom, dateTo, checkedState).GetAwaiter().GetResult();
        }

        private string ExportChannel(List<int> channelIdList)
        {
            var exportObject = new ExportObject(SiteId, AuthRequest.AdminName);
            return exportObject.ExportChannelsAsync(channelIdList).GetAwaiter().GetResult();
        }

        private string ExportSingleTableStyle(string tableName, int relatedIdentity)
        {
            var exportObject = new ExportObject(SiteId, AuthRequest.AdminName);
            return exportObject.ExportSingleTableStyleAsync(tableName, relatedIdentity).GetAwaiter().GetResult();
        }
    }
}
