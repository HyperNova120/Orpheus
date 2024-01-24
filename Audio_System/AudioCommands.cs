using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using DSharpPlus;
using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;
using DSharpPlus.Entities;
using DSharpPlus.EventArgs;
using DSharpPlus.Lavalink;
using DSharpPlus.Lavalink.Entities;
using DSharpPlus.Net;
using DSharpPlus.VoiceNext;
using NpgsqlTypes;
using Orpheus.Database;
using Orpheus.JailHandling;
using Orpheus.registerCommands;

namespace Orpheus.Audio_System
{
    public static class AudioCommands
    {
        public static async Task JoinVoiceChannel(CommandContext ctx)
        {
            DiscordChannel ChannelToJoin = getChannelToEnterAsync(ctx);
            if (ChannelToJoin == null)
            {
                await ctx.Channel.SendMessageAsync("Channel To Join Is Invalid");
                return;
            }
            LavalinkExtension lava = ctx.Client.GetLavalink();
            if (!lava.ConnectedNodes.Any())
            {
                await ctx.Channel.SendMessageAsync("The Lavalink connection is not established");
                return;
            }
            LavalinkNodeConnection node = lava.GetIdealNodeConnection();

            LavalinkGuildConnection conn = node.GetGuildConnection(ctx.Guild);
            if (conn != null)
            {
                if (conn.Channel == ChannelToJoin)
                {
                    Console.WriteLine("ALREADY CONNECTED TO CHANNEL");
                    return;
                }
                Console.WriteLine("DISCONNECT TO CONNECT TO CHANNEL:" + ctx.Channel.Name);
                await conn.DisconnectAsync();
            }

            await node.ConnectAsync(ChannelToJoin);
            await ctx.Channel.SendMessageAsync($"Joined {ChannelToJoin.Name}!");

            Console.WriteLine("CONNECT TO CHANNEL:" + ctx.Channel.Name);
            await ctx.Message.DeleteAsync();
        }

        private static DiscordChannel getChannelToEnterAsync(CommandContext ctx)
        {
            if (ctx.Member.VoiceState != null && ctx.Member.VoiceState.Channel != null)
            {
                return ctx.Member.VoiceState.Channel;
            }
            else if (ctx.Channel.Type == DSharpPlus.ChannelType.Voice)
            {
                return ctx.Channel;
            }
            else
            {
                return null;
            }
        }

        public static async Task LeaveVoiceChannel(CommandContext ctx)
        {
            LavalinkExtension lava = ctx.Client.GetLavalink();
            if (!lava.ConnectedNodes.Any())
            {
                await ctx.Channel.SendMessageAsync("The Lavalink connection is not established");
                return;
            }

            LavalinkNodeConnection node = lava.GetIdealNodeConnection();

            LavalinkGuildConnection conn = node.GetGuildConnection(ctx.Guild);

            if (conn == null)
            {
                await ctx.Channel.SendMessageAsync("Lavalink is not connected.");
                return;
            }
            string tempName = conn.Channel.Name;
            await conn.DisconnectAsync();
            await ctx.Channel.SendMessageAsync($"Left {tempName}!");
            Console.WriteLine("LEAVE CHANNEL:" + tempName);
            await ctx.Message.DeleteAsync();
        }

        public static async Task PlayMusic(CommandContext ctx, string seachString)
        {
            if (seachString.Length == 0)
            {
                return;
            }
            //join if needed
            await JoinVoiceChannel(ctx);

            LavalinkExtension lava = ctx.Client.GetLavalink();
            LavalinkNodeConnection node = lava.ConnectedNodes.Values.First();
            LavalinkGuildConnection conn = node.GetGuildConnection(ctx.Member.VoiceState.Guild);

            if (conn == null)
            {
                await ctx.Channel.SendMessageAsync("Lavalink is not connected.");
                return;
            }
            LavalinkLoadResult result = await node.Rest.GetTracksAsync(
                seachString,
                LavalinkSearchType.Youtube
            );
            //If something went wrong on Lavalink's end
            if (
                result.LoadResultType == LavalinkLoadResultType.LoadFailed
                || result.LoadResultType == LavalinkLoadResultType.NoMatches
            )
            {
                await ctx.RespondAsync($"Track search failed for {seachString}.");
                return;
            }
            LavalinkTrack track = result.Tracks.First();
            await conn.PlayAsync(track);
            await ctx.Channel.SendMessageAsync($"Playing Track {track.Title}");
        }

        public static async Task PlayMusic(CommandContext ctx, Uri url)
        {
            //join if needed
            await JoinVoiceChannel(ctx);

            LavalinkExtension lava = ctx.Client.GetLavalink();
            LavalinkNodeConnection node = lava.ConnectedNodes.Values.First();
            LavalinkGuildConnection conn = node.GetGuildConnection(ctx.Member.VoiceState.Guild);

            if (conn == null)
            {
                await ctx.Channel.SendMessageAsync("Lavalink is not connected.");
                return;
            }
            LavalinkLoadResult result = await node.Rest.GetTracksAsync(url);
            //If something went wrong on Lavalink's end
            if (
                result.LoadResultType == LavalinkLoadResultType.LoadFailed
                || result.LoadResultType == LavalinkLoadResultType.NoMatches
            )
            {
                await ctx.RespondAsync($"Track search failed for {url}.");
                return;
            }
            LavalinkTrack track = result.Tracks.First();
            await conn.PlayAsync(track);
            await ctx.Channel.SendMessageAsync($"Playing Track {track.Title}");
        }

        public static async Task ResumeMusic(CommandContext ctx)
        {

            LavalinkExtension lava = ctx.Client.GetLavalink();
            LavalinkNodeConnection node = lava.ConnectedNodes.Values.First();
            LavalinkGuildConnection conn = node.GetGuildConnection(ctx.Member.VoiceState.Guild);

            if (conn == null)
            {
                await ctx.Channel.SendMessageAsync("Lavalink is not connected.");
                return;
            }
            await conn.ResumeAsync();
            await ctx.Channel.SendMessageAsync($"Resuming Track");
        }
        public static async Task StopMusic(CommandContext ctx)
        {

            LavalinkExtension lava = ctx.Client.GetLavalink();
            LavalinkNodeConnection node = lava.ConnectedNodes.Values.First();
            LavalinkGuildConnection conn = node.GetGuildConnection(ctx.Member.VoiceState.Guild);

            if (conn == null)
            {
                await ctx.Channel.SendMessageAsync("Lavalink is not connected.");
                return;
            }
            await conn.StopAsync();
            await ctx.Channel.SendMessageAsync($"Stopped Track");
        }

        public static async Task pauseMusic(CommandContext ctx)
        {
            var lava = ctx.Client.GetLavalink();
            var node = lava.ConnectedNodes.Values.First();
            var conn = node.GetGuildConnection(ctx.Member.VoiceState.Guild);

            if (conn == null)
            {
                await ctx.RespondAsync("Lavalink is not connected.");
                return;
            }
            if (conn.CurrentState.CurrentTrack == null)
            {
                await ctx.RespondAsync("There are no tracks loaded.");
                return;
            }
            await conn.PauseAsync();
        }
    }
}
