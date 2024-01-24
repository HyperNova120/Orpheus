using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Threading;
using System.Threading.Tasks;
using DSharpPlus.Entities;
using DSharpPlus.Interactivity.Extensions;
using Orpheus.JailHandling;

namespace Orpheus.Voting
{
    public static class HandleVote
    {
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
                voteType = voteType
            };
            try
            {
                RecoveryStorageHandler.StoreVoteMessage(storedVoteMessage);
            }
            catch (Exception e)
            {
                Console.WriteLine("STORE RECOVERY FAIL:" + e.ToString());
            }
            await message.CreateReactionAsync(
                DiscordEmoji.FromName(
                    Program.Client.GetShard(storedVoteMessage.serverID),
                    ":thumbsup:"
                )
            );
            //await Task.Delay(250);
            await message.CreateReactionAsync(
                DiscordEmoji.FromName(
                    Program.Client.GetShard(storedVoteMessage.serverID),
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
                secondsBetweenCancelChecks
            );
        }

        public static async Task<bool> StartVoteFromAlreadySentMessage(
            DiscordMessage message,
            CountdownTimer countdownTimer,
            string Title,
            string Description,
            ulong referencedUser,
            Func<Task<bool>> CancelCondition,
            int secondsBetweenCancelChecks
        )
        {
            StoredVoteMessage storedVoteMessage = new StoredVoteMessage()
            {
                serverID = message.Channel.Guild.Id,
                channelID = message.ChannelId,
                messageID = message.Id,
                userID = referencedUser
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

            int yesVote = message
                .GetReactionsAsync(
                    DiscordEmoji.FromName(
                        Program.Client.GetShard(storedVoteMessage.serverID),
                        ":thumbsup:"
                    )
                )
                .Result.Count;
            await Task.Delay(250);
            int noVote = message
                .GetReactionsAsync(
                    DiscordEmoji.FromName(
                        Program.Client.GetShard(storedVoteMessage.serverID),
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
