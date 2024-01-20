using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;
using DSharpPlus.Entities;
using DSharpPlus.EventArgs;
using DSharpPlus.VoiceNext;
using NpgsqlTypes;
using Orpheus.Database;

namespace Orpheus.commands
{
    public class AdminCommands : BaseCommandModule
    {
        [Command("embed")]
        public async Task EmbedMessage(CommandContext ctx)
        {
            if (isNotValidCommand(ctx) || !Convert.ToBoolean(await doesUserHavePerms(ctx)))
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
            ctx.Channel.SendMessageAsync(embed: message2);
            await ctx.Message.DeleteAsync();
        }

        [Command("registerJail")]
        public async Task registerJail(CommandContext ctx, DiscordChannel jailChannel)
        {
            if (
                isNotValidCommand(ctx)
                || !Convert.ToBoolean(await doesUserHavePerms(ctx))
                || jailChannel == null
            )
            {
                return;
            }
            await OrpheusDatabaseHandler.UpdateServerJailChannelID(
                jailChannel.Guild.Id,
                jailChannel.Id
            );
            ctx.Channel.SendMessageAsync(
                $"Registered {jailChannel.Name} ID:{jailChannel.Id} as server jail"
            );
            await ctx.Message.DeleteAsync();
        }

        [Command("registerJailCourt")]
        public async Task registerJailCourt(CommandContext ctx, DiscordChannel jailCourtChannel)
        {
            if (
                isNotValidCommand(ctx)
                || !Convert.ToBoolean(await doesUserHavePerms(ctx))
                || jailCourtChannel == null
            )
            {
                return;
            }
            await OrpheusDatabaseHandler.UpdateServerJailCourtID(
                jailCourtChannel.Guild.Id,
                jailCourtChannel.Id
            );
            ctx.Channel.SendMessageAsync(
                $"Registered {jailCourtChannel.Name} ID:{jailCourtChannel.Id} as server jail court"
            );
            await ctx.Message.DeleteAsync();
        }

        [Command("registerJailRole")]
        public async Task registerJailCourt(CommandContext ctx, DiscordRole jailRole)
        {
            if (
                isNotValidCommand(ctx)
                || !Convert.ToBoolean(await doesUserHavePerms(ctx))
                || jailRole == null
            )
            {
                return;
            }

            await OrpheusDatabaseHandler.UpdateServerJailRoleID(ctx.Guild.Id, jailRole.Id);
            ctx.Channel.SendMessageAsync(
                $"Registered {jailRole.Name} ID:{jailRole.Id} as server jail role"
            );
            await ctx.Message.DeleteAsync();
        }

        [Command("registerServer")]
        public async Task RegisterServer(CommandContext ctx)
        {
            if (isNotValidCommand(ctx) || !Convert.ToBoolean(await doesUserHavePerms(ctx)))
            {
                return;
            }
            DServer dServer = new DServer()
            {
                serverID = ctx.Guild.Id,
                serverName = ctx.Guild.Name,
                jailChannelID = 0,
                JailCourtID = 0,
                JailRoleID = 0,
            };
            RegisterServer(dServer);
            await ctx.Message.DeleteAsync();
        }

        public async Task RegisterServer(GuildCreateEventArgs args)
        {
            DServer dServer = new DServer()
            {
                serverID = args.Guild.Id,
                serverName = args.Guild.Name,
                jailChannelID = 0,
                JailCourtID = 0,
                JailRoleID = 0,
            };
            await RegisterServer(dServer);
        }

        public async Task RegisterServer(DServer dServer)
        {
            if (
                Convert.ToBoolean(
                    await DBEngine.DoesEntryExist(
                        "orpheusdata.serverinfo",
                        "serverid",
                        dServer.serverID.ToString()
                    )
                )
            )
            {
                //if server already exists
                await OrpheusDatabaseHandler.UpdateServerAsync(dServer);
                Console.WriteLine($"UPDATED SERVER:{dServer.serverName}");
                return;
            }

            bool isStored = await OrpheusDatabaseHandler.StoreServerAsync(dServer);
            if (isStored)
            {
                Console.WriteLine("Succesfully stored in Database");
            }
            else
            {
                Console.WriteLine("Failed to store in Database");
            }
        }

        [Command("registerAdmin")]
        public async Task RegisterAdmin(CommandContext ctx, DiscordMember memberToAdmin)
        {
            if (
                isNotValidCommand(ctx)
                || !Convert.ToBoolean(await doesUserHavePerms(ctx))
                || Convert.ToBoolean(
                    await DBEngine.DoesEntryExist(
                        "orpheusdata.admininfo",
                        "userid",
                        "serverid",
                        memberToAdmin.Id.ToString(),
                        ctx.Guild.Id.ToString()
                    )
                )
            )
            {
                return;
            }
            DAdmin dAdmin = new DAdmin() { userID = memberToAdmin.Id, serverID = ctx.Guild.Id };
            await ctx.Message.DeleteAsync();
            await OrpheusDatabaseHandler.StoreAdminAsync(dAdmin);
        }

