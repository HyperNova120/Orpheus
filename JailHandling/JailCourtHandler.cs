using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;
using DSharpPlus.Entities;
using DSharpPlus.EventArgs;
using DSharpPlus.VoiceNext;
using NpgsqlTypes;
using Orpheus.commands;
using Orpheus.Database;
using Orpheus.Voting;

namespace Orpheus.JailHandling
{
    public static class JailCourtHandler
    {
        public static async Task HandleJailCourtMessage(MessageCreateEventArgs args)
        {
            CountdownTimer countdownTimer = new CountdownTimer(6, 0, 0);
            DiscordMember jailedUser = await args.Guild.GetMemberAsync(args.Author.Id);

            ulong jailRoleID = await OrpheusDatabaseHandler.GetJailIDInfo(
                args.Guild.Id,
                "jailroleid"
            );
            DiscordRole jailRole = args.Guild.GetRole(jailRoleID);

            bool didVoteSucceed = await HandleVote.StartVote(
                args.Channel,
                countdownTimer,
                $"Vote To Free {jailedUser.DisplayName} From Jail",
                $"vote using the provided reactions to decide if {jailedUser.DisplayName} should be released from jail",
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
                    DiscordRole courtrole = args.Guild.GetRole(
                        await OrpheusDatabaseHandler.GetJailIDInfo(args.Guild.Id, "jailcourtroleid")
                    );
                    await jailedUser.RevokeRoleAsync(courtrole);
                }
                catch (Exception e)
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
            discordMember = await discordMember.Guild.GetMemberAsync(discordMember.Id);
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
