using System.Security.Cryptography;
using Lavalink4NET.Protocol.Payloads;
using Npgsql;

namespace Orpheus.Database
{
    public static class DBConnectionHandler
    {
        private static string connectionString = "";
        private static List<ConnectionInfo> ConnectionPool = new List<ConnectionInfo>();

        private static int connectionLimit = 5; // max open connections

        public static void SetConnectionStrings(
            string host,
            string database,
            string username,
            string password
        )
        {
            connectionString =
                $"Host={host};Username={username};Password={password};Database={database}";
            setupConnections();
        }

        private static void setupConnections()
        {
            foreach (ConnectionInfo conn in ConnectionPool)
            {
                conn.closeMe();
            }
            ConnectionPool.Clear();
            for (int i = 0; i < connectionLimit; i++)
            {
                ConnectionPool.Add(new ConnectionInfo(connectionString));
            }
        }

        public static async Task<ConnectionInfo> GetConnection()
        {
            while (true)
            {
                int index = -1;
                for (int i = 0; i < connectionLimit; i++)
                {
                    lock (ConnectionPool[i])
                    {
                        if (!ConnectionPool[i].isInUse)
                        {
                            ConnectionPool[i].isInUse = true;
                            index = i;
                            break;
                        }
                    }
                }
                if (index != -1)
                {
                    ConnectionPool[index].openMe();
                    Console.WriteLine($"Returning Connection {index}");
                    return ConnectionPool[index];
                }
                else{
                    Console.WriteLine($"Connection Pool Full, Waiting");
                }
                await Task.Delay(5);
            }
        }

        public class ConnectionInfo
        {
            public NpgsqlConnection npgsqlConnection { get; private set; }
            public bool isInUse { get; set; }

            public DateTime timelastUsed { get; set; }

            public ConnectionInfo(string connectionString)
            {
                npgsqlConnection = new NpgsqlConnection(connectionString);
                isInUse = false;
            }

            public bool closeMe()
            {
                try
                {
                    isInUse = false;
                    npgsqlConnection.Close();
                    return true;
                }
                catch
                {
                    Console.WriteLine("Unable To Close Database Connection");
                    return false;
                }
            }

            public bool openMe()
            {
                try
                {
                    npgsqlConnection.Open();
                    return true;
                }
                catch
                {
                    Console.WriteLine("Unable To Open Database Connection");
                    return false;
                }
            }
        }
    }
}
