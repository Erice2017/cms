﻿using System.Collections.Generic;
using System.Threading.Tasks;
using System.Web.Http;
using SiteServer.Abstractions;
using SiteServer.CMS.Context.Enumerations;
using SiteServer.CMS.Core;
using SiteServer.CMS.Core.Create;
using SiteServer.CMS.Extensions;
using SiteServer.CMS.Repositories;

namespace SiteServer.API.Controllers.Pages.Cms.Channels
{
    [RoutePrefix("pages/cms/channels/channelsLayerCreate")]
    public partial class PagesChannelsLayerCreateController : ApiController
    {
        private const string Route = "";

        [HttpPost, Route(Route)]
        public async Task<List<int>> Create([FromBody] CreateRequest request)
        {
            var auth = await AuthenticatedRequest.GetAuthAsync();
            await auth.CheckSitePermissionsAsync(Request, request.SiteId, Constants.SitePermissions.Channels);

            var site = await DataProvider.SiteRepository.GetAsync(request.SiteId);
            if (site == null) return Request.NotFound<List<int>>();

            var expendedChannelIds = new List<int>
            {
                request.SiteId
            };

            foreach (var channelId in request.ChannelIds)
            {
                var channel = await DataProvider.ChannelRepository.GetAsync(channelId);
                if (!expendedChannelIds.Contains(channel.ParentId))
                {
                    expendedChannelIds.Add(channel.ParentId);
                }

                await CreateManager.CreateChannelAsync(request.SiteId, channelId);
                if (request.IsCreateContents)
                {
                    await CreateManager.CreateAllContentAsync(request.SiteId, channelId);
                }
                if (request.IsIncludeChildren)
                {
                    var channelIds = await DataProvider.ChannelRepository.GetChannelIdsAsync(channel, EScopeType.Descendant);

                    foreach (var childChannelId in channelIds)
                    {
                        await CreateManager.CreateChannelAsync(request.SiteId, childChannelId);
                        if (request.IsCreateContents)
                        {
                            await CreateManager.CreateAllContentAsync(request.SiteId, channelId);
                        }
                    }
                }
            }

            return expendedChannelIds;
        }
    }
}
