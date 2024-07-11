using Npgsql;

namespace Orpheus.Database
{
    public static class DBConnectionHandler
    {
        private static string host = "";
        private static string database = "";
        private static string username = "";
        private static string password = "";
        private static string connectionString = "";
        private static List<ConnectionInfo> ConnectionPool = new List<ConnectionInfo>();

        private static int connectionLimit = 100; // max open connections
        private static int unusedConnectionTimeLimit = 60; //seconds

        public static void SetConnectionStrings(
            string host,
            string database,
            string username,
            string password
        )
        {
            DBConnectionHandler.host = host;
            DBConnectionHandler.database = database;
            DBConnectionHandler.username = username;
            DBConnectionHandler.password = password;
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

        public static async Task HandleConnections()
        {
            while (true)
            {
                foreach (ConnectionInfo conn in ConnectionPool)
                {
                    lock (conn)
                    {
                        if (conn.isInUse)
                        {
                            continue;
                        }
                        if (
                            (DateTime.Now - conn.timelastUsed).TotalSeconds
                                >= unusedConnectionTimeLimit
                            && conn.npgsqlConnection.State == System.Data.ConnectionState.Open
                        )
                        {
                            Console.WriteLine("CLOSING CONNECTION");
                            conn.closeMe();
                        }
                    }
                }
                await Task.Delay(1000);
            }
        }

        public static async Task<ConnectionInfo> GetConnection()
        {
            while (true)
            {
                for (int i = 0; i < connectionLimit; i++)
                {
                    lock (ConnectionPool[i])
                    {
                        if (!ConnectionPool[i].isInUse)
                        {
                            //is free
                            if (
                                ConnectionPool[i].npgsqlConnection.State
                                == System.Data.ConnectionState.Closed
                            )
                            {
                                Console.WriteLine("OPEN CONNECTION: " + i);
                                
                                if (!ConnectionPool[i].openMe())
                                {
                                    continue;
                                }
                            }
                            ConnectionPool[i].isInUse = true;
                            Console.WriteLine("RETURN CONNECTION: " + i);
                            ConnectionPool[i].timelastUsed = DateTime.Now;
                            return ConnectionPool[i];
                        }
                    }
                }
                Console.WriteLine("DATABASE CONNECTION POOL FULL, WAITING");
                await Task.Delay(10);
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
