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
using Lavalink4NET.Rest.Entities.Tracks;
using Lavalink4NET.Tracks;

public static class MusicModule
{
    private static bool stop = false;

    private static IAudioService _audioService = null;
    private static Dictionary<ulong, DiscordMessage> PlayerControllerByServer = new Dictionary<ulong, DiscordMessage>();
    private static Dictionary<ulong, CommandContext> PlayerControllerContextByServer = new Dictionary<ulong, CommandContext>();

    public static async Task SetUp(IAudioService audioService)
    {
        ArgumentNullException.ThrowIfNull(audioService);
        _audioService = audioService;
        await _audioService.StartAsync();
        for (int i = 0; i < 5; i++)
        {
            Utils.PrintToConsoleWithColor($"Audio Start Complete In {5 - i}", ConsoleColor.Yellow);
            await Task.Delay(1000);
        }
        Console.WriteLine($"SUCCESSFULLY REGISTERED AUDIO SERVICE!!!");
        _ = updatePlayers();
    }

    public static async Task Stop()
    {
        stop = true;
        foreach (ulong key in PlayerControllerByServer.Keys)
        {
            try
            {
                await StopSpecificPlayer(key);
            }
            catch (Exception e) { }
        }
        await _audioService.StopAsync();
    }

    private static async Task StopSpecificPlayer(ulong guildID)
    {
        DiscordMessage msg;
        CommandContext ctx;
        PlayerControllerByServer.TryGetValue(guildID, out msg);
        PlayerControllerContextByServer.TryGetValue(guildID, out ctx);
        QueuedLavalinkPlayer player = await GetPlayerAsync(ctx);
        if (player == null)
        {
            return;
        }
        await player.StopAsync();
        await player.DisconnectAsync();
        await msg.DeleteAsync();
        PlayerControllerByServer.Remove(ctx.Guild.Id);
        PlayerControllerContextByServer.Remove(ctx.Guild.Id);
    }

    private static async Task updatePlayers()
    {
        while (true)
        {
            if (stop)
            {
                return;
            }
            foreach (ulong key in PlayerControllerByServer.Keys)
            {
                if (stop)
                {
                    return;
                }
                try
                {
                    DiscordMessage msg;
                    CommandContext ctx;
                    PlayerControllerByServer.TryGetValue(key, out msg);
                    PlayerControllerContextByServer.TryGetValue(key, out ctx);
                    await msg.ModifyAsync(await createMusicPlayer(ctx));
                }
                catch (Exception e) { }
            }
            await Task.Delay(950);
        }
    }

    public static async Task CreatePlayerController(CommandContext ctx)
    {
        Console.WriteLine("CreatePlayerController 0");
        if (PlayerControllerByServer.ContainsKey(ctx.Guild.Id))
        {
            //audio player controller exists
            PlayerControllerByServer.TryGetValue(ctx.Guild.Id, out DiscordMessage? msg_out);
            await msg_out.DeleteAsync();

            PlayerControllerByServer.Remove(ctx.Guild.Id);
            PlayerControllerContextByServer.Remove(ctx.Guild.Id);
        }

        DiscordMessageBuilder discordMessageBuilder = await createMusicPlayer(ctx);
        if (discordMessageBuilder == null)
        {
            return;
        }

        DiscordMessage msg = await ctx.Channel.SendMessageAsync(discordMessageBuilder);
        PlayerControllerByServer.Add(ctx.Guild.Id, msg);
        PlayerControllerContextByServer.Add(ctx.Guild.Id, ctx);
    }

