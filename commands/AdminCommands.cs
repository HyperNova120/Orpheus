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
            await Task.Delay(250);
            await ctx.Channel.SendMessageAsync(message);

            DiscordEmbedBuilder message2 = new DiscordEmbedBuilder
            {
                Title = "Clean Embed",
                Description = $"Excecuted by {ctx.User.Username}",
                Color = DiscordColor.Blue
            };
            await ctx.Channel.SendMessageAsync(embed: message2);
            await Task.Delay(250);
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
            await ctx.Channel.SendMessageAsync(
                $"Registered {jailChannel.Name} ID:{jailChannel.Id} as server jail"
            );
            await Task.Delay(250);
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
            await ctx.Channel.SendMessageAsync(
                $"Registered {jailCourtChannel.Name} ID:{jailCourtChannel.Id} as server jail court"
            );
            await Task.Delay(250);
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
                Console.WriteLine($"JAILROLE FAIL isNotValidCommand:{isNotValidCommand(ctx)} doesUserHavePerms:{Convert.ToBoolean(await doesUserHavePerms(ctx))} jailRole NULL:{jailRole == null}");
                return;
            }

            await OrpheusDatabaseHandler.UpdateServerJailRoleID(ctx.Guild.Id, jailRole.Id);
            Console.WriteLine($"Registered {jailRole.Name} ID:{jailRole.Id} as server jail role");
            await ctx.Channel.SendMessageAsync(
                $"Registered {jailRole.Name} ID:{jailRole.Id} as server jail role"
            );
            await Task.Delay(250);
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
            await RegisterServer(dServer);
            await Task.Delay(250);
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
            await Task.Delay(250);
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
            await OrpheusDatabaseHandler.RemoveAdminAsync(dAdmin);
            await Task.Delay(250);
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
            try
            {
                await user.GrantRoleAsync(jailrole);
                await ctx.Channel.SendMessageAsync($"{user.Username} has been sent to jail!");
            }
            catch (Exception e)
            {
                Console.WriteLine(e.ToString());
            }
            await Task.Delay(250);
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
            await Task.Delay(250);
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
            await Task.Delay(250);
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
            await Task.Delay(250);
            await ctx.Message.DeleteAsync();
        }

        [Command("join")]
        public async Task JoinVoiceChannel(CommandContext ctx)
        {
            if (isNotValidCommand(ctx) || !Convert.ToBoolean(await doesUserHavePerms(ctx)))
            {
                return;
            }
            //connect to channel user is in if applicable, else connect to channel msg was sent in if applicable
            if (ctx.Member.VoiceState != null && ctx.Member.VoiceState.Channel != null)
            {
                await ctx.Member.VoiceState.Channel.ConnectAsync();
            }
            else if (ctx.Channel.Type == DSharpPlus.ChannelType.Voice)
            {
                await ctx.Channel.ConnectAsync();
            }
            Console.WriteLine("CONNECT TO CHANNEL:" + ctx.Channel.Name);
            await Task.Delay(250);
            await ctx.Message.DeleteAsync();
        }

        [Command("leave")]
        public async Task LeaveVoiceChannel(CommandContext ctx)
        {
            if (isNotValidCommand(ctx) || !Convert.ToBoolean(await doesUserHavePerms(ctx)))
            {
                return;
            }
            Program.GetVoiceNextExtension().GetConnection(ctx.Guild).Disconnect();
            Console.WriteLine("LEAVE CHANNEL:" + ctx.Channel.Name);
            await Task.Delay(250);
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
                Console.WriteLine("VALID COMMAND, OWNER:");
                return true;
            }
            return await DBEngine.DoesEntryExist(
                "orpheusdata.admininfo",
                "serverid",
                "userid",
                ctx.Member.Guild.Id.ToString(),
                ctx.Member.Id.ToString()
            );
        }
    }
}
