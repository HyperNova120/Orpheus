

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
                await MusicModule.PauseTrack(args.Guild.Id);
                await args.Interaction.CreateResponseAsync(DiscordInteractionResponseType.UpdateMessage);
                break;
            case "Resume":
                await MusicModule.ResumeTrack(args.Guild.Id);
                await args.Interaction.CreateResponseAsync(DiscordInteractionResponseType.UpdateMessage);
                break;
            case "Next-Track":
                await MusicModule.NextTrack(args.Guild.Id);
                await args.Interaction.CreateResponseAsync(DiscordInteractionResponseType.UpdateMessage);
                break;
                case "ToggleAutoPlay":
                await MusicModule.ToggleAutoplay(args.Guild.Id);
                await args.Interaction.CreateResponseAsync(DiscordInteractionResponseType.UpdateMessage);
                break;
            default:
                Console.WriteLine($"UNKNOWN INTERACTION: {args.Id}");
                break;
        }
    }

    #endregion Music-Commands
}