        [Command("removeAdmin")]
        public async Task RemoveAdmin(CommandContext ctx, DiscordMember memberToRemoveAdmin)
        {
            if (
                isNotValidCommand(ctx)
                || !Convert.ToBoolean(await doesUserHavePerms(ctx))
                || !Convert.ToBoolean(
                    await DBEngine.DoesEntryExist(
                        "orpheusdata.admininfo",
                        "userid",
                        "serverid",
                        memberToRemoveAdmin.Id.ToString(),
                        ctx.Guild.Id.ToString()
                    )
                )
            )
            {
                return;
            }
            DAdmin dAdmin = new DAdmin()
            {
                userID = memberToRemoveAdmin.Id,
                serverID = ctx.Guild.Id
            };
            OrpheusDatabaseHandler.RemoveAdminAsync(dAdmin);
            await ctx.Message.DeleteAsync();
        }

        [Command("jail")]
        public async Task Jail(CommandContext ctx, DiscordMember user)
        {
            if (isNotValidCommand(ctx) || !Convert.ToBoolean(await doesUserHavePerms(ctx)))
            {
                return;
            }
            ulong channelid = await OrpheusDatabaseHandler.GetJailIDInfo(
                ctx.Guild.Id,
                "jailroleid"
            );
            if (channelid == 0)
            {
                await ctx.Channel.SendMessageAsync("Send Failed; JailRole has not been registered");
                return;
            }
            DiscordRole jailrole = ctx.Guild.GetRole(channelid);
            await user.GrantRoleAsync(jailrole);
            ctx.Channel.SendMessageAsync($"{user.Username} has been sent to jail!");
            await ctx.Message.DeleteAsync();
        }

        [Command("on")]
        public async Task StatusOnline(CommandContext ctx)
        {
            if (isNotValidCommand(ctx) || !Convert.ToBoolean(await doesUserHavePerms(ctx)))
            {
                return;
            }
            await Program.SetDiscordStatus(new DiscordActivity("In Testing on"), UserStatus.Online);
            await ctx.Message.DeleteAsync();
        }

        [Command("off")]
        public async Task StatusOffline(CommandContext ctx)
        {
            if (isNotValidCommand(ctx) || !Convert.ToBoolean(await doesUserHavePerms(ctx)))
            {
                return;
            }
            await Program.SetDiscordStatus(
                new DiscordActivity("In Testing off"),
                UserStatus.Offline
            );
            await ctx.Message.DeleteAsync();
        }

        [Command("dnd")]
        public async Task Statusdnd(CommandContext ctx)
        {
            if (isNotValidCommand(ctx) || !Convert.ToBoolean(await doesUserHavePerms(ctx)))
            {
                return;
            }
            await Program.SetDiscordStatus(
                new DiscordActivity("In Testing dnd"),
                UserStatus.DoNotDisturb
            );
            await ctx.Message.DeleteAsync();
        }

        [Command("join")]
        public async Task JoinVoiceChannel(CommandContext ctx)
        {
            if (
                isNotValidCommand(ctx)
                || !Convert.ToBoolean(await doesUserHavePerms(ctx))
                || ctx.Channel.Type != DSharpPlus.ChannelType.Voice
            )
            {
                return;
            }
            await ctx.Channel.ConnectAsync();
            Console.WriteLine("CONNECT TO CHANNEL:" + ctx.Channel.Name);
            await ctx.Message.DeleteAsync();
        }

        [Command("leave")]
        public async Task LeaveVoiceChannel(CommandContext ctx)
        {
            if (
                isNotValidCommand(ctx)
                || !Convert.ToBoolean(await doesUserHavePerms(ctx))
                || ctx.Channel.Type != DSharpPlus.ChannelType.Voice
            )
            {
                return;
            }
            Program.GetVoiceNextExtension().GetConnection(ctx.Guild).Disconnect();
            Console.WriteLine("LEAVE CHANNEL:" + ctx.Channel.Name);
            await ctx.Message.DeleteAsync();
        }

        public bool isNotValidCommand(CommandContext ctx)
        {
            return (ctx.Member == null || ctx.User.IsBot);
        }

        public async Task<bool> doesUserHavePerms(CommandContext ctx)
        {
            if (ctx.Member.IsOwner)
            {
                Console.Write("VALID COMMAND, OWNER:");
                return true;
            }
            return await DBEngine.DoesEntryExist(
                "orpheusdata.admininfo",
                "userid",
                ctx.Member.Id.ToString()
            );
        }
    }
}
