using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Linq;
using System.Security.Cryptography;
using System.Threading;
using System.Threading.Tasks;
using DSharpPlus;
using DSharpPlus.Entities;
using DSharpPlus.Interactivity.Extensions;
using Npgsql.Replication;
using Orpheus.JailHandling;

namespace Orpheus.Voting
{
    public static class HandleVote
    {

        public static async Task<bool> StartVote_V2(DiscordChannel channel, CountdownTimer countdownTimer, string voteType, string title, string description, ulong referencedUser, Func<Task<bool>> cancelCondition)
        {
            //create vars
            DiscordClient client = Program.ShardedClient.GetShard((ulong)channel.GuildId);
            DiscordEmoji thumbUp = DiscordEmoji.FromName(client, ":thumbsup:");
            DiscordEmoji thumbDown = DiscordEmoji.FromName(client, ":thumbsdown:");

            //create message
            DiscordMessageBuilder messageBuilder = new DiscordMessageBuilder();
            /*DiscordEmbedBuilder discordEmbedBuilder = new DiscordEmbedBuilder
            {
                Title = title,
                Description = description,
                Color = DiscordColor.Azure
            };*/

            
            messageBuilder.AddEmbed(createActiveCountdownEmbed(countdownTimer, title, description, DiscordColor.Green));
            DiscordMessage message = await messageBuilder.SendAsync(channel);
            _ = message.CreateReactionAsync(thumbUp);
            await message.CreateReactionAsync(thumbDown);

            StoredVoteMessage storedVoteMessage = new StoredVoteMessage()
            {
                serverID = channel.Guild.Id,
                channelID = channel.Id,
                messageID = message.Id,
                userID = referencedUser,
                voteType = voteType,
                storedCountdownTimerSeconds = countdownTimer.getTotalSecondsRemaining()
            };
            RecoveryStorageHandler.StoreVoteMessage(storedVoteMessage);

            return await UpdateVoteAsync(message, countdownTimer, voteType, title, description, referencedUser, cancelCondition);
        }

        public static async Task<bool> UpdateVoteAsync(DiscordMessage message, CountdownTimer countdownTimer, string voteType, string title, string description, ulong referencedUser, Func<Task<bool>> cancelCondition)
        {
            Console.WriteLine("UpdateVoteAsync");
            StoredVoteMessage storedVoteMessage = new StoredVoteMessage()
            {
                serverID = (ulong)message.Channel.GuildId,
                channelID = message.ChannelId,
                messageID = message.Id,
                userID = referencedUser,
                voteType = voteType,
                storedCountdownTimerSeconds = countdownTimer.getTotalSecondsRemaining()
            };

            DiscordClient shardClient = Program.ShardedClient.GetShard((ulong)message.Channel.GuildId);

            bool finished = await handleActiveVoteAsync(message, countdownTimer, voteType, title, description, referencedUser, cancelCondition, storedVoteMessage);
            if (!finished)
            {
                //if vote has been canceled
                Console.WriteLine("Vote Canceled");
                return false;
            }
            else{
                Console.WriteLine("Vote Finished");
            }

            //tally votes
            int yes = (await message.GetReactionsAsync(DiscordEmoji.FromName(shardClient, ":thumbsup:"))).Count;
            int no = (await message.GetReactionsAsync(DiscordEmoji.FromName(shardClient, ":thumbsdown:"))).Count;
            if (yes > no)
            {
                message = await message.ModifyAsync(
                    embed: createEndedCountdownEmbed(
                        title,
                        "This Vote Has Succeeded",
                        description,
                        DiscordColor.Green
                    )
                );
            }
            else
            {
                message = await message.ModifyAsync(
                    embed: createEndedCountdownEmbed(
                        title,
                        "This Vote Has Failed",
                        description,
                        DiscordColor.Red
                    )
                );
            }

            RecoveryStorageHandler.RemoveVoteMessage(storedVoteMessage);
            return (yes > no);
        }

        private static async Task<bool> handleActiveVoteAsync(DiscordMessage message, CountdownTimer countdownTimer, string voteType, string title, string description, ulong referencedUser, Func<Task<bool>> cancelCondition, StoredVoteMessage storedVoteMessage)
        {
            _ = countdownTimer.startCountDown();
            int secondsSinceCheck = 0;
            while (countdownTimer.getTotalSecondsRemaining() > 0)
            {
                if (countdownTimer.IsUpdateTime())
                {
                    message = await message.ModifyAsync(
                        embed: createActiveCountdownEmbed(
                            countdownTimer,
                            title,
                            description,
                            DiscordColor.Azure
                        ));
                }

                if (countdownTimer.getTotalSecondsRemaining() % 5 == 0 && countdownTimer.getTotalSecondsRemaining() > 4)
                {
                    storedVoteMessage.storedCountdownTimerSeconds = countdownTimer.getTotalSecondsRemaining();
                    RecoveryStorageHandler.UpdateVoteMessage(storedVoteMessage);
                }

                if (secondsSinceCheck > 5)
                {
                    secondsSinceCheck = 0;
                    if (await cancelCondition())
                    {
                        countdownTimer.endCountdown();
                        message = await message.ModifyAsync(
                            embed: createEndedCountdownEmbed(
                                title,
                                "This Vote Has Been Cancelled",
                                description,
                                DiscordColor.Black
                            )
                        );
                        RecoveryStorageHandler.RemoveVoteMessage(storedVoteMessage);
                        return false;
                    }
                }

                await Task.Delay(1000);
                secondsSinceCheck++;
            }
            return true;
        }








