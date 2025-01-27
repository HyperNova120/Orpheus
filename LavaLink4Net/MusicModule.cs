using System;
using System.Threading.Tasks;
using DSharpPlus.Entities;
using Lavalink4NET.DSharpPlus;
using Microsoft.Extensions.Options;
using System.ComponentModel;
using DSharpPlus.CommandsNext.Attributes;
using Lavalink4NET.Players.Queued;
using Lavalink4NET.Players;
using DSharpPlus.CommandsNext;
using System.Drawing;
using Lavalink4NET;

public static class MusicModule
{
    private static IAudioService _audioService = null;

    /* public MusicModule(IAudioService audioService)
    {
        SetUp(audioService);
    } */

    public static async Task SetUp(IAudioService audioService)
    {
        ArgumentNullException.ThrowIfNull(audioService);
        _audioService = audioService;
        await _audioService.StartAsync();
        Console.WriteLine($"SUCCESSFULLY REGISTERED AUDIO SERVICE!!!");
    }

    public static async Task JoinChannel(CommandContext ctx)
    {
        var playerOptions = new QueuedLavalinkPlayerOptions { HistoryCapacity = 10000 };
        try
        {
            await _audioService.Players.JoinAsync(ctx.Guild.Id, ctx.Member.VoiceState.Channel.Id, playerFactory: PlayerFactory.Queued, Options.Create(playerOptions));
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
        }
    }

    private static async ValueTask<QueuedLavalinkPlayer> GetPlayerAsync(CommandContext context, bool connectToVoiceChannel = true)
    {
        PlayerChannelBehavior channelBehavior = connectToVoiceChannel ? PlayerChannelBehavior.Join : PlayerChannelBehavior.None;
        Console.WriteLine("GetPlayerAsync 1");
        PlayerRetrieveOptions playerRetrieveOptions = new PlayerRetrieveOptions(ChannelBehavior: channelBehavior);
        Console.WriteLine("GetPlayerAsync 2");
        var playerOptions = new QueuedLavalinkPlayerOptions { HistoryCapacity = 10000 };
        Console.WriteLine("GetPlayerAsync 3");
        if (context.Guild == null)
        {
            Console.WriteLine("Not Guild Message");
        }
        else if (context.Member.VoiceState.Channel == null)
        {
            Console.WriteLine("not in voice channel");
        }
        try
        {
            var result = await _audioService.Players.RetrieveAsync(context.Guild!.Id, context.Member?.VoiceState?.Channel?.Id ?? null, playerFactory: PlayerFactory.Queued, Options.Create(playerOptions), playerRetrieveOptions).ConfigureAwait(false);



            Console.WriteLine("GetPlayerAsync 4");
            if (!result.IsSuccess)
            {
                var errorMessage = result.Status switch
                {
                    PlayerRetrieveStatus.UserNotInVoiceChannel => "You are not connected to a voice channel.",
                    PlayerRetrieveStatus.BotNotConnected => "The bot is currently not connected.",
                    _ => "Unknown Error.",
                };

                var errorResponse = new DiscordFollowupMessageBuilder().WithContent(errorMessage).AsEphemeral();
                await context.Message.RespondAsync(errorResponse.Content).ConfigureAwait(false);
                return null;
            }
            Console.WriteLine("GetPlayerAsync successfully retrieved player");
            return result.Player;
        }
        catch (Exception e)
        {
            Console.WriteLine("MUSIC PLAYER ERROR");
            Console.WriteLine(e.ToString());
            return null;
        }
    }

    private static async Task<Lavalink4NET.Tracks.LavalinkTrack?> GetTrackAsync(string query, QueuedLavalinkPlayer queuedLavalinkPlayer)
    {
        var track = await _audioService.Tracks.LoadTrackAsync(query, searchMode: Lavalink4NET.Rest.Entities.Tracks.TrackSearchMode.YouTube);
        if (track == null)
        {
            return null;
        }
        return track;
    }

    public static async Task PlayTrack(CommandContext ctx, string query)
    {
        Console.WriteLine("PlayTrack start");
        QueuedLavalinkPlayer queuedLavalinkPlayer = await GetPlayerAsync(ctx);
        if (queuedLavalinkPlayer == null)
        {
            Console.WriteLine("GetPlayerAsync FAILED");
            return;
        }
        else
        {
            Console.WriteLine("GetPlayerAsync SUCCEED");
        }
        Lavalink4NET.Tracks.LavalinkTrack? track = await GetTrackAsync(query, queuedLavalinkPlayer);
        if (track == null)
        {
            await ctx.Message.RespondAsync(new DiscordMessageBuilder().WithContent($"No Results"));
            return;
        }
        Console.WriteLine($"Start Playing: {track.Uri}");
        int position = await queuedLavalinkPlayer.PlayAsync(track);
        Console.WriteLine($"Started Playing: {track.Uri}");
        if (position == 0)
        {
            await ctx.Message.RespondAsync(new DiscordMessageBuilder().WithContent($"Playing: {track.Uri}"));
        }
        else
        {
            await ctx.Message.RespondAsync(new DiscordMessageBuilder().WithContent($"Added To Queue: {track.Uri}"));
        }
    }
}