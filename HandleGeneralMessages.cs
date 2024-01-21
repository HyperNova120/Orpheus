using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
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
using Orpheus.JailHandling;

namespace Orpheus
{
    public static class HandleGeneralMessages
    {
        public static async Task handleMessageCreated(
            DiscordClient sender,
            MessageCreateEventArgs args
        )
        {
            if (args.Author.IsBot)
            {
                return;
            }
            await StoreInDatabase(args);
            await FunnyBotResponses(args);

            if (
                await DBEngine.DoesEntryExist(
                    "orpheusdata.serverinfo",
                    "jailcourtid",
                    args.Channel.Id.ToString()
                )
            )
            {
                await HandleCourtMessage(args);
            }
        }

        private static async Task StoreInDatabase(MessageCreateEventArgs args)
        {
            Console.WriteLine(
                $"STORING:{args.Message.ToString()} MESSAGEID:{Convert.ToDecimal(args.Message.Id)}"
            );
            DMsg dMsg = new DMsg()
            {
                serverID = args.Guild.Id,
                channelID = args.Channel.Id,
                userID = args.Author.Id,
                sendingTime = DateTime.Now,
                msgText = OrpheusDatabaseHandler.ConvertToUFT8(args.Message.Content),
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
                    Url = OrpheusDatabaseHandler.ConvertToUFT8(attachment.Url)
                };
                await OrpheusDatabaseHandler.StoreAttachmentAsync(dAttachment);
            }
        }

        private static async Task HandleCourtMessage(MessageCreateEventArgs args)
        {
            if (args.Message.MessageType == MessageType.ChannelPinnedMessage)
            {
                return;
            }
            await JailCourtHandler.HandleJailCourtMessage(args);
        }

        private static async Task FunnyBotResponses(MessageCreateEventArgs args)
        {
            if (
                args.Author.Id == 465663563336384512 //MAFIO
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
            else if (
                args.Message.Content.Equals(
                    "https://tenor.com/view/anime-girl-cute-going-out-shopping-orange-hair-gif-8936739154264241945"
                )
            )
            {
                await args.Channel.SendMessageAsync(
                    "https://tenor.com/view/luluco-judgment-gun-morphing-gun-transform-magical-girl-gif-26460149"
                );
            }
        }
    }
}
