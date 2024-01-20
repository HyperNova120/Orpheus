using System;
using System.Data.Common;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices.ComTypes;
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
            FunnyBotResponses(args);
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

        private static async Task FunnyBotResponses(MessageCreateEventArgs args)
        {
            if (
                args.Author.Id == 465663563336384512
                && args.Message.Content.Equals(
                    "https://tenor.com/view/did-you-pray-today-gif-5116018886993652813"
                )
            )
            {
                await args.Channel.SendMessageAsync(
                    "https://tenor.com/view/pray-praying-dickies-gangster-gangsta-gif-1281706038007242304"
                );
            }
            else if (
                args.Message.Content.Equals(
                    "https://tenor.com/view/one-piece-one-piece-zoro-zoro-aight-im-going-to-bed-one-piece-sleep-gif-26290499"
                )
            )
            {
                await args.Channel.SendMessageAsync(
                    "https://tenor.com/view/goodnight-fat-cat-gif-25641116"
                );
            }
            else if (
                args.Message.Content.Equals(
                    "https://tenor.com/view/gojo-gojo-satoru-gojo-season-2-hip-thrust-reaction-gif-10399129046512126318"
                )
            )
            {
                await args.Channel.SendMessageAsync(
                    "https://tenor.com/view/cat-suckin-it-finger-troll-cat-gif-21799760"
                );
            }
            else if (args.Message.Content.Equals("https://tenor.com/view/anime-girl-cute-going-out-shopping-orange-hair-gif-8936739154264241945"))
            {
                await args.Channel.SendMessageAsync(
                    "https://tenor.com/view/luluco-judgment-gun-morphing-gun-transform-magical-girl-gif-26460149"
                );

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
