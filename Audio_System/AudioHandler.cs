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

        /// <summary>
        /// joins the voice channel ChannelToJoin. 
        /// the bot will disconnect from any voice channel it is currently in and connect to the given one
        /// </summary>
        /// <param name="ChannelToJoin"></param>
        /// <returns></returns>
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

        /// <summary>
        /// returns the discord channel to join. if user who send the command is in a voice channel already will return that channel. 
        /// otherwise joins the channel the message is sent in if it is a voice channel.
        /// otherwise returns null.
        /// </summary>
        /// <param name="ctx"></param>
        /// <returns></returns>
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
                Console.WriteLine("NO CHANNEL TO ENTER");
                return null;
            }
        }

        /// <summary>
        /// disconnects from the voice channel the bot is in. does nothing if not connected
        /// </summary>
        /// <param name="ctx"></param>
        /// <returns></returns>
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

        /// <summary>
        /// joins appropriate voice channel, then searches soundcloud for the best match. 
        /// if no match is found it will search youtube and return the best match 
        /// </summary>
        /// <param name="ctx"></param>
        /// <param name="searchString"></param>
        /// <param name="lavalinkSearchType"></param>
        /// <returns></returns>
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
            Console.WriteLine("CTX.CLIENT:" + ctx.Client.ToString());
            LavalinkExtension lava = Program.Client.GetShard(ctx.Guild.Id).GetLavalink();
            LavalinkNodeConnection node = lava.ConnectedNodes.Values.First();
            LavalinkGuildConnection conn = node.GetGuildConnection(ctx.Member.Guild);

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

            LavalinkTrack[] tracks = result.Tracks.ToArray();

            LavalinkTrack track = getBestTrackMatch(tracks, lavalinkSearchType, Title, Author);
            await HandleReturnedTrack(track, lavalinkSearchType, searchString, ctx);
        }

        /// <summary>
        /// decides what to do with best match track, 
        /// if is searching soundcloud and track is not a good enough match searches youtube,
        /// if searching youtube calls playMusic(ctx, url)
        /// </summary>
        /// <param name="track"></param>
        /// <param name="lavalinkSearchType"></param>
        /// <param name="searchString"></param>
        /// <param name="ctx"></param>
        /// <returns></returns>
        private static async Task HandleReturnedTrack(
            LavalinkTrack track,
            LavalinkSearchType lavalinkSearchType,
            string searchString,
            CommandContext ctx
        )
        {
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

        /// <summary>
        /// returns the best match to the title and author from a track list
        /// prioritizes author correctness over title. 
        /// if tracks are searching soundcloud requires author to be within 3 errors to match
        /// if tracks are searching youtube returns a track if the author + title errors are less than accepted error margin
        /// otherwise returns null
        /// </summary>
        /// <param name="tracks"></param>
        /// <param name="lavalinkSearchType"></param>
        /// <param name="Title"></param>
        /// <param name="Author"></param>
        /// <returns></returns>
        private static LavalinkTrack getBestTrackMatch(
            LavalinkTrack[] tracks,
            LavalinkSearchType lavalinkSearchType,
            string Title,
            string Author
        )
        {
            Console.WriteLine("TRACK RESULT SIZE:" + tracks.Length);
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
                    if (lavalinkSearchType == LavalinkSearchType.SoundCloud)
                    {
                        if (TitleDistance < currentTitleDist && AuthorDistance <= 3)
                        {
                            Console.WriteLine("TRACK FOUND TOTAL: " + trackToSearch.Title);
                            track = trackToSearch;
                            currentAuthorDist = AuthorDistance;
                            currentTitleDist = TitleDistance;
                        }
                    } //youtube
                    else if (AuthorDistance <= 3)
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
                    else if (TitleDistance <= acceptedErrorLong)
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
            return track;
        }

        /// <summary>
        /// gets the serverID, channelID, url
        /// and calls play music
        /// </summary>
        /// <param name="ctx"></param>
        /// <param name="url"></param>
        /// <returns></returns>
        public static async Task PlayMusic(CommandContext ctx, Uri url)
        {
            await PlayMusic(ctx.Guild.Id, getChannelToEnterAsync(ctx).Id, url, TimeSpan.Zero);
        }

        /// <summary>
        /// PRIMARY PLAYMUSIC METHOD
        /// gets the server, channel, and timespan
        /// connects to given voice channel and plays the sound from the given URL
        /// updates recovery storage with the current track and timestamp
        /// removes audio from recovery storage when done playing
        /// </summary>
        /// <param name="serverid"></param>
        /// <param name="channelid"></param>
        /// <param name="url"></param>
        /// <param name="timeSpan"></param>
        /// <returns></returns>
        public static async Task PlayMusic(
            ulong serverid,
            ulong channelid,
            Uri url,
            TimeSpan timeSpan
        )
        {
            Console.WriteLine($"PLAY MUSIC URL");
            StoredAudioAction storedAudioActionRemove = new StoredAudioAction()
            {
                serverID = serverid,
            };
            RecoveryStorageHandler.RemoveAudioAction(storedAudioActionRemove);

            //join if needed
            DiscordGuild server = await Program.Client.GetShard(serverid).GetGuildAsync(serverid);
            DiscordChannel discordChannel = server.GetChannel(channelid);
            DiscordClient client = Program.Client.GetShard(serverid);

            await JoinVoiceChannel(discordChannel);
            LavalinkExtension lava = client.GetLavalink();
            LavalinkNodeConnection node = lava.ConnectedNodes.Values.First();
            LavalinkGuildConnection conn = node.GetGuildConnection(server);

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

            conn.PlaybackFinished += async (guildConn, FinishArgs) =>
            {
                await HandleTrackFinishedUpdate(guildConn, FinishArgs, channelid, serverid);
            };
            conn.PlayerUpdated += async (guildConn, EventArgs) =>
            {
                await HandlePlayerUpdate(
                    guildConn,
                    EventArgs,
                    channelid,
                    serverid,
                    url.ToString()
                );
            };

            LavalinkTrack track = result.Tracks.First();
            await conn.PlayAsync(track);
            if (timeSpan.Seconds != 0)
            {
                await conn.SeekAsync(timeSpan);
            }
            else
            {
                Console.WriteLine("SKIP SEEK");
            }

            StoredAudioAction storedAudioAction = new StoredAudioAction()
            {
                channelID = channelid,
                serverID = serverid,
                Url = url.ToString(),
                position = timeSpan
            };
            RecoveryStorageHandler.StoreAudioAction(storedAudioAction);

            Console.WriteLine($"Playing Track {track.Title}");
        }

        /// <summary>
        /// resumes any currently paused music
        /// </summary>
        /// <param name="ctx"></param>
        /// <returns></returns>
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

        /// <summary>
        /// stops playing any current music, un-resumable
        /// removes it from recovery storage
        /// </summary>
        /// <param name="ctx"></param>
        /// <returns></returns>
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

        /// <summary>
        /// pauses the current music, leaves it in queue to be resumed
        /// </summary>
        /// <param name="ctx"></param>
        /// <returns></returns>
        public static async Task PauseMusic(CommandContext ctx)
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

        /// <summary>
        /// removes audio from recovery storage when audio finishes playing
        /// </summary>
        /// <param name="conn"></param>
        /// <param name="args"></param>
        /// <param name="channelid"></param>
        /// <param name="serverid"></param>
        /// <returns></returns>
        private static async Task HandleTrackFinishedUpdate(
            LavalinkGuildConnection conn,
            TrackFinishEventArgs args,
            ulong channelid,
            ulong serverid
        )
        {
            StoredAudioAction storedAudioActionRemove = new StoredAudioAction()
            {
                channelID = channelid,
                serverID = serverid,
                Url = args.Track.Uri.ToString(),
            };
            RecoveryStorageHandler.RemoveAudioAction(storedAudioActionRemove);
        }

        /// <summary>
        /// updates recovery storage with the current timestamp of the music
        /// </summary>
        /// <param name="conn"></param>
        /// <param name="args"></param>
        /// <param name="channelid"></param>
        /// <param name="serverid"></param>
        /// <param name="url"></param>
        /// <returns></returns>
        private static async Task HandlePlayerUpdate(
            LavalinkGuildConnection conn,
            PlayerUpdateEventArgs args,
            ulong channelid,
            ulong serverid,
            string url
        )
        {
            StoredAudioAction storedAudioAction = new StoredAudioAction()
            {
                channelID = channelid,
                serverID = serverid,
                Url = url.ToString(),
                position = args.Position
            };
            RecoveryStorageHandler.StoreAudioAction(storedAudioAction);
        }
    }
}
