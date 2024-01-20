using System;
using System.Data.Common;
using System.Runtime.CompilerServices;
using DSharpPlus;
using DSharpPlus.CommandsNext;
using DSharpPlus.Entities;
using DSharpPlus.EventArgs;
using DSharpPlus.Interactivity;
using DSharpPlus.VoiceNext;
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


        private static DiscordClient Client { get; set; }
        private static CommandsNextExtension Commands { get; set; }
        private static VoiceNextExtension voiceNextExtension { get; set; }

        static async Task Main(string[] args)
        {
            await BotSetup();

            await Client.ConnectAsync();
            voiceNextExtension = Client.UseVoiceNext();
            await Task.Delay(-1);
        }

        public static VoiceNextExtension GetVoiceNextExtension()
        {
            return voiceNextExtension;
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
                await OrpheusDatabaseHandler.StoreUserAsync(dUser);
            }
        }

        public static async Task SetDiscordStatus(
            DiscordActivity discordActivity,
            UserStatus userStatus
        )
        {
            await Client.UpdateStatusAsync(discordActivity, userStatus);
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
            string messageContent = args.Message.Content.Replace("'", "''");
            Console.WriteLine(
                $"STORING:{args.Message.ToString()} MESSAGEID:{Convert.ToDecimal(args.Message.Id)}"
            );
            DMsg dMsg = new DMsg()
            {
                serverID = args.Guild.Id,
                channelID = args.Channel.Id,
                userID = args.Author.Id,
                sendingTime = DateTime.Now,
                msgText = OrpheusDatabaseHandler.ConvertToUFT8(messageContent),
                dmsgID = args.Message.Id
            };
            await OrpheusDatabaseHandler.StoreMsgAsync(dMsg);
            Console.WriteLine($"STORED:{args.Message.ToString()}");

            //attachment storage handling
            DiscordAttachment[] attaches = args.Message.Attachments.ToArray();
            if (attaches.Length > 0)
            {
                //await args.Channel.SendMessageAsync($"ATTACHMENT {attaches[0].Url}");
                Console.WriteLine($"ATTACHMENT {attaches[0].Url}");
            }
            foreach (DiscordAttachment attachment in attaches)
            {
                DAttachment dAttachment = new DAttachment()
                {
                    channelID = args.Channel.Id,
                    serverID = args.Guild.Id,
                    msgID = args.Message.Id,
                    userID = args.Author.Id,
                    url = OrpheusDatabaseHandler.ConvertToUFT8(attachment.Url.Replace("'", "''"))
                };
                await OrpheusDatabaseHandler.StoreAttachmentAsync(dAttachment);
            }
            //await args.Channel.SendMessageAsync("GRABBED:" + s);
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
                handleMessageCreated(user, args);
            };

            Client.GuildAvailable += async (c, args) =>
            {
                runRegisterServerIfNeeded(args);
            };
            Client.GuildMemberAdded += async (user, args) =>
            {
                handleUserJoined(user, args);
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
