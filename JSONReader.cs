using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using DSharpPlus;
using DSharpPlus.Entities;
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

        public async Task ReadJson()
        {
            using (StreamReader sr = new StreamReader("config\\config.json"))
            {
                string json = await sr.ReadToEndAsync();
                JSONStructure data = JsonConvert.DeserializeObject<JSONStructure>(json);
                this.token = data.token;
                this.prefix = data.prefix;
                lavalinkConfig = data.lavalinkConfig;
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
        public required LavalinkConfig lavalinkConfig { get; set; }
    }

    public class LavalinkConfig
    {
        public int port { get; set; }
        public string hostName { get; set; }
        public string password { get; set; }
    }
}
