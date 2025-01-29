using DSharpPlus.Entities;
using DSharpPlus.EventArgs;
using Lavalink4NET.Filters;
using Newtonsoft.Json;
using Orpheus.ApiStuff;
using Orpheus.Database;
using Orpheus.Voting;

namespace Orpheus.JailHandling
{
    public static class JailCourtHandler
    {
        public static async Task HandleJailCourtMessage(MessageCreatedEventArgs args)
        {
            CountdownTimer countdownTimer = new CountdownTimer(ConfigReader.courtVoteTimeHours, ConfigReader.courtVoteTimeMinutes, ConfigReader.courtVoteTimeSeconds);
            DiscordMember jailedUser = await OrpheusAPIHandler.GetMemberAsync(
                args.Guild,
                args.Author.Id
            );
            _ = startCourtVote(args.Channel, jailedUser, countdownTimer);
        }

        public static async Task RestartJailCourtMessage(StoredVoteMessage storedVoteMessage)
        {
            DiscordGuild server = await Program.OrpheusClient.GetGuildAsync(storedVoteMessage.serverID);
            DiscordChannel channel = await server.GetChannelAsync(storedVoteMessage.channelID);
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
                RecoveryStorageHandler.RemoveVoteMessage(storedVoteMessage);
                return;
            }

            CountdownTimer countdownTimer = new CountdownTimer(storedVoteMessage.storedCountdownTimerSeconds);
            _ = ContinueCourtVote(discordmessage, jailedUser, countdownTimer);
        }

        private static async Task ContinueCourtVote(
            DiscordMessage message,
            DiscordMember jailedUser,
            CountdownTimer countdownTimer
        )
        {
            DBEngine.Serverproperties serverproperties = DBEngine.getServerProperties(message.Channel.Guild.Id);
            ulong jailRoleID = serverproperties.JailRoleID;
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
                await handleCourtVoteSuccess(jailedUser, jailRole, message.Channel);
            }
        }

        private static async Task startCourtVote(
            DiscordChannel channel,
            DiscordMember jailedUser,
            CountdownTimer countdownTimer
        )
        {
            DBEngine.Serverproperties serverproperties = DBEngine.getServerProperties(channel.Guild.Id);
            ulong jailRoleID = serverproperties.JailRoleID;
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
                await handleCourtVoteSuccess(jailedUser, jailRole, channel);
            }
        }

        private static async Task handleCourtVoteSuccess(DiscordMember jailedUser, DiscordRole jailRole, DiscordChannel channel)
        {
            await jailedUser.RevokeRoleAsync(jailRole);
            try
            {

                DBEngine.Serverproperties serverproperties = DBEngine.getServerProperties(channel.Guild.Id);
                DiscordRole courtrole = await OrpheusAPIHandler.GetRoleAsync(
                    channel.Guild,
                    serverproperties.JailCourtRoleID
                );
                await jailedUser.RevokeRoleAsync(courtrole);
            }
            catch
            {
                Console.WriteLine("FREE ERROR, JAIL COURT ROLE DOES NOT EXIST");
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
    }
}
