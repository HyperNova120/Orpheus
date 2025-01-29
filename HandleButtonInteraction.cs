

using DSharpPlus.Entities;
using DSharpPlus.EventArgs;
using DSharpPlus.Interactivity.Extensions;
using Lavalink4NET.Players.Queued;


public static class HandleButtonInteractions
{
    public static async Task ButtonInteractionSwitch(ComponentInteractionCreatedEventArgs args)
    {
        //await args.Interaction.DeferAsync();
        Console.WriteLine(args.Id);
        //button id starts with module_id
        switch (args.Id.Split("_")[0])
        {
            case "TestPlayerEmbed":
                await TestPlayerEmbed(args);
                break;
            default:
                Console.WriteLine($"UNKNOWN INTERACTION: {args.Id}");
                break;
        }
    }

    #region Music-Commands

    private static async Task TestPlayerEmbed(ComponentInteractionCreatedEventArgs args)
    {
        QueuedLavalinkPlayer player = await MusicModule.GetPlayerAsync(args.Guild.Id);
        if (player == null)
        {
            Console.WriteLine("No Player: TestPlayerEmbed");
            return;
        }
        switch (args.Id.Split("_")[1])
        {
            case "Pause":
                await args.Interaction.CreateResponseAsync(DiscordInteractionResponseType.UpdateMessage);
                await MusicModule.PauseTrack(args.Guild.Id);
                break;
            case "Resume":
                await args.Interaction.CreateResponseAsync(DiscordInteractionResponseType.UpdateMessage);
                await MusicModule.ResumeTrack(args.Guild.Id);
                break;
            case "Next-Track":
                await args.Interaction.CreateResponseAsync(DiscordInteractionResponseType.UpdateMessage);
                await MusicModule.NextTrack(args.Guild.Id);
                break;
                case "ToggleAutoPlay":
                await args.Interaction.CreateResponseAsync(DiscordInteractionResponseType.UpdateMessage);
                await MusicModule.ToggleAutoplay(args.Guild.Id);
                break;
                case "Leave":
                await args.Interaction.CreateResponseAsync(DiscordInteractionResponseType.UpdateMessage);
                await MusicModule.Leave(args.Guild.Id);
                break;
            default:
                Console.WriteLine($"UNKNOWN INTERACTION: {args.Id}");
                break;
        }
    }

    #endregion Music-Commands
}