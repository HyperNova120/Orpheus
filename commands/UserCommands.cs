using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;
using DSharpPlus.Entities;

namespace Orpheus.commands
{
    public class UserCommands : BaseCommandModule
    {
        [Command("ping")]
        public async Task Ping(CommandContext ctx)
        {
            if (ctx.Member == null || ctx.User.IsBot)
            {
                return;
            }
            if (ctx.Member.IsOwner)
            {
                await ctx.Channel.SendMessageAsync($"Ping Owner:{ctx.User.Username}");
            }
            else
            {
                await ctx.Channel.SendMessageAsync($"Ping {ctx.User.Username}");
            }
        }

        [Command("ping")]
        public async Task Ping(CommandContext ctx, DiscordMember mentionedUser)
        {
            if (ctx.Member == null || ctx.User.IsBot || mentionedUser.IsBot)
            {
                return;
            }
            await ctx.Channel.SendMessageAsync(
                $"{ctx.User.Username} Pinged {mentionedUser.Username}"
            );
            await mentionedUser.SendMessageAsync($"Pinged by {ctx.User.Username}");
        }

        [Command("dm")]
        public async Task Dm(
            CommandContext ctx,
            DiscordMember mentionedUser,
            [RemainingText] string args
        )
        {
            if (ctx.Member == null || ctx.User.IsBot || mentionedUser.IsBot)
            {
                return;
            }
            await mentionedUser.SendMessageAsync(args.Trim());
            await ctx.Message.DeleteAsync();
        }

        [Command("say")]
        public async Task Say(CommandContext ctx, [RemainingText] string msg)
        {
            if (ctx.Member == null || ctx.User.IsBot)
            {
                return;
            }
            await ctx.Channel.SendMessageAsync(msg);
            await ctx.Message.DeleteAsync();
        }
    }
}
