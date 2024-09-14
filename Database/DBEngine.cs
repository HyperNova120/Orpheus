using Npgsql;
namespace Orpheus.Database;
using Newtonsoft.Json;

public static class DBEngine
{
    static string dataFolderPath = AppContext.BaseDirectory + "Data" + Path.DirectorySeparatorChar;
    /*
    Server Folder
        Messages
            Channels
        Attachments
        Gifs
        Admins
        ServerProperties
    */

    public static void Init(ulong serverID)
    {
        if (!Directory.Exists(dataFolderPath + serverID))
        {
            createServerDirectory(serverID);
        }
    }

    private static void createServerDirectory(ulong serverID)
    {
        string serverDataPath = dataFolderPath + serverID + Path.DirectorySeparatorChar;

        //messages
        Directory.CreateDirectory(serverDataPath + "Messages");

        //Admins
        File.Create(serverDataPath + "Admins.txt");
        //Attachments
        File.Create(serverDataPath + "Attachments.txt");
        //Gifs
        File.Create(serverDataPath + "Gifs.txt");
        //Server Properties
        File.Create(serverDataPath + "ServerProperties.json");
    }

    public static void saveMessage(ulong serverID, ulong channelID, ulong userID, string content)
    {
        string DataPath = dataFolderPath + serverID + Path.DirectorySeparatorChar + "Messages" + Path.DirectorySeparatorChar + channelID.ToString() + ".txt";
        appendToFile(DataPath, $"({userID}):" + content);
        //File.AppendAllLines(DataPath, new string[] { $"({userID}):" + content });
    }

    public static void SaveAdmin(ulong serverID, ulong userID)
    {
        if (DoesAdminExist(serverID, userID))
        {
            Console.WriteLine("User Is Already Admin");
            return;
        }
        string DataPath = dataFolderPath + serverID + Path.DirectorySeparatorChar + "Admins.txt";
        appendToFile(DataPath, userID.ToString());
    }

    public static void SaveGif(ulong serverID, string gif)
    {
        string DataPath = dataFolderPath + serverID + Path.DirectorySeparatorChar + "Gifs.txt";

        HashSet<string> uniqueGifs = new HashSet<string>();
        StreamReader sr = new StreamReader(DataPath);
        string lines = sr.ReadToEnd();
        Console.WriteLine("GIF STORAGE: " + lines);
        foreach (string s in lines.Split("\n"))
        {
            if (s.Length == 0)
            {
                continue;
            }
            uniqueGifs.Add(s);
        }
        sr.Dispose();
        uniqueGifs.Add(gif);
        truncateFile(DataPath);
        appendToFile(DataPath, uniqueGifs.ToArray());
    }

    public static string[] GetGifs(ulong serverID)
    {
        string DataPath = dataFolderPath + serverID + Path.DirectorySeparatorChar + "Gifs.txt";
        StreamReader sr = new StreamReader(DataPath);
        string[] returner = sr.ReadToEnd().Split("\n");
        sr.Dispose();
        return returner;
    }

    public static void SaveAttachment(ulong serverID, string attachment)
    {
        string DataPath = dataFolderPath + serverID + Path.DirectorySeparatorChar + "Attachments.txt";
        appendToFile(DataPath, attachment);
    }

    public static bool DoesAdminExist(ulong serverID, ulong userId)
    {
        Console.WriteLine("DoesAdminExist: 1");
        string DataPath = dataFolderPath + serverID + Path.DirectorySeparatorChar + "Admins.txt";
        Console.WriteLine("DoesAdminExist: 2");
        StreamReader sr = null;
        try
        {
            sr = new StreamReader(DataPath);
        }
        catch (Exception ex)
        {
            Console.WriteLine("WHYYYYYYY:" + ex);
        }
        Console.WriteLine("DoesAdminExist: 3");
        foreach (string s in sr.ReadToEnd().Split("\n"))
        {
            if (s.Equals(userId.ToString()))
            {
                sr.Dispose();
                Console.WriteLine("DoesAdminExist: TRUE");
                return true;
            }
        }
        sr.Dispose();
        Console.WriteLine("DoesAdminExist: FALSE");
        return false;
    }

    public static void RemoveAdmin(ulong serverID, ulong userId)
    {
        string DataPath = dataFolderPath + serverID + Path.DirectorySeparatorChar + "Admins.txt";
        List<string> admins = new List<string>(File.ReadAllLines(DataPath));
        admins.Remove(userId.ToString());
        truncateFile(DataPath);
        appendToFile(DataPath, admins.ToArray());
    }

    private static void appendToFile(String Path, string text)
    {
        if (!File.Exists(Path))
        {
            FileStream fs = File.Create(Path);
            fs.Dispose();
        }
        //File.AppendAllText(Path, text);
        StreamWriter sr = new StreamWriter(Path, true);
        sr.WriteLine(text);
        sr.Dispose();
    }
    private static void appendToFile(String Path, string[] text)
    {
        if (!File.Exists(Path))
        {
            File.Create(Path);
        }
        //File.AppendAllText(Path, text);
        StreamWriter sr = new StreamWriter(Path, true);
        foreach (string s in text)
        {
            sr.WriteLine(s);
        }
        sr.Dispose();
    }

    private static void truncateFile(string Path)
    {
        FileStream fs = new FileStream(Path, FileMode.Truncate);
        fs.SetLength(0);
        fs.Dispose();
    }

    public static Serverproperties getServerProperties(ulong serverID)
    {
        StreamReader sr = new StreamReader(dataFolderPath + serverID + Path.DirectorySeparatorChar + "ServerProperties.json");
        string json = sr.ReadToEnd();
        sr.Dispose();
        Serverproperties data = JsonConvert.DeserializeObject<Serverproperties>(json);
        return data;
    }

    public static bool doesServerPropertiesExist(ulong serverID)
    {
        return File.Exists(dataFolderPath + serverID + Path.DirectorySeparatorChar + "ServerProperties.json");
    }

    public static void setServerProperties(ulong serverID, Serverproperties serverproperties)
    {
        string DataPath = dataFolderPath + serverID + Path.DirectorySeparatorChar + "ServerProperties.json";
        truncateFile(DataPath);
        Console.WriteLine("Update Server Properties:" + JsonConvert.SerializeObject(serverproperties));
        appendToFile(DataPath, JsonConvert.SerializeObject(serverproperties));
    }



    public struct Serverproperties
    {
        public ulong ServerID;
        public ulong JailRoleID;
        public ulong JailCourtRoleID;
        public ulong JailCourtChannelID;
    }
}

