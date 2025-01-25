using System.Diagnostics;
using DSharpPlus;
using DSharpPlus.CommandsNext;
using DSharpPlus.Entities;
using DSharpPlus.EventArgs;
using DSharpPlus.Lavalink;
using DSharpPlus.Net;
using DSharpPlus.SlashCommands;
using DSharpPlus.VoiceNext;
using Orpheus.commands;
using Orpheus.Database;
using Lavalink4NET.DSharpPlus;
using Microsoft.Extensions.DependencyInjection;
using Lavalink4NET.Extensions;
using Lavalink4NET.DSharpPlus;
using Microsoft.Extensions.DependencyInjection;
using Lavalink4NET.Extensions;
namespace Orpheus // Note: actual namespace depends on the project name.
{
    internal class Program
    {
        /*
        [x]- !say
        [x]-!dm
        []-rand gif
        [x]-!send
        [x]-!joinPrelo
        [x]-!joinPrelo
        [x]-!leave
        [X]-!play
        [X]-!pause
        [X]-!stop
        [x]-!on
        [x]-!off
        [x]-!dnd
        */



        public static DiscordShardedClient ShardedClient { get; private set; }
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
           /*  AppDomain.CurrentDomain.ProcessExit += async (sender, e) =>
            {
                //on application close
                Console.WriteLine("App Exit");
                await LLHandler.Close();
            };
            Console.CancelKeyPress += async delegate (object? sender, ConsoleCancelEventArgs e)
            {
                e.Cancel = true;
                Console.WriteLine("App Exit");
                await LLHandler.Close();
                e.Cancel = false;
            };
            await LLHandler.Setup(); */


            JSONReader jsonReader = new JSONReader();
            await jsonReader.ReadJson();
            await BotSetup();
            await ShardedClient.StartAsync();
            //await setupVoiceNext();
            //await setupLavalink();

            RecoveryStorageHandler.InitiateRecovery();
            await Task.Delay(-1);
                LLHandler.Close();
        }

        private static async Task setupVoiceNext()
        {

            VoiceNextConfiguration voiceConfiguration = new VoiceNextConfiguration()
            {
                EnableIncoming = false,
            };

            foreach (
                KeyValuePair<
                    int,
                    VoiceNextExtension
                > keyValuePair in await ShardedClient.UseVoiceNextAsync(voiceConfiguration)
            )
            {
                voiceNextExtension.Add(keyValuePair.Key, keyValuePair.Value);
            }
        }

        private static async Task setupLavalink()
        {
            JSONReader jsonReader = new JSONReader();
            await jsonReader.ReadJson();
            Console.WriteLine($"LAVALINK INFO: HOST:{jsonReader.lavalinkConfig.hostName}    PORT:{jsonReader.lavalinkConfig.port}");
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

            foreach (KeyValuePair<int, LavalinkExtension> temp in await ShardedClient.UseLavalinkAsync())
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
            voiceNextExtension.TryGetValue(0, out VoiceNextExtension? tempVoiceNextExtension);
            return tempVoiceNextExtension;
        }

        private static Task Client_Ready(DiscordClient sender, ReadyEventArgs args)
        {
            return Task.CompletedTask;
        }

        private static async Task runRegisterServerIfNeeded(GuildCreateEventArgs args)
        {
            //register or update server
            if (DBEngine.doesServerPropertiesExist(args.Guild.Id))
            {
                return;
            }
            AdminCommands temp = new AdminCommands();
            temp.RegisterServer(args);
        }

        public static async Task SetDiscordStatus(
            DiscordActivity discordActivity,
            UserStatus userStatus
        )
        {
            await ShardedClient.UpdateStatusAsync(discordActivity, userStatus);
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

            ShardedClient = new DiscordShardedClient(discordConfig);

            ShardedClient.Ready += Client_Ready;
            ShardedClient.MessageCreated += async (user, args) =>
            {
                await HandleGeneralMessages.handleMessageCreated(user, args);
            };

            ShardedClient.GuildAvailable += async (c, args) =>
            {
                await runRegisterServerIfNeeded(args);
            };
            ShardedClient.GuildCreated += async (c, args) =>
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
                > values in await ShardedClient.UseCommandsNextAsync(commandsConfig)
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
