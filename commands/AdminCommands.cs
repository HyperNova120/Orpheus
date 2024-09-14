using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;
using DSharpPlus.Entities;
using DSharpPlus.EventArgs;
using Orpheus.Database;
using Orpheus.JailHandling;
using Orpheus.registerCommands;
using Orpheus.Voting;

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
            await Registration.registerJail(ctx, jailChannel);
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
            await Registration.registerJailCourt(ctx, jailCourtChannel);
        }

        [Command("registerJailRole")]
        public async Task registerJailRole(CommandContext ctx, DiscordRole jailRole)
        {
            if (
                isNotValidCommand(ctx)
                || !Convert.ToBoolean(await doesUserHavePerms(ctx))
                || jailRole == null
            )
            {
                Console.WriteLine(
                    $"JAILROLE FAIL isNotValidCommand:{isNotValidCommand(ctx)} doesUserHavePerms:{Convert.ToBoolean(await doesUserHavePerms(ctx))} jailRole NULL:{jailRole == null}"
                );
                return;
            }

            await Registration.registerJailRole(ctx, jailRole);
        }

        [Command("registerJailCourtRole")]
        public async Task registerJailCourtRole(CommandContext ctx, DiscordRole jailCourtRole)
        {
            if (
                isNotValidCommand(ctx)
                || !Convert.ToBoolean(await doesUserHavePerms(ctx))
                || jailCourtRole == null
            )
            {
                Console.WriteLine(
                    $"JAILROLE FAIL isNotValidCommand:{isNotValidCommand(ctx)} doesUserHavePerms:{Convert.ToBoolean(await doesUserHavePerms(ctx))} jailRole NULL:{jailCourtRole == null}"
                );
                return;
            }

            await Registration.registerJailCourtRole(ctx, jailCourtRole);
        }

        [Command("registerServer")]
        public async Task RegisterServer(CommandContext ctx)
        {
            if (isNotValidCommand(ctx) || !Convert.ToBoolean(await doesUserHavePerms(ctx)))
            {
                return;
            }
            await Registration.RegisterServer(ctx);
        }

        public void RegisterServer(GuildCreateEventArgs args)
        {
            DBEngine.Init(args.Guild.Id);
            if (DBEngine.doesServerPropertiesExist(args.Guild.Id))
            {
                return;
            }
            DBEngine.Serverproperties dServer = new DBEngine.Serverproperties()
            {
                ServerID = args.Guild.Id,
                JailCourtChannelID = 0,
                JailRoleID = 0,
                JailCourtRoleID = 0
            };
            DBEngine.setServerProperties(args.Guild.Id, dServer);
        }

        [Command("registerAdmin")]
        public async Task RegisterAdmin(CommandContext ctx, DiscordMember memberToAdmin)
        {
            if (
                isNotValidCommand(ctx)
                || !Convert.ToBoolean(await doesUserHavePerms(ctx))
                ||  DBEngine.DoesAdminExist(ctx.Guild.Id, memberToAdmin.Id)
            )
            {
                Console.WriteLine("Failed to add user as admin");
                return;
            }
            Console.WriteLine("adding user as admin");
            await Registration.RegisterAdmin(ctx, memberToAdmin);
        }

        [Command("removeAdmin")]
        public async Task RemoveAdmin(CommandContext ctx, DiscordMember memberToRemoveAdmin)
        {
            if (
                isNotValidCommand(ctx)
                || !Convert.ToBoolean(await doesUserHavePerms(ctx))
                || !DBEngine.DoesAdminExist(ctx.Guild.Id, memberToRemoveAdmin.Id)
                )
            {
                return;
            }
            await Registration.RemoveAdmin(ctx, memberToRemoveAdmin);
        }

        [Command("jail")]
        public async Task Jail(CommandContext ctx, DiscordMember user)
        {
            if (isNotValidCommand(ctx) || !Convert.ToBoolean(await doesUserHavePerms(ctx)))
            {
                return;
            }
            await JailCommands.Jail(ctx, user);
        }

        [Command("free")]
        public async Task JailFREE(CommandContext ctx, DiscordMember user)
        {
            if (isNotValidCommand(ctx) || !Convert.ToBoolean(await doesUserHavePerms(ctx)))
            {
                return;
            }
            await JailCommands.JailFREE(ctx, user);
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


        public static bool isNotValidCommand(CommandContext ctx)
        {
            return ctx.Member == null || ctx.User.IsBot;
        }

        public static async Task<bool> doesUserHavePerms(CommandContext ctx)
        {
            if (ctx.Member.IsOwner)
            {
                Console.WriteLine("VALID COMMAND, OWNER:");
                return true;
            }
            return DBEngine.DoesAdminExist(ctx.Member.Guild.Id, ctx.Member.Id);
        }
    }
}
