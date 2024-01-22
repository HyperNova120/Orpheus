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

        private static async Task<DBConnectionHandler.ConnectionInfo> GetConnection()
        {
            return await DBConnectionHandler.GetConnection();
        }

        public static void SetConnectionStrings(
            string host,
            string database,
            string username,
            string password
        )
        {
            DBConnectionHandler.SetConnectionStrings(host, database, username, password);
        }

        public static async Task<bool> DoesEntryExist(
            string table,
            string columnName,
            string testForValue
        )
        {
            DBConnectionHandler.ConnectionInfo conninfo = await GetConnection();
            try
            {
                string query =
                    $"SELECT EXISTS(SELECT 1 FROM {table} WHERE {columnName}='{testForValue}');";
                NpgsqlCommand cmd = new NpgsqlCommand(query, conninfo.npgsqlConnection);

                bool temp = Convert.ToBoolean(await cmd.ExecuteScalarAsync());
                conninfo.isInUse = false;
                return temp;
            }
            catch (Exception e)
            {
                Console.WriteLine(e.ToString());
                conninfo.isInUse = false;
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
            DBConnectionHandler.ConnectionInfo conninfo = await GetConnection();
            //Console.WriteLine("DoesEntryExist CALLED");
            try
            {
                string query =
                    $"SELECT EXISTS(SELECT 1 FROM {table} WHERE {columnName}='{testForValue}' AND {columnName2}='{testForValue2}');";
                using (NpgsqlCommand cmd = new NpgsqlCommand(query, conninfo.npgsqlConnection))
                {
                    bool temp = Convert.ToBoolean(await cmd.ExecuteScalarAsync());
                    conninfo.isInUse = false;
                    return temp;
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e.ToString());
                conninfo.isInUse = false;
                return false;
            }
        }

        public static async Task<bool> RunExecuteNonQueryAsync(NpgsqlCommand cmd)
        {
            //Console.WriteLine("RunExecuteNonQueryAsync CALLED");
            DBConnectionHandler.ConnectionInfo conninfo = await GetConnection();
            NpgsqlTransaction transaction = conninfo.npgsqlConnection.BeginTransaction();
            try
            {
                cmd.Connection = conninfo.npgsqlConnection;
                await cmd.PrepareAsync();
                await cmd.ExecuteNonQueryAsync();
                transaction.Commit();
                transaction.Dispose();
                conninfo.isInUse = false;
                return true;
            }
            catch (Exception e)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine("ERROR:" + cmd.CommandText);
                Console.ResetColor();
                Console.WriteLine(e.ToString());
                transaction.Rollback();
                transaction.Dispose();
                conninfo.isInUse = false;
                return false;
            }
        }

        public static async Task<NpgsqlDataReader> RunExecuteReaderAsync(NpgsqlCommand cmd)
        {
            //Console.WriteLine("RunExecuteReaderAsync CALLED");
            DBConnectionHandler.ConnectionInfo conninfo = await GetConnection();
            NpgsqlTransaction transaction = conninfo.npgsqlConnection.BeginTransaction();
            try
            {
                cmd.Connection = conninfo.npgsqlConnection;
                await cmd.PrepareAsync();
                NpgsqlDataReader reader = await cmd.ExecuteReaderAsync();
                transaction.Commit();
                transaction.Dispose();
                conninfo.isInUse = false;
                return reader;
            }
            catch (Exception e)
            {
                Console.WriteLine(e.ToString());
                transaction.Rollback();
                transaction.Dispose();
                conninfo.isInUse = false;
                return null;
            }
        }

        public static async Task<object> RunExecuteScalarAsync(NpgsqlCommand cmd)
        {
            //Console.WriteLine("RunExecuteScalarAsync CALLED");
            DBConnectionHandler.ConnectionInfo conninfo = await GetConnection();
            NpgsqlTransaction transaction = conninfo.npgsqlConnection.BeginTransaction();
            try
            {
                cmd.Connection = conninfo.npgsqlConnection;
                await cmd.PrepareAsync();
                object? obj = await cmd.ExecuteScalarAsync();
                transaction.Commit();
                transaction.Dispose();
                conninfo.isInUse = false;
                return obj;
            }
            catch (Exception e)
            {
                Console.WriteLine(e.ToString());
                transaction.Rollback();
                transaction.Dispose();
                conninfo.isInUse = false;
                return null;
            }
        }
    }
}
