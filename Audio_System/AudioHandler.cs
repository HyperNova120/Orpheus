using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Threading.Tasks;
using DSharpPlus;
using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;
using DSharpPlus.Entities;
using DSharpPlus.EventArgs;
using DSharpPlus.Lavalink;
using DSharpPlus.Lavalink.Entities;
using DSharpPlus.Lavalink.EventArgs;
using DSharpPlus.Net;
using DSharpPlus.VoiceNext;
using NpgsqlTypes;
using Orpheus.Database;
using Orpheus.JailHandling;
using Orpheus.registerCommands;

namespace Orpheus.Audio_System
{
    public static class AudioHandler
    {
        public static async Task JoinVoiceChannel(CommandContext ctx)
        {
            await JoinVoiceChannel(getChannelToEnterAsync(ctx));
        }

        public static async Task JoinVoiceChannel(DiscordChannel ChannelToJoin)
        {
            if (ChannelToJoin == null)
            {
                Console.WriteLine("Channel To Join Is Invalid");
                return;
            }
            LavalinkExtension lava = Program.Client.GetShard(ChannelToJoin.Guild.Id).GetLavalink();
            if (!lava.ConnectedNodes.Any())
            {
                Console.WriteLine("The Lavalink connection is not established");
                return;
            }
            LavalinkNodeConnection node = lava.GetIdealNodeConnection();

            LavalinkGuildConnection conn = node.GetGuildConnection(ChannelToJoin.Guild);
            if (conn != null)
            {
                if (conn.Channel == ChannelToJoin)
                {
                    Console.WriteLine("ALREADY CONNECTED TO CHANNEL");
                    return;
                }
                Console.WriteLine("DISCONNECT TO CONNECT TO CHANNEL:" + ChannelToJoin.Name);
                await conn.DisconnectAsync();
            }

            await node.ConnectAsync(ChannelToJoin);
            Console.WriteLine($"Joined {ChannelToJoin.Name}!");

            Console.WriteLine("CONNECT TO CHANNEL:" + ChannelToJoin.Name);
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

            StoredAudioAction storedAudioActionRemove = new StoredAudioAction()
            {
                serverID = ctx.Guild.Id
            };
            RecoveryStorageHandler.RemoveAudioAction(storedAudioActionRemove);

            string tempName = conn.Channel.Name;
            await conn.DisconnectAsync();
            await ctx.Channel.SendMessageAsync($"Left {tempName}!");
            Console.WriteLine("LEAVE CHANNEL:" + tempName);
            await ctx.Message.DeleteAsync();
        }

