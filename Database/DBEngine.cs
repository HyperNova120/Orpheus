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


        //TODO use this to get all admins of a server "SELECT username FROM orpheusdata.admininfo INNER JOIN orpheusdata.userinfo ON orpheusdata.admininfo.userid=orpheusdata.userinfo.userid WHERE orpheusdata.admininfo.serverid='{serverid}';"

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
            //Console.WriteLine("DoesEntryExist CALLED");
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

        public static async Task<bool> DoesEntryExist(
            string table,
            string columnName,
            string columnName2,
            string testForValue,
            string testForValue2
        )
        {
            //Console.WriteLine("DoesEntryExist CALLED");
            try
            {
                using (NpgsqlConnection conn = new NpgsqlConnection(connectionString))
                {
                    await conn.OpenAsync();
                    string query =
                        $"SELECT EXISTS(SELECT 1 FROM {table} WHERE {columnName}='{testForValue}' AND {columnName2}='{testForValue2}');";
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

        public static async Task<bool> RunExecuteNonQueryAsync(NpgsqlCommand cmd)
        {
            //Console.WriteLine("RunExecuteNonQueryAsync CALLED");
            NpgsqlConnection conn = new NpgsqlConnection(connectionString);
            await conn.OpenAsync();
            NpgsqlTransaction transaction = conn.BeginTransaction();
            try
            {
                cmd.Connection = conn;
                await cmd.PrepareAsync();
                await cmd.ExecuteNonQueryAsync();
                transaction.Commit();
                await conn.CloseAsync();
                return true;
            }
            catch (Exception e)
            {
                Console.WriteLine(e.ToString());
                transaction.Rollback();
                await conn.CloseAsync();
                return false;
            }
        }

        public static async Task<NpgsqlDataReader> RunExecuteReaderAsync(NpgsqlCommand cmd)
        {
            //Console.WriteLine("RunExecuteReaderAsync CALLED");
            NpgsqlConnection conn = new NpgsqlConnection(connectionString);
            await conn.OpenAsync();
            NpgsqlTransaction transaction = conn.BeginTransaction();
            try
            {
                cmd.Connection = conn;
                await cmd.PrepareAsync();
                NpgsqlDataReader reader = await cmd.ExecuteReaderAsync();
                transaction.Commit();
                await conn.CloseAsync();
                return reader;
            }
            catch (Exception e)
            {
                Console.WriteLine(e.ToString());
                transaction.Rollback();
                await conn.CloseAsync();
                return null;
            }
        }

        public static async Task<Object> RunExecuteScalarAsync(NpgsqlCommand cmd)
        {
            //Console.WriteLine("RunExecuteScalarAsync CALLED");
            NpgsqlConnection conn = new NpgsqlConnection(connectionString);
            await conn.OpenAsync();
            NpgsqlTransaction transaction = conn.BeginTransaction();
            try
            {
                cmd.Connection = conn;
                await cmd.PrepareAsync();
                Object obj = await cmd.ExecuteScalarAsync();
                transaction.Commit();
                await conn.CloseAsync();
                return obj;
            }
            catch (Exception e)
            {
                Console.WriteLine(e.ToString());
                transaction.Rollback();
                await conn.CloseAsync();
                return null;
            }
        }
    }
}
