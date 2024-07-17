using System;
using System.Threading;
using System.Threading.Tasks;
using DSharpPlus;
using DSharpPlus.EventArgs;
using DSharpPlus.SlashCommands;
using Lavalink4NET.Extensions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;


public static class LLHandler
{
    public static void setup()
    {

    }

    private static void ConfigureServices(IServiceCollection services)
    {
        services.ConfigureLavalink(config =>
        {
            config.BaseAddress = new Uri("http://localhost:2333");
            config.WebSocketUri = new Uri("ws://localhost:2333/v4/websocket");
            config.ReadyTimeout = TimeSpan.FromSeconds(10);
            config.ResumptionOptions = new Lavalink4NET.LavalinkSessionResumptionOptions(TimeSpan.FromSeconds(60));
            config.Label = "LL Node";
            config.Passphrase = "youshallnotpass";
            config.HttpClientName = "LavalinkHttpClient";
        });
    }

    
}