using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;
using DSharpPlus.Entities;
using Orpheus.Database;

namespace Orpheus.commands
{
    public class MusicCommands : BaseCommandModule
    {
        [Command("play")]
        public async Task Play(CommandContext ctx, [RemainingText] string query)
        {
            if (ctx.Member == null || ctx.User.IsBot)
            {
                return;
            }
            Console.WriteLine($"Attempting to play audio:{query}");
            await MusicModule.PlayTrack(ctx, query);
        }
        [Command("join")]
        public async Task Join(CommandContext ctx)
        {
            if (ctx.Member == null || ctx.User.IsBot)
            {
                return;
            }
            Console.WriteLine($"Attempting to Join Channel");
            await MusicModule.JoinChannel(ctx);
        }
    }
}