        public static async Task PlayMusic(
            CommandContext ctx,
            string searchString,
            LavalinkSearchType lavalinkSearchType
        )
        {
            if (searchString.Length == 0)
            {
                await ctx.Channel.SendMessageAsync("SearchString Empty");
                return;
            }
            await JoinVoiceChannel(getChannelToEnterAsync(ctx));
            LavalinkExtension lava = ctx.Client.GetLavalink();
            LavalinkNodeConnection node = lava.ConnectedNodes.Values.First();
            LavalinkGuildConnection conn = node.GetGuildConnection(ctx.Member.VoiceState.Guild);

            string Author = "";
            if (searchString.ToLower().Contains("by"))
            {
                Author = searchString.ToLower().Split("by")[1].Trim();
            }
            string Title = searchString.ToLower().Split("by")[0].Trim();

            if (conn == null)
            {
                await ctx.Channel.SendMessageAsync("Lavalink is not connected.");
                return;
            }
            LavalinkLoadResult result = await node.Rest.GetTracksAsync(
                searchString,
                lavalinkSearchType
            );
            //If something went wrong on Lavalink's end
            if (
                result.LoadResultType == LavalinkLoadResultType.LoadFailed
                || result.LoadResultType == LavalinkLoadResultType.NoMatches
            )
            {
                await ctx.Channel.SendMessageAsync($"Track search failed for {searchString}.");
                return;
            }

            //TODO search through results until either best match is found or no match is found
            LavalinkTrack[] tracks = result.Tracks.ToArray();

            LavalinkTrack track = null;
            int acceptedError = 50;
            long currentTitleDist = long.MaxValue;
            long currentAuthorDist = long.MaxValue;
            foreach (LavalinkTrack trackToSearch in tracks)
            {
                long TitleDistance = LevenshteinDistance.DamerauLevenshteinDistance(
                    Title,
                    trackToSearch.Title.ToLower(),
                    acceptedError
                );
                long AuthorDistance = LevenshteinDistance.DamerauLevenshteinDistance(
                    Author,
                    trackToSearch.Author.ToLower(),
                    acceptedError
                );
                Console.ForegroundColor = ConsoleColor.Magenta;
                Console.WriteLine(
                    $"TITLE:{trackToSearch.Title.ToLower()}\n\tDISTANCE:{TitleDistance}\n  AUTHOR:{trackToSearch.Author.ToLower()}\n\tDISTANCE:{AuthorDistance}"
                );
                Console.ResetColor();
                long acceptedErrorLong = acceptedError;
                if (Author.Length != 0)
                {
                    if (AuthorDistance <= 3)
                    {
                        if (TitleDistance <= currentTitleDist)
                        {
                            Console.WriteLine("TRACK FOUND AUTHOR: " + trackToSearch.Title);
                            track = trackToSearch;
                            currentAuthorDist = AuthorDistance;
                            currentTitleDist = TitleDistance;
                        }
                        else
                        {
                            Console.WriteLine(
                                $"BETTER AUTHOR: {trackToSearch.Author} WORSE TITLE OLD:{currentTitleDist} NEW: {TitleDistance}"
                            );
                        }
                    }
                    else if (TitleDistance + AuthorDistance <= acceptedErrorLong)
                    {
                        if (TitleDistance < currentTitleDist && AuthorDistance <= currentAuthorDist)
                        {
                            Console.WriteLine("TRACK FOUND TOTAL: " + trackToSearch.Title);
                            track = trackToSearch;
                            currentAuthorDist = AuthorDistance;
                            currentTitleDist = TitleDistance;
                        }
                    }
                }
                else
                {
                    if (TitleDistance <= acceptedErrorLong)
                    {
                        if (TitleDistance < currentTitleDist)
                        {
                            Console.WriteLine("TRACK FOUND TITLE: " + trackToSearch.Title);
                            track = trackToSearch;
                            currentAuthorDist = AuthorDistance;
                            currentTitleDist = TitleDistance;
                        }
                    }
                }
            }

            if (track == null && lavalinkSearchType == LavalinkSearchType.SoundCloud)
            {
                Console.WriteLine(
                    $"Track search \"{searchString}\" not found on soundcloud, searching youtube"
                );
                await PlayMusic(ctx, searchString, LavalinkSearchType.Youtube);
            }
            else if (track == null && lavalinkSearchType == LavalinkSearchType.Youtube)
            {
                await ctx.Channel.SendMessageAsync(
                    $"Track search \"{searchString}\" could not find a match"
                );
                return;
            }
            else
            {
                _ = ctx.Channel.SendMessageAsync(
                    $"Track search \"{searchString}\" found song \"{track.Title}\" by \"{track.Author}\""
                );
                await PlayMusic(ctx, track.Uri);
            }
        }

        public static async Task PlayMusic(CommandContext ctx, Uri url)
        {
            await PlayMusic(ctx.Guild.Id, getChannelToEnterAsync(ctx).Id, url);
        }

