using DSharpPlus;
using DSharpPlus.CommandsNext;
using DSharpPlus.Entities;
using DSharpPlus.EventArgs;
using DSharpPlus.VoiceNext;
using Orpheus.commands;
using Orpheus.Database;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Lavalink4NET.DSharpPlus;
using Microsoft.Extensions.Hosting;
using Lavalink4NET;
using DSharpPlus.Extensions;
using Newtonsoft.Json;
using Orpheus;
using Lavalink4NET.Extensions;
//using DSharpPlus.Lavalink;
//using DSharpPlus.Net;



namespace Orpheus
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
        public static DiscordClient OrpheusClient { get; private set; }

        static async Task Main(string[] args)
        {
            AppDomain.CurrentDomain.ProcessExit += async (object? sender, EventArgs e) =>
            {
                //on application close
                Console.WriteLine("App Exit ProcessExit");
                await LLHandler.Close();
            };
            Console.CancelKeyPress += async delegate (object? sender, ConsoleCancelEventArgs e)
            {
                e.Cancel = true;
                Console.WriteLine("App Exit CancelKeyPress");
                await LLHandler.Close();
                e.Cancel = false;
            };

            try
            {
                await ConfigReader.ReadConfig();
                await BotSetup();
                RecoveryStorageHandler.InitiateRecovery();
                await Task.Delay(-1);
            }
            catch (Exception e)
            {
                Console.WriteLine("EMERGENCY CLOSE");
                await LLHandler.Close();
                Console.WriteLine(e.ToString());
            }
        }

        private static void runRegisterServerIfNeeded(GuildCreatedEventArgs args)
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
            DiscordUserStatus userStatus
        )
        {
            await OrpheusClient.UpdateStatusAsync(discordActivity, userStatus);
        }

        private static async Task BotSetup()
        {
            IServiceCollection serviceCollection = new ServiceCollection();
            serviceCollection.AddDiscordClient(ConfigReader.token, DiscordIntents.All);
            serviceCollection.ConfigureEventHandlers(
                b => b.HandleMessageCreated(async (user, args) =>
                {
                    await HandleGeneralMessages.handleMessageCreated(user, args);
                })
                .HandleGuildAvailable(async (c, args) => { runRegisterServerIfNeeded(args); })
                .HandleGuildCreated(async (c, args) => { runRegisterServerIfNeeded(args); })
                .HandleComponentInteractionCreated(async (client, args) => { await HandleButtonInteractions.ButtonInteractionSwitch(args); })
            );

            CommandsNextConfiguration commandsConfig = new CommandsNextConfiguration()
            {
                StringPrefixes = new string[] { ConfigReader.prefix },
                EnableMentionPrefix = true,
                EnableDms = true,
                EnableDefaultHelp = false,
            };
            serviceCollection.AddCommandsNextExtension(commands =>
            {
                commands.RegisterCommands<TestCommands>();
                commands.RegisterCommands<AdminCommands>();
                commands.RegisterCommands<UserCommands>();
                commands.RegisterCommands<MusicCommands>();
            }, commandsConfig);
            VoiceNextConfiguration voiceConfiguration = new VoiceNextConfiguration()
            {
                EnableIncoming = false,
            };
            serviceCollection.AddVoiceNextExtension(voiceConfiguration);
            serviceCollection.AddLavalink();
            serviceCollection.ConfigureLavalink(config =>
            {
                config.BaseAddress = new Uri($"http://{ConfigReader.lavalinkConfig.hostName}:{ConfigReader.lavalinkConfig.port}");
                config.WebSocketUri = new Uri($"ws://{ConfigReader.lavalinkConfig.hostName}:{ConfigReader.lavalinkConfig.port}/v4/websocket");
                config.ReadyTimeout = TimeSpan.FromSeconds(10);
                config.ResumptionOptions = new LavalinkSessionResumptionOptions(TimeSpan.FromSeconds(60));
                config.Label = "Node Alpha";
                config.Passphrase = ConfigReader.lavalinkConfig.password;
                config.HttpClientName = "LavalinkHttpClient";
            });


            serviceCollection.AddLogging(s => s.AddConsole().SetMinimumLevel(LogLevel.Debug));
            ServiceProvider serviceProvider = serviceCollection.BuildServiceProvider();


            OrpheusClient = serviceProvider.GetRequiredService<DiscordClient>();
            await LLHandler.Setup();
            await OrpheusClient.ConnectAsync();
            await MusicModule.SetUp(serviceProvider.GetRequiredService<IAudioService>());
        }
    }
}
