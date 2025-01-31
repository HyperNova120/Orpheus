using System.Diagnostics;


public static class LLHandler
{
    private static Process lavalinkProcess = null;

    public static async Task Setup()
    {
        Console.WriteLine(@$" -jar {AppContext.BaseDirectory}LavaLink4Net{Path.DirectorySeparatorChar}Lavalink.jar");
        lavalinkProcess = new Process();
        lavalinkProcess.StartInfo.WorkingDirectory = $"{AppContext.BaseDirectory}LavaLink4Net";
        lavalinkProcess.StartInfo.FileName = "java";
        lavalinkProcess.StartInfo.UseShellExecute = true;
        lavalinkProcess.StartInfo.Arguments = $" -jar {AppContext.BaseDirectory}LavaLink4Net{Path.DirectorySeparatorChar}Lavalink.jar";
        lavalinkProcess.StartInfo.CreateNoWindow = true;
        lavalinkProcess.StartInfo.ErrorDialog = true;
        lavalinkProcess.Start();

        int waitSec = 5;

        for (int i = 0; i < waitSec; i++)
        {
            Utils.PrintToConsoleWithColor("STARTING IN " + (waitSec - i), ConsoleColor.DarkYellow);
            await Task.Delay(1000);
        }
        await Task.Delay(0);



    }

    public static async Task Close()
    {
        try
        {
            await MusicModule.Stop();
            lavalinkProcess.Close();
            //lavalinkProcess.Kill();
            Console.WriteLine("lavalinkProcess CLOSE");
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
        }
    }


}