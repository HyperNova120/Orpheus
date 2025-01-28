using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;
using DSharpPlus.Entities;

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
            await ctx.Message.DeleteAsync();
        }

        [Command("stop")]
        public async Task Stop(CommandContext ctx)
        {
            if (ctx.Member == null || ctx.User.IsBot)
            {
                return;
            }
            await MusicModule.StopTrack(ctx);
            await ctx.Message.DeleteAsync();
        }
        

        [Command("musicplayer")]
        public async Task MusicPlayer(CommandContext ctx)
        {
            await MusicModule.CreatePlayerController(ctx);
            await ctx.Message.DeleteAsync();
        }
    }
}