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
                await Task.Delay(5000);
                Members = (await trackedServer.GetAllMembersAsync()).ToArray();
                Roles = trackedServer.Roles.Values.ToArray();
            }
        }

        public DiscordMember GetMember(ulong id)
        {
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

        public DiscordRole GetRole(ulong id)
        {
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
