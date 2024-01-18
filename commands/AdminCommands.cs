using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Linq;
using System.Threading.Tasks;
using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;
using DSharpPlus.Entities;
using DSharpPlus.EventArgs;
using Orpheus.Database;

namespace Orpheus.commands
{
    public class AdminCommands : BaseCommandModule
    {
        public static DiscordChannel RegisteredJail = null;
        public static DiscordChannel RegisteredJailCourt = null;
        public static DiscordRole RegisteredJailRole = null;

        [Command("embed")]
        public async Task EmbedMessage(CommandContext ctx)
        {
            if (ctx.Member == null || ctx.User.IsBot)
            {
                return;
            }
            DiscordMessageBuilder message = new DiscordMessageBuilder().AddEmbed(
                new DiscordEmbedBuilder()
                    .WithTitle("Test Embed")
                    .WithDescription($"Excecuted by {ctx.User.Username}")
            );
            await ctx.Channel.SendMessageAsync(message);

            DiscordEmbedBuilder message2 = new DiscordEmbedBuilder
            {
                Title = "Clean Embed",
                Description = $"Excecuted by {ctx.User.Username}",
                Color = DiscordColor.Blue
            };
            await ctx.Channel.SendMessageAsync(embed: message2);
        }

        [Command("registerJail")]
        public async Task registerJail(CommandContext ctx, DiscordChannel jailChannel)
        {
            if (ctx.Member == null || ctx.Member.IsBot || jailChannel == null)
            {
                return;
            }
            RegisteredJail = jailChannel;
            await ctx.RespondAsync(
                $"Registered {jailChannel.Name} ID:{jailChannel.Id} as server jail"
            );
        }

        [Command("registerJailCourt")]
        public async Task registerJailCourt(CommandContext ctx, DiscordChannel jailCourtChannel)
        {
            if (ctx.Member == null || ctx.Member.IsBot || jailCourtChannel == null)
            {
                return;
            }
            RegisteredJailCourt = jailCourtChannel;
            await ctx.RespondAsync(
                $"Registered {jailCourtChannel.Name} ID:{jailCourtChannel.Id} as server jail"
            );
        }

        [Command("registerJailRole")]
        public async Task registerJailCourt(CommandContext ctx, DiscordRole jailRole)
        {
            if (ctx.Member == null || ctx.Member.IsBot || jailRole == null)
            {
                return;
            }
            RegisteredJailRole = jailRole;
            await ctx.RespondAsync($"Registered {jailRole.Name} ID:{jailRole.Id} as server jail");
        }

        [Command("registerServer")]
        public async Task RegisterServer(CommandContext ctx)
        {
            if (ctx.Member == null || ctx.User.IsBot)
            {
                return;
            }
            OrpheusDatabaseHandler handler = new OrpheusDatabaseHandler();
            DServer dServer = new DServer()
            {
                serverID = ctx.Guild.Id,
                serverName = ctx.Guild.Name,
                jailChannelID = (RegisteredJail == null) ? 0 : RegisteredJail.Id,
                JailCourtID = (RegisteredJailCourt == null) ? 0 : RegisteredJailCourt.Id,
                JailRoleID = (RegisteredJailRole == null) ? 0 : RegisteredJailRole.Id,
            };

            bool isStored = await handler.StoreServerAsync(dServer);
            if (isStored)
            {
                await ctx.Channel.SendMessageAsync("Succesfully stored in Database");
            }
            else
            {
                await ctx.Channel.SendMessageAsync("Failed to store in Database");
            }
        }

        public async Task RegisterServer(GuildCreateEventArgs args)
        {
            OrpheusDatabaseHandler handler = new OrpheusDatabaseHandler();
            DServer dServer = new DServer()
            {
                serverID = args.Guild.Id,
                serverName = args.Guild.Name,
                jailChannelID = (RegisteredJail == null) ? 0 : RegisteredJail.Id,
                JailCourtID = (RegisteredJailCourt == null) ? 0 : RegisteredJailCourt.Id,
                JailRoleID = (RegisteredJailRole == null) ? 0 : RegisteredJailRole.Id,
            };

            if (
                Convert.ToBoolean(
                    await DBEngine.DoesEntryExist(
                        "orpheusdata.serverinfo",
                        "serverid",
                        args.Guild.Id.ToString()
                    )
                )
            )
            {
                //if server already exists
                await handler.UpdateServerAsync(dServer);
                Console.WriteLine("UPDATED SERVER");
                return;
            }

            bool isStored = await handler.StoreServerAsync(dServer);
            if (isStored)
            {
                Console.WriteLine("Succesfully stored in Database");
            }
            else
            {
                Console.WriteLine("Failed to store in Database");
            }
        }
    }
}
