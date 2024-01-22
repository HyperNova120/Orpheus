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
using Orpheus.Database;
using Orpheus.JailHandling;
using Orpheus.registerCommands;

namespace Orpheus.Audio_System
{
    public static class AudioCommands
    {
        public static async Task JoinVoiceChannel(CommandContext ctx)
        {
            DiscordChannel ChannelToJoin = null;
            if (ctx.Member.VoiceState != null && ctx.Member.VoiceState.Channel != null)
            {
                ChannelToJoin = ctx.Member.VoiceState.Channel;
            }
            else if (ctx.Channel.Type == DSharpPlus.ChannelType.Voice)
            {
                ChannelToJoin = ctx.Channel;
            }
            //check if in channel and need to leave
            if (
                Program.GetVoiceNextExtension().GetConnection(ctx.Guild) != null
                && Program.GetVoiceNextExtension().GetConnection(ctx.Guild).TargetChannel.Id
                    != ChannelToJoin.Id
            )
            {
                await LeaveVoiceChannel(ctx);
            }
            //connect to channel user is in if applicable, else connect to channel msg was sent in if applicable
            if (ctx.Member.VoiceState != null && ctx.Member.VoiceState.Channel != null)
            {
                await ChannelToJoin.ConnectAsync();
            }
            else if (ctx.Channel.Type == DSharpPlus.ChannelType.Voice)
            {
                await ChannelToJoin.ConnectAsync();
            }
            Console.WriteLine("CONNECT TO CHANNEL:" + ctx.Channel.Name);
            await Task.Delay(250);
            await ctx.Message.DeleteAsync();
        }

        public static async Task LeaveVoiceChannel(CommandContext ctx)
        {
            Program.GetVoiceNextExtension().GetConnection(ctx.Guild).Disconnect();
            Console.WriteLine("LEAVE CHANNEL:" + ctx.Channel.Name);
            await Task.Delay(250);
            await ctx.Message.DeleteAsync();
        }
    }
}
