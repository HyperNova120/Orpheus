using System.Dynamic;
using System.Security.Cryptography;
using DSharpPlus;
using DSharpPlus.Entities;
using DSharpPlus.EventArgs;
using Newtonsoft.Json;
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
            _ = FunnyBotResponses(args);
            if (args.Author.IsBot)
            {
                return;
            }
            _ = StoreInDatabase(args);

            if (
                await DBEngine.DoesEntryExist(
                    "orpheusdata.serverinfo",
                    "jailcourtid",
                    args.Channel.Id.ToString()
                )
            )
            {
                HandleCourtMessage(args);
            }
        }

        private static async Task StoreInDatabase(MessageCreateEventArgs args)
        {
            //Console.WriteLine(
            //   $"STORING:{args.Message.ToString()} MESSAGEID:{Convert.ToDecimal(args.Message.Id)}"
            //);
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
            Console.WriteLine($"STORED FROM USER {args.Author.Username}:{args.Message.ToString()}");

            //attachment storage handling
            DiscordAttachment[] attaches = args.Message.Attachments.ToArray();
            foreach (DiscordAttachment attachment in attaches)
            {
                Console.WriteLine($"ATTACHMENT {attachment.Url}");
                DAttachment dAttachment = new DAttachment()
                {
                    channelID = args.Channel.Id,
                    serverID = args.Guild.Id,
                    msgID = args.Message.Id,
                    userID = args.Author.Id,
                    Url = OrpheusDatabaseHandler.ConvertToUFT8(attachment.Url)
                };
                _ = OrpheusDatabaseHandler.StoreAttachmentAsync(dAttachment);
            }

            /*
                        DiscordEmbed[] embeds = args.Message.Embeds.ToArray();
                        foreach (DiscordEmbed emb in embeds)
                        {
                            Console.WriteLine($"Testing Embed URL {emb.Url.ToString().Substring(0, 23)}");
                            if (emb.Url.ToString().Substring(0, 23).Equals("https://tenor.com/view/"))
                            {
                                Console.WriteLine($"STORING GIF {emb.Url.ToString()}");
                                DGif dGif = new DGif()
                                {
                                    serverID = args.Guild.Id,
                                    gifurl = emb.Url.ToString()
                                };
                                if (await OrpheusDatabaseHandler.StoreGifAsync(dGif))
                                {
                                    Console.WriteLine($"SUCCESS STORING GIF {emb.Url.ToString()}");
                                }
                                else{
                                    Console.WriteLine($"FAIL STORING GIF {emb.Url.ToString()}");
                                }
                            }
                        }
            */
            foreach (string s in args.Message.Content.Split(" "))
            {
                if (s.Substring(0, 23).Equals("https://tenor.com/view/"))
                {
                    DGif dGif = new DGif()
                    {
                        serverID = args.Guild.Id,
                        gifurl = s
                    };
                    Console.WriteLine($"STORING GIF {s}");


                    //serverID gif delimiter ":[]:"
                    string[] json = { JsonConvert.SerializeObject($"{args.Guild.Id}:[]:{s}") };
                    File.AppendAllLines("gifs.txt", json);

                    if (await OrpheusDatabaseHandler.StoreGifAsync(dGif))
                    {
                        Console.WriteLine($"SUCCESS STORING GIF {s}");
                    }
                    else
                    {
                        Console.WriteLine($"FAIL STORING GIF {s}");
                    }
                }
            }
        }

        private static void HandleCourtMessage(MessageCreateEventArgs args)
        {
            if (args.Message.MessageType == MessageType.ChannelPinnedMessage)
            {
                return;
            }
            _ = JailCourtHandler.HandleJailCourtMessage(args);
        }

        private static async Task FunnyBotResponses(MessageCreateEventArgs args)
        {
            if (
                //args.Author.Id == 465663563336384512 //MAFIO
                /*&&*/ args.Message.Content.Equals(
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
                ) || args.Message.Content.Equals(
                    "https://tenor.com/view/did-you-pray-today-gif-5116018886993652813"
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

            Random rand = new Random();
            int ran = rand.Next(0, 100);
            if (ran <= 10)
            {
                Console.WriteLine("Sending Gif");
                //post funny bot response
                string[] lines = File.ReadAllLines("gifs.txt");

                List<string> gifs = new List<string>();
                HashSet<string> uniqueGifs = new HashSet<string>();

                for (int i = 0; i < lines.Length; i++)
                {
                    string s = lines[i].Replace("\"", "");

                    if (s.Split(":[]:")[0].Equals(args.Guild.Id.ToString()))
                    {
                        gifs.Add(s.Split(":[]:")[1]);
                    }
                    uniqueGifs.Add(s);
                }

                File.WriteAllLines("gifs.txt", uniqueGifs.ToArray());
                string responseGif = gifs.ToArray()[rand.Next(0, gifs.Count)];
                await args.Channel.SendMessageAsync(responseGif);
            }
        }
    }
}
