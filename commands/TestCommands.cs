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

        [Command("testhidden")]
        public async Task testHide(CommandContext ctx)
        {
            DiscordFollowupMessageBuilder builder = new DiscordFollowupMessageBuilder();
            builder.AsEphemeral(true);
            builder.WithContent("Hello Tester");
            DiscordMessageBuilder mb = new DiscordMessageBuilder(builder);
            await ctx.Channel.SendMessageAsync(mb);
        }
    }
}