    private static async Task<DiscordMessageBuilder> createMusicPlayer(CommandContext ctx)
    {
        QueuedLavalinkPlayer player = await GetPlayerAsync(ctx, false);

        LavalinkTrack? currentTrack = null;
        TrackPosition? trackPosition = new TrackPosition();
        List<ITrackQueueItem> tracks = new List<ITrackQueueItem>();


        if (player != null)
        {
            currentTrack = player.CurrentTrack;
            trackPosition = player.Position;
            tracks = player.Queue.ToList();
        }
        string currentTrackTitle = (currentTrack != null) ? currentTrack.Title.ToString() : "N/A";
        string currentTrackPosition = (currentTrack != null) ? $"{new TimeSpan(0, 0, (int)trackPosition.Value.Position.TotalSeconds)}/{currentTrack.Duration}" : "N/A";
        string queuedTracks = (tracks.Count == 0) ? "N/A" : "";
        int numQueuedTracks = (tracks != null) ? tracks.Count : 0;
        foreach (ITrackQueueItem track in tracks)
        {
            queuedTracks += $"-{track.Track.Title} By: {track.Track.Author}\n";
        }
        try
        {

            DiscordEmbedBuilder embedBuilder1 = new DiscordEmbedBuilder();
            string autoplayIndicator = (player != null && player.AutoPlay) ? "On" : "Off";
            embedBuilder1.Title = "|\t\t\t\t\t\t\t\t\t\tAudio Player Controller\t\t\t\t\t\t\t\t\t\t|";
            embedBuilder1.AddField("Autoplay", $"{autoplayIndicator}", true);
            embedBuilder1.AddField("Position", $"{currentTrackPosition}", true);
            embedBuilder1.AddField("Current Track", $"{currentTrackTitle}", false);
            embedBuilder1.AddField("Queued Tracks", queuedTracks.Trim(), false);
            embedBuilder1.Color = DiscordColor.Gold;

            DiscordMessageBuilder discordMessageBuilder = new DiscordMessageBuilder();
            discordMessageBuilder.AddEmbed(embedBuilder1);

            DiscordButtonComponent Pause = new DiscordButtonComponent(DiscordButtonStyle.Primary, "TestPlayerEmbed_Pause", "Pause");
            DiscordButtonComponent Resume = new DiscordButtonComponent(DiscordButtonStyle.Primary, "TestPlayerEmbed_Resume", "Resume");
            DiscordButtonComponent ToggleAutoPlay = new DiscordButtonComponent(DiscordButtonStyle.Primary, "TestPlayerEmbed_ToggleAutoPlay", "Toggle Autoplay");
            DiscordButtonComponent NextTrack = new DiscordButtonComponent(DiscordButtonStyle.Primary, "TestPlayerEmbed_Next-Track", "Next Track");
            DiscordButtonComponent Leave = new DiscordButtonComponent(DiscordButtonStyle.Primary, "TestPlayerEmbed_Leave", "Leave");

            DiscordTextInputComponent TrackRequest = new DiscordTextInputComponent("Track Request", "TestPlayerEmbed_TrackRequest", "Requested Track URL", null, false, DiscordTextInputStyle.Short, max_length: 100);

            DiscordActionRowComponent discordActionRowComponent = new DiscordActionRowComponent([Pause, Resume, NextTrack, ToggleAutoPlay, Leave]);
            discordMessageBuilder.AddComponents(discordActionRowComponent.Components);
            return discordMessageBuilder;
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
            return null;
        }
    }





