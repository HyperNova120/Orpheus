using System;
using System.Data.Common;
using System.Diagnostics;
using System.Net.Mime;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices.ComTypes;
using System.Runtime.Serialization.Json;
using System.Security.Cryptography;
using DSharpPlus;
using DSharpPlus.CommandsNext;
using DSharpPlus.Entities;
using DSharpPlus.EventArgs;
using DSharpPlus.Interactivity;
using DSharpPlus.Lavalink;
using DSharpPlus.Net;
using DSharpPlus.SlashCommands;
using DSharpPlus.VoiceNext;
using Npgsql.Replication;
using Orpheus.commands;
using Orpheus.Database;

namespace Orpheus // Note: actual namespace depends on the project name.
{
    internal class Program
    {
        /*
        [x]- !say
        [x]-!dm
        []-rand gif
        [x]-!send
        [x]-!join
        [x]-!leave
        [X]-!play
        [X]-!pause
        [X]-!stop
        [x]-!on
        [x]-!off
        [x]-!dnd
        */


        public static DiscordShardedClient Client { get; private set; }
        private static Dictionary<int, CommandsNextExtension> Commands =
            new Dictionary<int, CommandsNextExtension>();
        private static Dictionary<int, VoiceNextExtension> voiceNextExtension =
            new Dictionary<int, VoiceNextExtension>();
        private static Dictionary<int, LavalinkExtension> lavaLinkExtension =
            new Dictionary<int, LavalinkExtension>();
        private static Dictionary<int, LavalinkNodeConnection> lavaLinkNodeConnection =
            new Dictionary<int, LavalinkNodeConnection>();

        static async Task Main(string[] args)
        {
            await BotSetup();
            await Client.StartAsync();
            //VoiceNextExtension = ;
            VoiceNextConfiguration voiceConfiguration = new VoiceNextConfiguration()
            {
                EnableIncoming = false,
            };

            foreach (
                KeyValuePair<
                    int,
                    VoiceNextExtension
                > keyValuePair in await Client.UseVoiceNextAsync(voiceConfiguration)
            )
            {
                voiceNextExtension.Add(keyValuePair.Key, keyValuePair.Value);
            }
            await setupLavalink();

            RecoveryStorageHandler.InitiateRecovery();
            await Task.Delay(-1);
        }

        private static async Task setupLavalink()
        {
            JSONReader jsonReader = new JSONReader();
            await jsonReader.ReadJson();
            ConnectionEndpoint connectionEndpoint = new ConnectionEndpoint()
            {
                Hostname = jsonReader.lavalinkConfig.hostName,
                Port = jsonReader.lavalinkConfig.port
            };

            LavalinkConfiguration lavalinkConfiguration = new LavalinkConfiguration()
            {
                Password = jsonReader.lavalinkConfig.password,
                RestEndpoint = connectionEndpoint,
                SocketEndpoint = connectionEndpoint,
            };

            foreach (KeyValuePair<int, LavalinkExtension> temp in await Client.UseLavalinkAsync())
            {
                lavaLinkExtension.Add(temp.Key, temp.Value);
            }

            foreach (KeyValuePair<int, LavalinkExtension> temp in lavaLinkExtension)
            {
                lavaLinkNodeConnection.Add(
                    temp.Key,
                    await temp.Value.ConnectAsync(lavalinkConfiguration)
                );
            }
        }

        public static VoiceNextExtension GetVoiceNextExtension()
        {
            voiceNextExtension.TryGetValue(0, out VoiceNextExtension tempVoiceNextExtension);
            return tempVoiceNextExtension;
        }

        private static Task Client_Ready(DiscordClient sender, ReadyEventArgs args)
        {
            return Task.CompletedTask;
        }

        private static async Task runRegisterServerIfNeeded(GuildCreateEventArgs args)
        {
            //register or update server
            AdminCommands temp = new AdminCommands();
            await temp.RegisterServer(args);

            //create or update all user registry
            foreach (KeyValuePair<ulong, DiscordMember> keyValuePair in args.Guild.Members)
            {
                DiscordMember member = keyValuePair.Value;
                if (member.IsBot)
                {
                    continue;
                }
                DUser dUser = new DUser() { userId = member.Id, username = member.Username, };
                _ = OrpheusDatabaseHandler.StoreUserAsync(dUser);
            }
        }

        public static async Task SetDiscordStatus(
            DiscordActivity discordActivity,
            UserStatus userStatus
        )
        {
            await Client.UpdateStatusAsync(discordActivity, userStatus);
        }

        private static async Task handleUserJoined(DiscordClient user, GuildMemberAddEventArgs args)
        {
            DUser dUser = new DUser() { userId = args.Member.Id, username = args.Member.Username, };

            bool isStored = await OrpheusDatabaseHandler.StoreUserAsync(dUser);
            if (isStored)
            {
                Console.WriteLine("Succesfully stored in Database");
            }
            else
            {
                Console.WriteLine("Failed to store in Database");
            }
        }

        private static async Task BotSetup()
        {
            JSONReader jsonReader = new JSONReader();
            await jsonReader.ReadJson();
            DiscordConfiguration discordConfig = new DiscordConfiguration()
            {
                Intents = DiscordIntents.All,
                Token = jsonReader.token,
                TokenType = TokenType.Bot,
                AutoReconnect = true
            };

            Client = new DiscordShardedClient(discordConfig);

            Client.Ready += Client_Ready;
            Client.MessageCreated += async (user, args) =>
            {
                _ = HandleGeneralMessages.handleMessageCreated(user, args);
            };

            Client.GuildAvailable += async (c, args) =>
            {
                await runRegisterServerIfNeeded(args);
            };
            Client.GuildMemberAdded += async (user, args) =>
            {
                await handleUserJoined(user, args);
            };
            Client.GuildCreated += async (c, args) =>
            {
                await runRegisterServerIfNeeded(args);
            };

            CommandsNextConfiguration commandsConfig = new CommandsNextConfiguration()
            {
                StringPrefixes = new string[] { jsonReader.prefix },
                EnableMentionPrefix = true,
                EnableDms = true,
                EnableDefaultHelp = false,
            };
            foreach (
                KeyValuePair<
                    int,
                    CommandsNextExtension
                > values in await Client.UseCommandsNextAsync(commandsConfig)
            )
            {
                if (values.Value == null)
                {
                    continue;
                }
                Commands.Add(values.Key, values.Value);
            }
            Commands.RegisterCommands<TestCommands>();
            Commands.RegisterCommands<AdminCommands>();
            Commands.RegisterCommands<UserCommands>();
        }
    }
}
