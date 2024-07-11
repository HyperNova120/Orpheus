using DSharpPlus.Entities;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Orpheus.Database;

namespace Orpheus
{
    public class JSONReader
    {
        public string token { get; set; }
        public string prefix { get; set; }
        public DiscordUser registeredAdmin { get; private set; }
        public LavalinkConfig lavalinkConfig { get; private set; }

        public static int courtVoteTimeHours {get; private set;}
        public static int courtVoteTimeMinutes {get; private set;}
        public static int courtVoteTimeSeconds {get; private set;}

        public async Task ReadJson()
        {
            Console.WriteLine($"CONFIG FOLDER LOCATION: {AppContext.BaseDirectory}/config/config.json");
            using (StreamReader sr = new StreamReader($"{AppContext.BaseDirectory}/config/config.json"))
            {
                string json = await sr.ReadToEndAsync();
                JSONStructure? data = JsonConvert.DeserializeObject<JSONStructure>(json);


                this.token = data.token;
                this.prefix = data.prefix;
                lavalinkConfig = data.lavalinkConfig;
                courtVoteTimeHours = data.courtVoteTimeHours;
                courtVoteTimeMinutes = data.courtVoteTimeMinutes;
                courtVoteTimeSeconds = data.courtVoteTimeSeconds;

                DBEngine.SetConnectionStrings(
                    data.host,
                    data.database,
                    data.username,
                    data.password
                );
            }
        }

        public void testWrite()
        {
            JSONStructure temp = new JSONStructure()
            {
                database = "",
                host = "",
                lavalinkConfig = new LavalinkConfig()
                {
                    hostName = "lavahost",
                    password = "lavapassword",
                    port = 255
                },
                password = "",
                prefix = "",
                token = "",
                username = "",
                courtVoteTimeHours = 3,
                courtVoteTimeMinutes = 0,
                courtVoteTimeSeconds = 0,
            };
            string json = JsonConvert.SerializeObject(temp);
            File.WriteAllText("temp.json", json);
        }
    }

    internal sealed class JSONStructure
    {
        public required string token { get; set; }
        public required string prefix { get; set; }
        public required string host { get; set; }
        public required string database { get; set; }
        public required string username { get; set; }
        public required string password { get; set; }
        public required int courtVoteTimeHours { get; set; }
        public required int courtVoteTimeMinutes { get; set; }
        public required int courtVoteTimeSeconds { get; set; }
        public required LavalinkConfig lavalinkConfig { get; set; }
    }

    public class LavalinkConfig
    {
        public int port { get; set; }
        public string hostName { get; set; }
        public string password { get; set; }
    }
}
