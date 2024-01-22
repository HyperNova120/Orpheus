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
            //connect to channel user is in if applicable, else connect to channel msg was sent in if applicable
            if (ctx.Member.VoiceState != null && ctx.Member.VoiceState.Channel != null)
            {
                await ctx.Member.VoiceState.Channel.ConnectAsync();
            }
            else if (ctx.Channel.Type == DSharpPlus.ChannelType.Voice)
            {
                await ctx.Channel.ConnectAsync();
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
