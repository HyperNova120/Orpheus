using System;
using System.Data.Common;
using System.Runtime.CompilerServices;
using DSharpPlus;
using DSharpPlus.CommandsNext;
using DSharpPlus.Entities;
using DSharpPlus.EventArgs;
using DSharpPlus.Interactivity;
using Orpheus.commands;
using Orpheus.Database;

namespace Orpheus // Note: actual namespace depends on the project name.
{
    internal class Program
    {
        private static DiscordClient Client { get; set; }
        private static CommandsNextExtension Commands { get; set; }

        static async Task Main(string[] args)
        {
            await BotSetup();

            await Client.ConnectAsync();
            await Task.Delay(-1);
        }

        private static Task Client_Ready(DiscordClient sender, ReadyEventArgs args)
        {
            return Task.CompletedTask;
        }

        private static async Task runRegisterServerIfNeeded(GuildCreateEventArgs args)
        {
            OrpheusDatabaseHandler handler = new OrpheusDatabaseHandler();

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
                await handler.StoreUserAsync(dUser);
            }
        }

        private static async Task handleMessageCreated(
            DiscordClient sender,
            MessageCreateEventArgs args
        )
        {
            if (args.Author.IsBot)
            {
                return;
            }
            string messageContent = args.Message.Content.Replace("'","''");
            Console.WriteLine();
            Console.WriteLine($"STORING:{args.Message.ToString()}");
            DMsg dMsg = new DMsg()
            {
                serverID = args.Guild.Id,
                channelID = args.Channel.Id,
                userID = args.Author.Id,
                sendingTime = DateTime.Now,
                msgText = messageContent
            };
            OrpheusDatabaseHandler handler = new OrpheusDatabaseHandler();
            await handler.StoreMsgAsync(dMsg);
            //await args.Channel.SendMessageAsync("GRABBED:" + s);
        }

        private static async Task handleUserJoined(DiscordClient user, GuildMemberAddEventArgs args)
        {
            OrpheusDatabaseHandler handler = new OrpheusDatabaseHandler();
            DUser dUser = new DUser() { userId = args.Member.Id, username = args.Member.Username, };

            bool isStored = await handler.StoreUserAsync(dUser);
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
                await handleMessageCreated(user, args);
            };

            Client.GuildAvailable += async (c, args) =>
            {
                await runRegisterServerIfNeeded(args);
            };
            Client.GuildMemberAdded += async (user, args) =>
            {
                await handleUserJoined(user, args);
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
