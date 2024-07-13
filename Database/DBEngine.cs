using Npgsql;
namespace Orpheus.Database
{
    public static class DBEngine
    {
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
            DBConnectionHandler.ConnectionInfo conninfo = await DBConnectionHandler.GetConnection();

            try
            {
                string query =
                    $"SELECT EXISTS(SELECT 1 FROM {table} WHERE {columnName}='{testForValue}');";
                NpgsqlCommand cmd = new NpgsqlCommand(query, conninfo.npgsqlConnection);

                bool temp = Convert.ToBoolean(await cmd.ExecuteScalarAsync());
                conninfo.closeMe();
                return temp;
            }
            catch (Exception e)
            {
                Console.WriteLine(e.ToString());
                conninfo.closeMe();
                return false;
            }
        }

        [Obsolete("Use array param version")]
        public static async Task<bool> DoesEntryExist(
             string table,
             string columnName,
             string columnName2,
             string testForValue,
             string testForValue2
         )
        {
            DBConnectionHandler.ConnectionInfo conninfo = await DBConnectionHandler.GetConnection();
            //Console.WriteLine("DoesEntryExist CALLED");
            try
            {
                string query =
                    $"SELECT EXISTS(SELECT 1 FROM {table} WHERE {columnName}='{testForValue}' AND {columnName2}='{testForValue2}');";
                using (NpgsqlCommand cmd = new NpgsqlCommand(query, conninfo.npgsqlConnection))
                {
                    bool temp = Convert.ToBoolean(await cmd.ExecuteScalarAsync());
                    conninfo.closeMe();
                    return temp;
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e.ToString());
                conninfo.closeMe();
                return false;
            }
        }

        public static async Task<bool> DoesEntryExist(
            string table,
            string[] columnNames,
            string[] testForValues
        )
        {
            if (columnNames.Length != testForValues.Length)
            {
                Console.WriteLine("[ERROR] ColumnName testForValues Length Mismatch");
                return false;
            }
            DBConnectionHandler.ConnectionInfo conninfo = await DBConnectionHandler.GetConnection();
            //Console.WriteLine("DoesEntryExist CALLED");
            try
            {
                string query =
                    $"SELECT EXISTS(SELECT 1 FROM {table} WHERE";
                for (int i = 0; i < columnNames.Length; i++)
                {
                    query += $" {columnNames[i]}='{testForValues[i]}'";
                    if (i != columnNames.Length - 1)
                    {
                        query += " AND";
                    }
                }
                query += ");";

                using (NpgsqlCommand cmd = new NpgsqlCommand(query, conninfo.npgsqlConnection))
                {
                    bool temp = Convert.ToBoolean(await cmd.ExecuteScalarAsync());
                    conninfo.closeMe();
                    return temp;
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e.ToString());
                conninfo.closeMe();
                return false;
            }
        }

        public static async Task<bool> RunExecuteNonQueryAsync(NpgsqlCommand cmd)
        {
            //Console.WriteLine("RunExecuteNonQueryAsync CALLED");
            DBConnectionHandler.ConnectionInfo conninfo = await DBConnectionHandler.GetConnection();
            NpgsqlTransaction transaction = conninfo.npgsqlConnection.BeginTransaction();
            try
            {
                cmd.Connection = conninfo.npgsqlConnection;
                await cmd.PrepareAsync();
                await cmd.ExecuteNonQueryAsync();
                transaction.Commit();
                transaction.Dispose();
                conninfo.closeMe();
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
                conninfo.closeMe();
                return false;
            }
        }

        public static async Task<NpgsqlDataReader> RunExecuteReaderAsync(NpgsqlCommand cmd)
        {
            //Console.WriteLine("RunExecuteReaderAsync CALLED");
            DBConnectionHandler.ConnectionInfo conninfo = await DBConnectionHandler.GetConnection();
            NpgsqlTransaction transaction = conninfo.npgsqlConnection.BeginTransaction();
            try
            {
                cmd.Connection = conninfo.npgsqlConnection;
                await cmd.PrepareAsync();
                NpgsqlDataReader reader = await cmd.ExecuteReaderAsync();
                transaction.Commit();
                transaction.Dispose();
                conninfo.closeMe();
                return reader;
            }
            catch (Exception e)
            {
                Console.WriteLine(e.ToString());
                transaction.Rollback();
                transaction.Dispose();
                conninfo.closeMe();
                return null;
            }
        }

        public static async Task<object> RunExecuteScalarAsync(NpgsqlCommand cmd)
        {
            //Console.WriteLine("RunExecuteScalarAsync CALLED");
            DBConnectionHandler.ConnectionInfo conninfo = await DBConnectionHandler.GetConnection();
            NpgsqlTransaction transaction = conninfo.npgsqlConnection.BeginTransaction();
            try
            {
                cmd.Connection = conninfo.npgsqlConnection;
                await cmd.PrepareAsync();
                object? obj = await cmd.ExecuteScalarAsync();
                transaction.Commit();
                transaction.Dispose();
                conninfo.closeMe();
                return obj;
            }
            catch (Exception e)
            {
                Console.WriteLine(e.ToString());
                transaction.Rollback();
                transaction.Dispose();
                conninfo.closeMe();
                return null;
            }
        }
    }
}