        [Obsolete("Use StartVote_V2")]
        public static async Task<bool> StartVote(
            DiscordChannel channelToVote,
            CountdownTimer countdownTimer,
            string voteType,
            string Title,
            string Description,
            ulong referencedUser,
            Func<Task<bool>> CancelCondition,
            int secondsBetweenCancelChecks
        )
        {


            DiscordMessage message = await channelToVote.SendMessageAsync(
                embed: createActiveCountdownEmbed(
                    countdownTimer,
                    Title,
                    Description,
                    DiscordColor.Azure
                )
            );

            StoredVoteMessage storedVoteMessage = new StoredVoteMessage()
            {
                serverID = channelToVote.Guild.Id,
                channelID = channelToVote.Id,
                messageID = message.Id,
                userID = referencedUser,
                voteType = voteType,
                storedCountdownTimerSeconds = countdownTimer.getTotalSecondsRemaining()
            };
            try
            {
                RecoveryStorageHandler.StoreVoteMessage(storedVoteMessage);
            }
            catch (Exception e)
            {
                Console.WriteLine("STORE RECOVERY FAIL:" + e.ToString());
            }
            DiscordClient client = Program.ShardedClient.GetShard(storedVoteMessage.serverID);
            await message.CreateReactionAsync(
                DiscordEmoji.FromName(
                   client,
                    ":thumbsup:"
                )
            );
            //await Task.Delay(250);
            await message.CreateReactionAsync(
                DiscordEmoji.FromName(
                    client,
                    ":thumbsdown:"
                )
            );
            return await StartVoteFromAlreadySentMessage(
                message,
                countdownTimer,
                Title,
                Description,
                referencedUser,
                CancelCondition,
                secondsBetweenCancelChecks,
                "voteType"
            );
        }


        [Obsolete("Use UpdateVoteAsync Instead")]
        public static async Task<bool> StartVoteFromAlreadySentMessage(
            DiscordMessage message,
            CountdownTimer countdownTimer,
            string Title,
            string Description,
            ulong referencedUser,
            Func<Task<bool>> CancelCondition,
            int secondsBetweenCancelChecks,
            string VoteType
        )
        {
            StoredVoteMessage storedVoteMessage = new StoredVoteMessage()
            {
                serverID = message.Channel.Guild.Id,
                channelID = message.ChannelId,
                messageID = message.Id,
                userID = referencedUser,
                voteType = VoteType,
                storedCountdownTimerSeconds = countdownTimer.getTotalSecondsRemaining()
            };
            _ = countdownTimer.startCountDown();
            int currentSecondsSinceCancelCheck = 0;
            while (countdownTimer.getTotalSecondsRemaining() > 0)
            {
                if (countdownTimer.IsUpdateTime())
                {
                    message = await message.ModifyAsync(
                        embed: createActiveCountdownEmbed(
                            countdownTimer,
                            Title,
                            Description,
                            DiscordColor.Azure
                        )
                    );
                }
                await Task.Delay(1000);

                if (countdownTimer.getTotalSecondsRemaining() % 5 == 0 && countdownTimer.getTotalSecondsRemaining() > 4)
                {
                    storedVoteMessage.storedCountdownTimerSeconds = countdownTimer.getTotalSecondsRemaining();
                    RecoveryStorageHandler.UpdateVoteMessage(storedVoteMessage);
                }

                currentSecondsSinceCancelCheck++;
                if (currentSecondsSinceCancelCheck >= secondsBetweenCancelChecks)
                {
                    bool cancel = await CancelCondition.Invoke();
                    if (cancel)
                    {
                        countdownTimer.endCountdown();
                        message = await message.ModifyAsync(
                            embed: createEndedCountdownEmbed(
                                Title,
                                "This Vote Has Been Cancelled",
                                Description,
                                DiscordColor.Black
                            )
                        );
                        RecoveryStorageHandler.RemoveVoteMessage(storedVoteMessage);
                        return false;
                    }
                }
            }

            DiscordClient client = Program.ShardedClient.GetShard(storedVoteMessage.serverID);
            int yesVote = message
                .GetReactionsAsync(
                    DiscordEmoji.FromName(
                        client,
                        ":thumbsup:"
                    )
                )
                .Result.Count;
            //await Task.Delay(250);
            int noVote = message
                .GetReactionsAsync(
                    DiscordEmoji.FromName(
                        client,
                        ":thumbsdown:"
                    )
                )
                .Result.Count;
            if (yesVote > noVote)
            {
                message = await message.ModifyAsync(
                    embed: createEndedCountdownEmbed(
                        Title,
                        "This Vote Has Succeeded",
                        Description,
                        DiscordColor.Green
                    )
                );
            }
            else
            {
                message = await message.ModifyAsync(
                    embed: createEndedCountdownEmbed(
                        Title,
                        "This Vote Has Failed",
                        Description,
                        DiscordColor.Red
                    )
                );
            }
            RecoveryStorageHandler.RemoveVoteMessage(storedVoteMessage);
            return yesVote > noVote;
        }

        private static DiscordEmbed createActiveCountdownEmbed(
            CountdownTimer countdownTimer,
            string Title,
            string Description,
            DiscordColor color
        )
        {
            return createEmbed(
                Title,
                $"This Vote Will Remain Active For {countdownTimer.toQuickTime()}",
                Description,
                color
            );
        }

        private static DiscordEmbed createEndedCountdownEmbed(
            string Title,
            string footer,
            string Description,
            DiscordColor color
        )
        {
            return createEmbed(Title, footer, Description, color);
        }

        private static DiscordEmbed createEmbed(
            string Title,
            string footerText,
            string Description,
            DiscordColor color
        )
        {
            return new DiscordEmbedBuilder
            {
                Title = Title,
                Footer = new DiscordEmbedBuilder.EmbedFooter() { Text = footerText },
                Description = Description,
                Color = color,
            }.Build();
        }
    }
}
