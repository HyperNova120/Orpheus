using System;
using System.Diagnostics;
using System.Reflection.Metadata.Ecma335;
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
    private static Process lavalinkProcess = null;
    
    public static async Task Setup()
    {
        Console.WriteLine(@$" -jar {AppContext.BaseDirectory}LavaLink4Net{Path.DirectorySeparatorChar}Lavalink.jar");
        lavalinkProcess = new Process();
        lavalinkProcess.StartInfo.FileName = "java";
        lavalinkProcess.StartInfo.UseShellExecute = false;
        lavalinkProcess.StartInfo.Arguments = $" -jar {AppContext.BaseDirectory}LavaLink4Net{Path.DirectorySeparatorChar}Lavalink.jar";
        lavalinkProcess.StartInfo.CreateNoWindow = true;
        lavalinkProcess.StartInfo.ErrorDialog = false;
        lavalinkProcess.Start();

        int waitSec = 5;

        for (int i = 0; i < waitSec; i++)
        {
            Console.WriteLine("STARTING IN " + (waitSec - i));
            await Task.Delay(1000);
        }
        await Task.Delay(0);
    }

    public static async Task Close()
    {
        lavalinkProcess.Close();
        //lavalinkProcess.CloseMainWindow();
        lavalinkProcess.Dispose();
        Console.WriteLine("lavalinkProcess CLOSE");
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