using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using DSharpPlus.Entities;

namespace Orpheus.ApiStuff
{
    public class ApiServerInfo
    {
        public DiscordGuild trackedServer { get; private set; }
        DiscordMember[] Members { get; set; }
        DiscordRole[] Roles { get; set; }
        bool isStale = false;

        public async Task StartTrack(DiscordGuild discordGuild)
        {
            trackedServer = discordGuild;
            Members = (await trackedServer.GetAllMembersAsync()).ToArray();
            Roles = trackedServer.Roles.Values.ToArray();
            _ = UpdateTrackedData();
        }

        private async Task UpdateTrackedData()
        {
            while (true)
            {
                if (!isStale)
                {
                    await Task.Delay(10000);
                    isStale = true;
                }
            }
        }

        private async Task callUpdate()
        {
            Members = (await trackedServer.GetAllMembersAsync()).ToArray();
            Roles = trackedServer.Roles.Values.ToArray();
            isStale = false;
        }

        public async Task<DiscordMember> GetMember(ulong id)
        {
            if (isStale)
            {
                await callUpdate();
            }
            DiscordMember returner = null;
            foreach (DiscordMember mem in Members)
            {
                if (mem.Id == id)
                {
                    returner = mem;
                    break;
                }
            }

            return returner;
        }

        public async Task<DiscordRole> GetRole(ulong id)
        {
            if (isStale)
            {
                await callUpdate();
            }
            DiscordRole returner = null;
            foreach (DiscordRole mem in Roles)
            {
                if (mem.Id == id)
                {
                    returner = mem;
                    break;
                }
            }

            return returner;
        }
    }
}
