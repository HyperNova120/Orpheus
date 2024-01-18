using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Security.Claims;
using System.Threading.Tasks;
using Npgsql;
using Npgsql.Replication;

namespace Orpheus.Database
{
    public static class DBEngine
    {
        private static string host = "";
        private static string database = "";
        private static string username = "";
        private static string password = "";
        private static string connectionString = "";

        //TODO: refactor DBEngine to include only general methods for future projects, move orpheus specific code to another class
        //TODO: Future DBEngine specification; layered approach, library as core, DBEngine as intermediate layer, applicaiton as user
        //TODO: layers only touch next lower layer, never more



        public static void SetConnectionStrings(
            string host,
            string database,
            string username,
            string password
        )
        {
            DBEngine.host = host;
            DBEngine.database = database;
            DBEngine.username = username;
            DBEngine.password = password;
            connectionString =
                $"Host={host};Username={username};Password={password};Database={database}";
        }

        public static async Task<bool> DoesEntryExist(
            string table,
            string columnName,
            string testForValue
        )
        {
            try
            {
                using (NpgsqlConnection conn = new NpgsqlConnection(connectionString))
                {
                    await conn.OpenAsync();
                    string query =
                        $"SELECT EXISTS(SELECT 1 FROM {table} WHERE {columnName}='{testForValue}');";
                    using (NpgsqlCommand cmd = new NpgsqlCommand(query, conn))
                    {
                        bool temp = Convert.ToBoolean(await cmd.ExecuteScalarAsync());
                        return temp;
                    }
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e.ToString());
                return false;
            }
        }

        public static async Task<bool> RunExecuteNonQueryAsync(string query)
        {
            try
            {
                using (NpgsqlConnection conn = new NpgsqlConnection(connectionString))
                {
                    await conn.OpenAsync();
                    using (NpgsqlCommand cmd = new NpgsqlCommand(query, conn))
                    {
                        await cmd.ExecuteNonQueryAsync();
                    }
                }
                return true;
            }
            catch (Exception e)
            {
                Console.WriteLine(e.ToString());
                return false;
            }
        }
    }
}
