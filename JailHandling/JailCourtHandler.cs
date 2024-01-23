using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Security.Cryptography;
using System.Threading.Tasks;
using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;
using DSharpPlus.Entities;
using DSharpPlus.EventArgs;
using DSharpPlus.VoiceNext;
using NpgsqlTypes;
using Orpheus.ApiStuff;
using Orpheus.commands;
using Orpheus.Database;
using Orpheus.Voting;

namespace Orpheus.JailHandling
{
    public static class JailCourtHandler
    {
        public static async Task HandleJailCourtMessage(MessageCreateEventArgs args)
        {
            CountdownTimer countdownTimer = new CountdownTimer(0, 0, 30);
            DiscordMember jailedUser = await OrpheusAPIHandler.GetMemberAsync(
                args.Guild,
                args.Author.Id
            );
            _ = startCourtVote(args.Channel, jailedUser, countdownTimer);
        }

        public static async Task RestartJailCourtMessage(StoredVoteMessage storedVoteMessage)
        {
            DiscordGuild server = await Program.Client.GetGuildAsync(storedVoteMessage.serverID);
            DiscordChannel channel = server.GetChannel(storedVoteMessage.channelID);
            DiscordMember jailedUser = await server.GetMemberAsync(storedVoteMessage.userID);
            DiscordMessage discordmessage = await channel.GetMessageAsync(
                storedVoteMessage.messageID
            );
            DiscordEmbed discordEmbed = discordmessage.Embeds[0];
            DiscordEmbedFooter footer = discordEmbed.Footer;
            string text = footer.Text;
            if (text.Equals("This Vote Has Been Cancelled"))
            {
                //vote already ended
                TempStorageHandler.RemoveVoteMessage(storedVoteMessage);
                return;
            }
            else
            {
                text = text.Split("For")[1];
            }
            string[] valueAmount = text.Trim().Split(" ");
            int sec = 0;
            int min = 0;
            int hr = 0;
            if (valueAmount[1].Equals("Seconds"))
            {
                    sec = int.Parse(valueAmount[0]);
            }
            else if (valueAmount[1].Equals("Minutes"))
            {
                    min = int.Parse(valueAmount[0]);
            }
            else if (valueAmount[1].Equals("Hours"))
            {
                    hr = int.Parse(valueAmount[0]);
            }
            CountdownTimer countdownTimer = new CountdownTimer(hr, min, sec);
            Console.WriteLine($"RESTART COURT MESSAGE REMAINING TIME {text}:{valueAmount[0]}:{hr} HOURS {min} MINUTES {sec} SECONDS");
            _ = startCourtVote(discordmessage, jailedUser, countdownTimer);
        }

        private static async Task startCourtVote(
            DiscordMessage message,
            DiscordMember jailedUser,
            CountdownTimer countdownTimer
        )
        {
            ulong jailRoleID = await OrpheusDatabaseHandler.GetJailIDInfo(
                message.Channel.Guild.Id,
                "jailroleid"
            );
            DiscordRole jailRole = await OrpheusAPIHandler.GetRoleAsync(message.Channel.Guild, jailRoleID);
            bool didVoteSucceed = await HandleVote.StartVoteFromAlreadySentMessage(
                message,
                countdownTimer,
                $"Vote To Free {jailedUser.DisplayName} From Jail",
                $"vote using the provided reactions to decide if {jailedUser.DisplayName} should be released from jail",
                jailedUser.Id,
                async () =>
                {
                    return await checkIfVoteNeedsCancel(jailRole, jailedUser);
                },
                5
            );

            if (didVoteSucceed)
            {
                await jailedUser.RevokeRoleAsync(jailRole);
                try
                {
                    DiscordRole courtrole = await OrpheusAPIHandler.GetRoleAsync(
                        message.Channel.Guild,
                        await OrpheusDatabaseHandler.GetJailIDInfo(
                            message.Channel.Guild.Id,
                            "jailcourtroleid"
                        )
                    );
                    await jailedUser.RevokeRoleAsync(courtrole);
                }
                catch
                {
                    Console.WriteLine("FREE ERROR, JAIL COURT ROLE DOES NOT EXIST");
                }
            }

            StoredVoteMessage storedVoteMessage = new StoredVoteMessage()
            {
                messageID = message.Id
            };
            TempStorageHandler.RemoveVoteMessage(storedVoteMessage);
        }

        private static async Task startCourtVote(
            DiscordChannel channel,
            DiscordMember jailedUser,
            CountdownTimer countdownTimer
        )
        {
            ulong jailRoleID = await OrpheusDatabaseHandler.GetJailIDInfo(
                channel.Guild.Id,
                "jailroleid"
            );
            DiscordRole jailRole = await OrpheusAPIHandler.GetRoleAsync(channel.Guild, jailRoleID);
            bool didVoteSucceed = await HandleVote.StartVote(
                channel,
                countdownTimer,
                "CourtVote",
                $"Vote To Free {jailedUser.DisplayName} From Jail",
                $"vote using the provided reactions to decide if {jailedUser.DisplayName} should be released from jail",
                jailedUser.Id,
                async () =>
                {
                    return await checkIfVoteNeedsCancel(jailRole, jailedUser);
                },
                5
            );

            if (didVoteSucceed)
            {
                await jailedUser.RevokeRoleAsync(jailRole);
                try
                {
                    DiscordRole courtrole = await OrpheusAPIHandler.GetRoleAsync(
                        channel.Guild,
                        await OrpheusDatabaseHandler.GetJailIDInfo(
                            channel.Guild.Id,
                            "jailcourtroleid"
                        )
                    );
                    await jailedUser.RevokeRoleAsync(courtrole);
                }
                catch
                {
                    Console.WriteLine("FREE ERROR, JAIL COURT ROLE DOES NOT EXIST");
                }
            }
        }

        private static async Task<bool> checkIfVoteNeedsCancel(
            DiscordRole checkIfHasRole,
            DiscordMember discordMember
        )
        {
            bool returner = true;
            discordMember = await OrpheusAPIHandler.GetMemberAsync(
                discordMember.Guild,
                discordMember.Id
            );
            foreach (DiscordRole role in discordMember.Roles)
            {
                if (role.Id == checkIfHasRole.Id)
                {
                    returner = false;
                }
            }
            return returner;
        }

        private static async Task<DiscordMessage> UpdateVoteTime(
            DiscordMessage message,
            string remainingTime
        )
        {
            DiscordEmbedBuilder messageBuilder = new DiscordEmbedBuilder
            {
                Title = "Vote To Free User From Jail",
                Footer = new DiscordEmbedBuilder.EmbedFooter() { Text = "hi" },
                Description = $"This vote will remain active for {remainingTime}",
                Color = DiscordColor.Azure,
            };

            return await message.ModifyAsync(embed: messageBuilder.Build());
        }

        private static async Task SendUserManuallyFreed(DiscordMessage message)
        {
            DiscordEmbedBuilder messageBuilder = new DiscordEmbedBuilder
            {
                Title = "Vote To Free User From Jail",
                Description = $"User has been manually freed",
                Color = DiscordColor.Green,
            };

            message = await message.ModifyAsync(embed: messageBuilder.Build());
        }
    }
}
