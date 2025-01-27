using System;
using System.Threading.Tasks;
using DSharpPlus.Entities;
using Lavalink4NET;
using Microsoft.Extensions.Options;
using System.ComponentModel;
using DSharpPlus.CommandsNext.Attributes;

public class MusicCommands
{
    private static IAudioService _audioService = null;

    MusicCommands(IAudioService _audioService)
    {
        MusicCommands.SetUp(_audioService);
    }

    public static void SetUp(IAudioService audioService)
    {
        ArgumentNullException.ThrowIfNull(audioService);
        _audioService = audioService;
        Console.WriteLine("SUCCESSFULLY REGISTERED AUDIO SERVICE!!!");
    }
}