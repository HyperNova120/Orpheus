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
//using DSharpPlus.Lavalink;
//using DSharpPlus.Net;
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



        public static DiscordClient OrpheusClient { get; private set; }

        static async Task Main(string[] args)
        {
            AppDomain.CurrentDomain.ProcessExit += async (object sender, EventArgs e) =>
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
                await JSONReader.ReadJson();
                await BotSetup();
                RecoveryStorageHandler.InitiateRecovery();
                await Task.Delay(-1);
            }
            catch (Exception e)
            {
                Console.WriteLine("EMERGENCY CLOSE");
                await LLHandler.Close();
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
            DiscordClientBuilder discordClientBuilder = DiscordClientBuilder.CreateDefault(JSONReader.token, DiscordIntents.All);

            discordClientBuilder.ConfigureEventHandlers
            (
                b => b.HandleMessageCreated(async (user, args) =>
                {
                    await HandleGeneralMessages.handleMessageCreated(user, args);
                })
                .HandleGuildAvailable(async (c, args) => { runRegisterServerIfNeeded(args); })
                .HandleGuildCreated(async (c, args) => { runRegisterServerIfNeeded(args); })
            );

            CommandsNextConfiguration commandsConfig = new CommandsNextConfiguration()
            {
                StringPrefixes = new string[] { JSONReader.prefix },
                EnableMentionPrefix = true,
                EnableDms = true,
                EnableDefaultHelp = false,
            };
            discordClientBuilder.UseCommandsNext(Commands =>
            {
                Commands.RegisterCommands<TestCommands>();
                Commands.RegisterCommands<AdminCommands>();
                Commands.RegisterCommands<UserCommands>();
            }, commandsConfig);



            VoiceNextConfiguration voiceConfiguration = new VoiceNextConfiguration()
            {
                EnableIncoming = false,
            };
            discordClientBuilder.UseVoiceNext(voiceConfiguration);

            Console.WriteLine($"LAVALINK INFO: HOST:{JSONReader.lavalinkConfig.hostName}    PORT:{JSONReader.lavalinkConfig.port}");
            await LLHandler.Setup();

            LavalinkNodeOptions lavalinkNodeOptions = new LavalinkNodeOptions();


            var serviceProvider = new ServiceCollection()
            .AddDiscordClient(JSONReader.token, DiscordIntents.All)
            .AddSingleton<IAudioService, LavalinkNode>()
            .AddSingleton<IDiscordClientWrapper, DiscordClientWrapper>()
            .AddSingleton<LavalinkNodeOptions>()
            .BuildServiceProvider();

            OrpheusClient = discordClientBuilder.Build();
            await OrpheusClient.ConnectAsync();
        }
    }
}
