using DSharpPlus.CommandsNext;
using DSharpPlus.Entities;
using DSharpPlus.EventArgs;
using Orpheus.Database;

namespace Orpheus.registerCommands
{
    public static class Registration
    {
        public static async Task registerJail(CommandContext ctx, DiscordChannel jailChannel)
        {
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

        public static async Task registerJailCourt(CommandContext ctx, DiscordChannel jailCourtChannel)
        {
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

        public static async Task registerJailRole(CommandContext ctx, DiscordRole jailRole)
        {

            await OrpheusDatabaseHandler.UpdateServerJailRoleID(ctx.Guild.Id, jailRole.Id);
            Console.WriteLine($"Registered {jailRole.Name} ID:{jailRole.Id} as server jail role");
            await ctx.Channel.SendMessageAsync(
                $"Registered {jailRole.Name} ID:{jailRole.Id} as server jail role"
            );
            await Task.Delay(250);
            await ctx.Message.DeleteAsync();
        }

        public static async Task registerJailCourtRole(CommandContext ctx, DiscordRole jailCourtRole)
        {

            await OrpheusDatabaseHandler.UpdateServerJailCourtRoleID(
                ctx.Guild.Id,
                jailCourtRole.Id
            );
            Console.WriteLine(
                $"Registered {jailCourtRole.Name} ID:{jailCourtRole.Id} as server jail role"
            );
            await ctx.Channel.SendMessageAsync(
                $"Registered {jailCourtRole.Name} ID:{jailCourtRole.Id} as server jail role"
            );
            await Task.Delay(250);
            await ctx.Message.DeleteAsync();
        }

        public static async Task RegisterServer(CommandContext ctx)
        {
            DServer dServer = new DServer()
            {
                serverID = ctx.Guild.Id,
                serverName = ctx.Guild.Name,
                jailChannelID = 0,
                JailCourtID = 0,
                JailRoleID = 0,
                JailCourtRoleID = 0
            };
            await RegisterServer(dServer);
            await Task.Delay(250);
            await ctx.Message.DeleteAsync();
        }

        public static async Task RegisterServer(GuildCreateEventArgs args)
        {
            DServer dServer = new DServer()
            {
                serverID = args.Guild.Id,
                serverName = args.Guild.Name,
                jailChannelID = 0,
                JailCourtID = 0,
                JailRoleID = 0,
                JailCourtRoleID = 0
            };
            await RegisterServer(dServer);
        }

        public static async Task RegisterServer(DServer dServer)
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

        public static async Task RegisterAdmin(CommandContext ctx, DiscordMember memberToAdmin)
        {
            DAdmin dAdmin = new DAdmin() { userID = memberToAdmin.Id, serverID = ctx.Guild.Id };
            await Task.Delay(250);
            await ctx.Message.DeleteAsync();
            await OrpheusDatabaseHandler.StoreAdminAsync(dAdmin);
        }

        public static async Task RemoveAdmin(CommandContext ctx, DiscordMember memberToRemoveAdmin)
        {
            DAdmin dAdmin = new DAdmin()
            {
                userID = memberToRemoveAdmin.Id,
                serverID = ctx.Guild.Id
            };
            await OrpheusDatabaseHandler.RemoveAdminAsync(dAdmin);
            await Task.Delay(250);
            await ctx.Message.DeleteAsync();
        }
    }
}
