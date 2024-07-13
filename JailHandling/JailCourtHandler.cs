using DSharpPlus.Entities;
using DSharpPlus.EventArgs;
using Newtonsoft.Json;
using Orpheus.ApiStuff;
using Orpheus.Database;
using Orpheus.Voting;

namespace Orpheus.JailHandling
{
    public static class JailCourtHandler
    {
        public static async Task HandleJailCourtMessage(MessageCreateEventArgs args)
        {
            CountdownTimer countdownTimer = new CountdownTimer(JSONReader.courtVoteTimeHours, JSONReader.courtVoteTimeMinutes, JSONReader.courtVoteTimeSeconds);
            DiscordMember jailedUser = await OrpheusAPIHandler.GetMemberAsync(
                args.Guild,
                args.Author.Id
            );
            _ = startCourtVote(args.Channel, jailedUser, countdownTimer);
        }

        public static async Task RestartJailCourtMessage(StoredVoteMessage storedVoteMessage)
        {
            DiscordGuild server = await Program.ShardedClient.GetShard(storedVoteMessage.serverID).GetGuildAsync(storedVoteMessage.serverID);
            DiscordChannel channel = server.GetChannel(storedVoteMessage.channelID);
            DiscordMember jailedUser = await server.GetMemberAsync(storedVoteMessage.userID);
            DiscordMessage discordmessage = await channel.GetMessageAsync(
                storedVoteMessage.messageID
            );
            DiscordEmbed discordEmbed = discordmessage.Embeds[0];
            DiscordEmbedFooter footer = discordEmbed.Footer;
            Console.WriteLine($"\tRECIEVED DISCORD FOOTER");
            string text = footer.Text;
            Console.WriteLine($"\tRECIEVED DISCORD FOOTER TEXT");
            if (text.Equals("This Vote Has Been Cancelled"))
            {
                Console.WriteLine($"RESTART COURT FAILED, VOTE CANCELLED");
                //vote already ended
                RecoveryStorageHandler.RemoveVoteMessage(storedVoteMessage);
                return;
            }
            Console.WriteLine($"\tVOTE ACTIVE");

            CountdownTimer countdownTimer = new CountdownTimer(storedVoteMessage.storedCountdownTimerSeconds);
            Console.WriteLine($"RESTART COURT MESSAGE REMAINING TIME {countdownTimer.toQuickTime()}");
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
            bool didVoteSucceed = await HandleVote.UpdateVoteAsync(
                message,
                countdownTimer,
                "CourtVote",
                $"Vote To Free {jailedUser.DisplayName} From Jail",
                $"vote using the provided reactions to decide if {jailedUser.DisplayName} should be released from jail",
                jailedUser.Id,
                async () =>
                {
                    return await checkIfVoteNeedsCancel(jailRole, jailedUser);
                }
            );

            if (didVoteSucceed)
            {
                Console.WriteLine("FREE USER");
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
            RecoveryStorageHandler.RemoveVoteMessage(storedVoteMessage);
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
            bool didVoteSucceed = await HandleVote.StartVote_V2(
                channel,
                countdownTimer,
                "CourtVote",
                $"Vote To Free {jailedUser.DisplayName} From Jail",
                $"vote using the provided reactions to decide if {jailedUser.DisplayName} should be released from jail",
                jailedUser.Id,
                async () =>
                {
                    return await checkIfVoteNeedsCancel(jailRole, jailedUser);
                }
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
