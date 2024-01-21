using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using DSharpPlus.Entities;

namespace Orpheus.ApiStuff
{
    public static class OrpheusAPIHandler
    {
        private static List<ApiServerInfo> TrackedServers = new List<ApiServerInfo>();

        public static async Task<DiscordMember> GetMemberAsync(
            DiscordGuild discordGuild,
            ulong userID
        )
        {
            foreach (ApiServerInfo info in TrackedServers)
            {
                if (info.trackedServer.Id == discordGuild.Id)
                {
                    return info.GetMember(userID);
                }
            }
            //new server to track
            ApiServerInfo newServerTrack = await AddServerToTracking(discordGuild);
            return newServerTrack.GetMember(userID);
        }

        public static async Task<DiscordRole> GetRoleAsync(
            DiscordGuild discordGuild,
            ulong roleID)
        {
            foreach (ApiServerInfo info in TrackedServers)
            {
                if (info.trackedServer.Id == discordGuild.Id)
                {
                    return info.GetRole(roleID);
                }
            }
            //new server to track
            ApiServerInfo newServerTrack = await AddServerToTracking(discordGuild);
            return newServerTrack.GetRole(roleID);
        }

        public static async Task<ApiServerInfo> AddServerToTracking(DiscordGuild discordGuild)
        {
            ApiServerInfo temp = new ApiServerInfo();
            await temp.StartTrack(discordGuild);
            TrackedServers.Add(temp);
            return temp;
        }
    }
}
