using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Threading.Tasks;
using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;
using DSharpPlus.Entities;
using Orpheus.Database;

namespace Orpheus.commands
{
    public class TestCommands : BaseCommandModule
    {
        [Command("add")]
        public async Task Add(CommandContext ctx, int numA, int numB)
        {
            if (ctx.Member == null || ctx.User.IsBot)
            {
                return;
            }
            int result = numA + numB;
            await ctx.Message.RespondAsync(result.ToString());
        }

        [Command("testJail")]
        public async Task TestJail(CommandContext ctx)
        {
            if (ctx.Member == null || ctx.User.IsBot)
            {
                return;
            }
            OrpheusDatabaseHandler handler = new OrpheusDatabaseHandler();
            ulong channelid = await handler.GetJailIDInfo(ctx.Guild.Id, "jailid");
            if (channelid == 0)
            {
                await ctx.Channel.SendMessageAsync("Failed; JailChannel has not been registered");
                return;
            }
            await ctx.Guild.GetChannel(channelid).SendMessageAsync("Jail Test");
        }

        [Command("testSend")]
        public async Task testSend(CommandContext ctx, DiscordMember user)
        {
            if (ctx.Member == null || ctx.User.IsBot)
            {
                return;
            }
            OrpheusDatabaseHandler handler = new OrpheusDatabaseHandler();
            ulong channelid = await handler.GetJailIDInfo(ctx.Guild.Id, "jailroleid");
            if (channelid == 0)
            {
                await ctx.Channel.SendMessageAsync("Send Failed; JailRole has not been registered");
                return;
            }
            DiscordRole jailrole = ctx.Guild.GetRole(channelid);
            await user.GrantRoleAsync(jailrole);
        }

        [Command("storeUser")]
        public async Task StoreUser(CommandContext ctx)
        {
            if (ctx.Member == null || ctx.User.IsBot)
            {
                return;
            }
            OrpheusDatabaseHandler handler = new OrpheusDatabaseHandler();
            DUser dUser = new DUser() { userId = ctx.User.Id, username = ctx.User.Username, };

            bool isStored = await handler.StoreUserAsync(dUser);
            if (isStored)
            {
                await ctx.Channel.SendMessageAsync("Succesfully stored in Database");
            }
            else
            {
                await ctx.Channel.SendMessageAsync("Failed to store in Database");
            }
        }
    }
}
