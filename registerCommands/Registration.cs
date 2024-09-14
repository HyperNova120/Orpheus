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
            DBEngine.Serverproperties sp = DBEngine.getServerProperties(ctx.Guild.Id);
            sp.JailCourtChannelID = jailChannel.Id;
            DBEngine.setServerProperties(ctx.Guild.Id, sp);
            await ctx.Channel.SendMessageAsync(
                $"Registered {jailChannel.Name} ID:{jailChannel.Id} as server jail"
            );
            await Task.Delay(250);
            await ctx.Message.DeleteAsync();
        }

        public static async Task registerJailCourt(CommandContext ctx, DiscordChannel jailCourtChannel)
        {
            DBEngine.Serverproperties sp = DBEngine.getServerProperties(ctx.Guild.Id);
            sp.JailCourtChannelID = jailCourtChannel.Id;

            DBEngine.setServerProperties(ctx.Guild.Id, sp);
            await ctx.Channel.SendMessageAsync(
                $"Registered {jailCourtChannel.Name} ID:{jailCourtChannel.Id} as server jail court"
            );
            await Task.Delay(250);
            await ctx.Message.DeleteAsync();
        }

        public static async Task registerJailRole(CommandContext ctx, DiscordRole jailRole)
        {
            Console.WriteLine("test1");
            DBEngine.Serverproperties sp = DBEngine.getServerProperties(ctx.Guild.Id);
            sp.JailRoleID = jailRole.Id;

            Console.WriteLine("test2");
            DBEngine.setServerProperties(ctx.Guild.Id, sp);

            Console.WriteLine($"Registered {jailRole.Name} ID:{jailRole.Id} as server jail role");
            await ctx.Channel.SendMessageAsync(
                $"Registered {jailRole.Name} ID:{jailRole.Id} as server jail role"
            );
            await ctx.Message.DeleteAsync();
        }

        public static async Task registerJailCourtRole(CommandContext ctx, DiscordRole jailCourtRole)
        {

            
            DBEngine.Serverproperties sp = DBEngine.getServerProperties(ctx.Guild.Id);
            sp.JailCourtRoleID = jailCourtRole.Id;
            DBEngine.setServerProperties(ctx.Guild.Id, sp);

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
            DBEngine.Serverproperties sp = DBEngine.getServerProperties(ctx.Guild.Id);
            sp.JailCourtChannelID = 0;
            sp.JailCourtRoleID = 0;
            sp.JailRoleID = 0;
            sp.ServerID = ctx.Guild.Id;
            DBEngine.setServerProperties(ctx.Guild.Id, sp);
            await ctx.Message.DeleteAsync();
        }


        public static async Task RegisterAdmin(CommandContext ctx, DiscordMember memberToAdmin)
        {
            DBEngine.SaveAdmin(ctx.Guild.Id, memberToAdmin.Id);
            await ctx.Message.DeleteAsync();
        }

        public static async Task RemoveAdmin(CommandContext ctx, DiscordMember memberToRemoveAdmin)
        {
            DBEngine.RemoveAdmin(ctx.Guild.Id, memberToRemoveAdmin.Id);
            await ctx.Message.DeleteAsync();
        }
    }
}
