using DSharpPlus.CommandsNext;
using DSharpPlus.Entities;
using Orpheus.Database;

namespace Orpheus.JailHandling
{
    public static class JailCommands
    {
        public static async Task Jail(CommandContext ctx, DiscordMember user)
        {
            ulong JailRoleID = await OrpheusDatabaseHandler.GetJailIDInfo(
                ctx.Guild.Id,
                "jailroleid"
            );
            ulong CourtRoleID = await OrpheusDatabaseHandler.GetJailIDInfo(
                ctx.Guild.Id,
                "jailcourtroleid"
            );
            if (JailRoleID == 0)
            {
                await ctx.Channel.SendMessageAsync("Send Failed; JailRole has not been registered");
                return;
            }
            if (CourtRoleID == 0)
            {
                //await ctx.Channel.SendMessageAsync("Send Court Failed; JailCourtRole has not been registered");
                Console.WriteLine("Send Court Failed; JailCourtRole has not been registered");
            }
            DiscordRole jailrole = await ApiStuff.OrpheusAPIHandler.GetRoleAsync(ctx.Guild, JailRoleID);
            DiscordRole jailCourtrole = await ApiStuff.OrpheusAPIHandler.GetRoleAsync(ctx.Guild, CourtRoleID);
            try
            {
                await user.GrantRoleAsync(jailrole);
            }
            catch (Exception e)
            {
                Console.WriteLine(e.ToString());
            }

            try
            {
                await user.GrantRoleAsync(jailCourtrole);
            }
            catch (Exception e)
            {
                Console.WriteLine(e.ToString());
            }
            await ctx.Channel.SendMessageAsync($"{user.Username} has been sent to jail!");
            await Task.Delay(250);
            await ctx.Message.DeleteAsync();
        }

        public static async Task JailFREE(CommandContext ctx, DiscordMember user)
        {
            ulong JailRoleID = await OrpheusDatabaseHandler.GetJailIDInfo(
                ctx.Guild.Id,
                "jailroleid"
            );
            ulong JailCourtRoleID = await OrpheusDatabaseHandler.GetJailIDInfo(
                ctx.Guild.Id,
                "jailcourtroleid"
            );
            if (JailRoleID == 0)
            {
                await ctx.Channel.SendMessageAsync("Free Failed; JailRole has not been registered");
                return;
            }
            if (JailCourtRoleID == 0)
            {
                Console.WriteLine("Free Court Failed; JailCourtRole has not been registered");
            }
            DiscordRole jailrole = await ApiStuff.OrpheusAPIHandler.GetRoleAsync(ctx.Guild, JailRoleID);
            DiscordRole jailcourtrole = await ApiStuff.OrpheusAPIHandler.GetRoleAsync(ctx.Guild, JailCourtRoleID);
            await user.RevokeRoleAsync(jailrole);
            try
            {
                await user.RevokeRoleAsync(jailcourtrole);
            }
            catch (Exception e)
            {
                Console.WriteLine(e.ToString());
            }
            await ctx.Channel.SendMessageAsync($"{user.Username} has been freed from jail!");
            await Task.Delay(250);
            await ctx.Message.DeleteAsync();
        }
    }
}