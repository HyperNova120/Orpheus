using System;
using System.Data.Common;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices.ComTypes;
using System.Security.Cryptography;
using DSharpPlus;
using DSharpPlus.CommandsNext;
using DSharpPlus.Entities;
using DSharpPlus.EventArgs;
using DSharpPlus.Interactivity;
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
        []-!play
        []-!pause
        []-!stop
        [x]-!on
        [x]-!off
        [x]-!dnd
        */


        public static DiscordClient Client { get; private set; }
        private static CommandsNextExtension Commands { get; set; }
        private static VoiceNextExtension VoiceNextExtension { get; set; }

        static async Task Main(string[] args)
        {
            await BotSetup();
            await Client.ConnectAsync();
            VoiceNextExtension = Client.UseVoiceNext();
            TempStorageHandler.RestartFromTempStorage();
            await Task.Delay(-1);
        }

        public static VoiceNextExtension GetVoiceNextExtension()
        {
            return VoiceNextExtension;
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

            Client = new DiscordClient(discordConfig);
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
            Commands = Client.UseCommandsNext(commandsConfig);
            Commands.RegisterCommands<TestCommands>();
            Commands.RegisterCommands<AdminCommands>();
            Commands.RegisterCommands<UserCommands>();
        }
    }
}
