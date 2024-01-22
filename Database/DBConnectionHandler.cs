using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
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
        private static List<ConnectionInfo> CachedConnections = new List<ConnectionInfo>();

        private static int connectionLimit = 5;

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
            foreach (ConnectionInfo conn in CachedConnections)
            {
                conn.closeMe();
            }
            CachedConnections.Clear();
            for (int i = 0; i < connectionLimit; i++)
            {
                CachedConnections.Add(new ConnectionInfo(connectionString));
            }
        }

        public static async Task<ConnectionInfo> GetConnection()
        {
            while (true)
            {
                for (int i = 0; i < connectionLimit; i++)
                {
                    lock (CachedConnections[i])
                    {
                        if (!CachedConnections[i].isInUse)
                        {
                            //is free
                            CachedConnections[i].isInUse = true;
                            return CachedConnections[i];
                        }
                    }
                }
                await Task.Delay(200);
            }
        }

        public class ConnectionInfo
        {
            public NpgsqlConnection npgsqlConnection { get; private set; }
            public bool isInUse { get; set; }

            public ConnectionInfo(string connectionString)
            {
                npgsqlConnection = new NpgsqlConnection(connectionString);
                isInUse = false;
                npgsqlConnection.Open();
            }

            public void closeMe()
            {
                npgsqlConnection.Close();
            }
        }
    }
}