        //PRIMARY PLAY MUSIC METHOD
        public static async Task PlayMusic(ulong serverid, ulong channelid, Uri url)
        {
            Console.WriteLine($"PLAY MUSIC URL");
            try
            {
                StoredAudioAction storedAudioActionRemove = new StoredAudioAction()
                {
                    serverID = serverid,
                };
                RecoveryStorageHandler.RemoveAudioAction(storedAudioActionRemove);
            }
            catch (Exception e)
            {
                Console.WriteLine($"END SERVER AUDIO RECOVERY FAIL:" + e.ToString());
            }
            Console.WriteLine($"PLAY MUSIC URL 2");
            //join if needed
            Console.WriteLine($"HERE");
            DiscordGuild server = await Program.Client.GetShard(serverid).GetGuildAsync(serverid);
            DiscordChannel discordChannel = server.GetChannel(channelid);
            DiscordClient client = Program.Client.GetShard(serverid);
            await JoinVoiceChannel(discordChannel);

            Console.WriteLine($"HERE 2");
            LavalinkExtension lava = client.GetLavalink();
            LavalinkNodeConnection node = lava.ConnectedNodes.Values.First();
            LavalinkGuildConnection conn = node.GetGuildConnection(server);

            Console.WriteLine($"HERE 3");
            if (conn == null)
            {
                Console.WriteLine("Lavalink is not connected.");
                return;
            }
            LavalinkLoadResult result = await node.Rest.GetTracksAsync(url);
            //If something went wrong on Lavalink's end
            if (
                result.LoadResultType == LavalinkLoadResultType.LoadFailed
                || result.LoadResultType == LavalinkLoadResultType.NoMatches
            )
            {
                Console.WriteLine($"URL search failed for {url}.");
                return;
            }
            Console.WriteLine($"URL search FOUND {url}.");
            await conn.StopAsync();
            LavalinkTrack track = result.Tracks.First();
            await conn.PlayAsync(track);

            conn.TrackException += async (guildConn, ExceptionArgs) =>
            {
                await HandlePlayError(guildConn, ExceptionArgs);
            };
            conn.TrackStuck += async (guildConn, StuckArgs) =>
            {
                await HandlePlayError(guildConn, StuckArgs);
            };
            conn.PlaybackFinished += async (guildConn, FinishArgs) =>
            {
                Console.WriteLine("REASON:" + FinishArgs.Reason);
                StoredAudioAction storedAudioActionRemove = new StoredAudioAction()
                {
                    channelID = channelid,
                    serverID = serverid,
                    Url = FinishArgs.Track.Uri.ToString(),
                };
                RecoveryStorageHandler.RemoveAudioAction(storedAudioActionRemove);
                Console.WriteLine("PLAYBACK FINISHED");
                //_ = discordChannel.SendMessageAsync("Track Finished Playback");
            };

            StoredAudioAction storedAudioAction = new StoredAudioAction()
            {
                channelID = channelid,
                serverID = serverid,
                Url = url.ToString(),
            };
            RecoveryStorageHandler.StoreAudioAction(storedAudioAction);

            Console.WriteLine($"Playing Track {track.Title}");
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
            StoredAudioAction storedAudioActionRemove = new StoredAudioAction()
            {
                serverID = ctx.Guild.Id
            };
            RecoveryStorageHandler.RemoveAudioAction(storedAudioActionRemove);
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
                await ctx.Channel.SendMessageAsync("Lavalink is not connected.");
                return;
            }
            if (conn.CurrentState.CurrentTrack == null)
            {
                await ctx.Channel.SendMessageAsync("There are no tracks loaded.");
            }
            await conn.PauseAsync();
            await ctx.Channel.SendMessageAsync($"Pausing Track");
        }

        private static async Task HandlePlayError(
            LavalinkGuildConnection conn,
            TrackExceptionEventArgs args
        )
        {
            await conn.Channel.SendMessageAsync("AUDIO PLAYBACK ERROR");
            Console.WriteLine("PLAYBACK ERROR" + args.ToString());
        }

        private static async Task HandlePlayError(
            LavalinkGuildConnection conn,
            TrackStuckEventArgs args
        )
        {
            await conn.Channel.SendMessageAsync("AUDIO PLAYBACK ERROR");
            Console.WriteLine("PLAYBACK STUCK" + args.ToString());
        }
    }
}
