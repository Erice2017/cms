﻿using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Threading.Tasks;
using System.Web;
using SiteServer.Abstractions;
using SiteServer.BackgroundPages.Cms;
using SiteServer.CMS.Context;
using SiteServer.CMS.Core;
using SiteServer.CMS.Dto.Request;
using SiteServer.CMS.Repositories;

namespace SiteServer.API.Controllers.Pages
{
    public partial class PagesMainController
    {
        public class Local
        {
            public int UserId { get; set; }
            public string UserName { get; set; }
            public string AvatarUrl { get; set; }
            public string Level { get; set; }
        }

        public class GetResult
        {
            public bool Value { get; set; }
            public string RedirectUrl { get; set; }
            public string DefaultPageUrl { get; set; }
            public bool IsNightly { get; set; }
            public string ProductVersion { get; set; }
            public string PluginVersion { get; set; }
            public string TargetFramework { get; set; }
            public string EnvironmentVersion { get; set; }
            public string AdminLogoUrl { get; set; }
            public string AdminTitle { get; set; }
            public bool IsSuperAdmin { get; set; }
            public List<object> PackageList { get; set; }
            public List<string> PackageIds { get; set; }
            public List<Tab> Menus { get; set; }
            public string SiteUrl { get; set; }
            public string PreviewUrl { get; set; }
            public Local Local { get; set; }
        }

        public class CreateRequest
        {
            public string SessionId { get; set; }
        }

        public class DownloadRequest
        {
            public string PackageId { get; set; }
            public string Version { get; set; }
        }

        private static async Task<List<Tab>> GetTopMenusAsync(Site siteInfo, bool isSuperAdmin, List<int> siteIdListLatestAccessed, List<int> siteIdListWithPermissions, List<string> permissionList, List<Tab> siteMenus)
        {
            var menus = new List<Tab>();

            if (siteInfo != null && siteIdListWithPermissions.Contains(siteInfo.Id))
            {
                menus.Add(new Tab
                {
                    Id = Constants.TopMenu.SiteCurrent,
                    Text = siteInfo.SiteName,
                    Children = siteMenus.ToArray()
                });

                if (siteIdListWithPermissions.Count > 1)
                {
                    var allSiteMenus = new List<Tab>();

                    var siteIdList = await DataProvider.AdministratorRepository.GetLatestTop10SiteIdListAsync(siteIdListLatestAccessed, siteIdListWithPermissions);
                    foreach (var siteId in siteIdList)
                    {
                        var site = await DataProvider.SiteRepository.GetAsync(siteId);
                        if (site == null) continue;

                        allSiteMenus.Add(new Tab
                        {
                            Href = PageUtils.GetMainUrl(site.Id),
                            Target = "_top",
                            Text = site.SiteName
                        });
                    }
                    allSiteMenus.Add(new Tab
                    {
                        Href = ModalSiteSelect.GetRedirectUrl(siteInfo.Id),
                        Target = "_layer",
                        Text = "全部站点..."
                    });
                    menus.Add(new Tab
                    {
                        Id = Constants.TopMenu.SiteCurrent,
                        Text = "切换站点",
                        Href = ModalSiteSelect.GetRedirectUrl(siteInfo.Id),
                        Target = "_layer",
                        Children = allSiteMenus.ToArray()
                    });
                }
            }

            if (isSuperAdmin)
            {
                foreach (var tab in TabManager.GetTopMenuTabs())
                {
                    var tabs = await TabManager.GetTabListAsync(tab.Id, 0);
                    tab.Children = tabs.ToArray();

                    menus.Add(tab);
                }
            }
            else
            {
                foreach (var tab in TabManager.GetTopMenuTabs())
                {
                    if (!TabManager.IsValid(tab, permissionList)) continue;

                    var tabToAdd = new Tab
                    {
                        Id = tab.Id,
                        Name = tab.Name,
                        Text = tab.Text,
                        Target = tab.Target,
                        Href = tab.Href
                    };
                    var tabs = await TabManager.GetTabListAsync(tab.Id, 0);
                    var tabsToAdd = new List<Tab>();
                    foreach (var menu in tabs)
                    {
                        if (!TabManager.IsValid(menu, permissionList)) continue;

                        Tab[] children = null;
                        if (menu.Children != null)
                        {
                            children = menu.Children.Where(child => TabManager.IsValid(child, permissionList))
                                .ToArray();
                        }

                        tabsToAdd.Add(new Tab
                        {
                            Id = menu.Id,
                            Name = menu.Name,
                            Text = menu.Text,
                            Target = menu.Target,
                            Href = menu.Href,
                            Children = children
                        });
                    }
                    tabToAdd.Children = tabsToAdd.ToArray();

                    menus.Add(tabToAdd);
                }
            }

            return menus;
        }

        private static async Task<List<Tab>> GetLeftMenusAsync(Site site, string topId, bool isSuperAdmin, List<string> permissionList)
        {
            var menus = new List<Tab>();

            var tabs = await TabManager.GetTabListAsync(topId, site.Id);
            foreach (var parent in tabs)
            {
                if (!isSuperAdmin && !TabManager.IsValid(parent, permissionList)) continue;

                var children = new List<Tab>();
                if (parent.Children != null && parent.Children.Length > 0)
                {
                    var tabCollection = new TabCollection(parent.Children);
                    if (tabCollection.Tabs != null && tabCollection.Tabs.Length > 0)
                    {
                        foreach (var childTab in tabCollection.Tabs)
                        {
                            if (!isSuperAdmin && !TabManager.IsValid(childTab, permissionList)) continue;

                            children.Add(new Tab
                            {
                                Id = childTab.Id,
                                Href = GetHref(childTab, site.Id),
                                Text = childTab.Text,
                                Target = childTab.Target,
                                IconClass = childTab.IconClass
                            });
                        }
                    }
                }

                menus.Add(new Tab
                {
                    Id = parent.Id,
                    Href = GetHref(parent, site.Id),
                    Text = parent.Text,
                    Target = parent.Target,
                    IconClass = parent.IconClass,
                    Selected = parent.Selected,
                    Children = children.ToArray()
                });
            }

            return menus;
        }

        private static string GetHref(Tab tab, int siteId)
        {
            var href = tab.Href;
            if (!PageUtils.IsAbsoluteUrl(href))
            {
                href = PageUtils.AddQueryString(href,
                    new NameValueCollection { { "siteId", siteId.ToString() } });
            }

            return href;
        }
    }
}