    private static async ValueTask<QueuedLavalinkPlayer> GetPlayerAsync(CommandContext context, bool connectToVoiceChannel = true, DiscordMember memberToJoinTo = null)
    {
        PlayerChannelBehavior channelBehavior = connectToVoiceChannel ? PlayerChannelBehavior.Join : PlayerChannelBehavior.None;
        PlayerRetrieveOptions playerRetrieveOptions = new PlayerRetrieveOptions(ChannelBehavior: channelBehavior);
        var playerOptions = new QueuedLavalinkPlayerOptions { HistoryCapacity = 10000 };
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
            ulong? channelToJoin = context.Member?.VoiceState?.Channel?.Id ?? null;
            channelToJoin = (memberToJoinTo != null) ? memberToJoinTo.VoiceState?.Channel?.Id ?? null : channelToJoin;
            var result = await _audioService.Players.RetrieveAsync(context.Guild!.Id, channelToJoin, playerFactory: PlayerFactory.Queued, Options.Create(playerOptions), playerRetrieveOptions).ConfigureAwait(false);

            if (!result.IsSuccess)
            {
                var errorMessage = result.Status switch
                {
                    PlayerRetrieveStatus.UserNotInVoiceChannel => "You are not connected to a voice channel.",
                    PlayerRetrieveStatus.BotNotConnected => "The bot is currently not connected.",
                    _ => "Unknown Error.",
                };
                await context.Message.RespondAsync(errorMessage).ConfigureAwait(false);
                return null;
            }

            return result.Player;
        }
        catch (Exception e)
        {
            Console.WriteLine(e.ToString());
            return null;
        }
    }

    public static async ValueTask<QueuedLavalinkPlayer> GetPlayerAsync(ulong guildId)
    {
        PlayerRetrieveOptions playerRetrieveOptions = new PlayerRetrieveOptions(ChannelBehavior: PlayerChannelBehavior.None);
        var playerOptions = new QueuedLavalinkPlayerOptions { HistoryCapacity = 10000 };
        var result = await _audioService.Players.RetrieveAsync(guildId, null, playerFactory: PlayerFactory.Queued, Options.Create(playerOptions), playerRetrieveOptions).ConfigureAwait(false);
        return result.Player;
    }

    private static async Task<Lavalink4NET.Tracks.LavalinkTrack?> GetTrackAsync(string query)
    {
        var track = await _audioService.Tracks.LoadTrackAsync(query, TrackSearchMode.YouTube);
        //var track2 = await _audioService.Tracks.LoadTrackAsync(query, TrackSearchMode.YouTubeMusic);
        if (track != null)
        {
            return track;
        }

        Console.WriteLine("Failed to find track");
        return null;
    }

    public static async Task PlayTrack(CommandContext ctx, string query, DiscordMember discordMemberToJoinTo = null)
    {
        Console.WriteLine("PlayTrack start");
        QueuedLavalinkPlayer queuedLavalinkPlayer = await GetPlayerAsync(ctx, memberToJoinTo: (discordMemberToJoinTo == null) ? null : discordMemberToJoinTo);
        if (queuedLavalinkPlayer == null)
        {
            Console.WriteLine("GetPlayerAsync FAILED");
            return;
        }
        else
        {
            Console.WriteLine("GetPlayerAsync SUCCEED");
        }
        Lavalink4NET.Tracks.LavalinkTrack? track = await GetTrackAsync(query);
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
            await ctx.Message.RespondAsync(new DiscordMessageBuilder().WithContent($"Playing: <{track.Uri}>"));
        }
        else
        {
            await ctx.Message.RespondAsync(new DiscordMessageBuilder().WithContent($"Added To Queue: <{track.Uri}>"));
        }
    }

    public static async Task StopTrack(CommandContext ctx)
    {
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
        await queuedLavalinkPlayer.StopAsync();
    }

    public static async Task PauseTrack(ulong guildId)
    {
        QueuedLavalinkPlayer queuedLavalinkPlayer = await GetPlayerAsync(guildId);
        if (queuedLavalinkPlayer == null)
        {
            return;
        }
        await queuedLavalinkPlayer.PauseAsync();
    }

    public static async Task ResumeTrack(ulong guildId)
    {
        QueuedLavalinkPlayer queuedLavalinkPlayer = await GetPlayerAsync(guildId);
        if (queuedLavalinkPlayer == null)// || queuedLavalinkPlayer.IsPaused)
        {
            string tmp = (queuedLavalinkPlayer == null) ? "NULL" : "NOT PAUSED";
            Console.WriteLine($"Resume Failed: {tmp}");
            return;
        }
        await queuedLavalinkPlayer.ResumeAsync();
    }

    public static async Task NextTrack(ulong guildId)
    {
        QueuedLavalinkPlayer queuedLavalinkPlayer = await GetPlayerAsync(guildId);
        if (queuedLavalinkPlayer == null)
        {
            return;
        }
        await queuedLavalinkPlayer.SkipAsync();
    }

    internal static async Task ToggleAutoplay(ulong guildId)
    {
        QueuedLavalinkPlayer queuedLavalinkPlayer = await GetPlayerAsync(guildId);
        if (queuedLavalinkPlayer == null)
        {
            return;
        }
        queuedLavalinkPlayer.AutoPlay = !queuedLavalinkPlayer.AutoPlay;
    }

    internal static async Task Leave(ulong guildId)
    {
        await StopSpecificPlayer(guildId);
    }
}