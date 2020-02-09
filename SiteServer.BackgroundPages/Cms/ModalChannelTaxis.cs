﻿using System;
using System.Collections.Generic;
using System.Web.UI.WebControls;
using Datory.Utils;
using SiteServer.Abstractions;
using SiteServer.CMS.Context;
using SiteServer.CMS.DataCache;
using SiteServer.CMS.Repositories;

namespace SiteServer.BackgroundPages.Cms
{
    public class ModalChannelTaxis : BasePageCms
    {
        protected DropDownList DdlTaxisType;
        protected TextBox TbTaxisNum;

        private List<int> _channelIdList;

        public static string GetOpenWindowString(int siteId)
        {
            return LayerUtils.GetOpenScriptWithCheckBoxValue("栏目排序", PageUtils.GetCmsUrl(siteId, nameof(ModalChannelTaxis), null), "ChannelIDCollection", "请选择需要排序的栏目！", 400, 280);
        }

        public void Page_Load(object sender, EventArgs e)
        {
            if (IsForbidden) return;

            PageUtils.CheckRequestParameter("siteId", "ChannelIDCollection");

            _channelIdList = Utilities.GetIntList(AuthRequest.GetQueryString("channelIDCollection"));

            if (IsPostBack) return;

            DdlTaxisType.Items.Add(new ListItem("上升", "Up"));
            DdlTaxisType.Items.Add(new ListItem("下降", "Down"));
            ControlUtils.SelectSingleItem(DdlTaxisType, "Up");
        }

        public override void Submit_OnClick(object sender, EventArgs e)
        {
            var isSubtract = DdlTaxisType.SelectedValue == "Up";
            var taxisNum = TranslateUtils.ToInt(TbTaxisNum.Text);

            foreach (var channelId in _channelIdList)
            {
                for (var num = 0; num < taxisNum; num++)
                {
                    DataProvider.ChannelRepository.UpdateTaxisAsync(SiteId, channelId, isSubtract).GetAwaiter().GetResult();
                }

                AuthRequest.AddSiteLogAsync(SiteId, channelId, 0, "栏目排序" + (isSubtract ? "上升" : "下降"), $"栏目:{DataProvider.ChannelRepository.GetChannelNameAsync(SiteId, channelId).GetAwaiter().GetResult()}").GetAwaiter().GetResult();
            }
            LayerUtils.Close(Page);
        }

    }
}
