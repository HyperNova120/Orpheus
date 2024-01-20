using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using Npgsql;
using Npgsql.PostgresTypes;
using NpgsqlTypes;

namespace Orpheus.Database
{
    public static class OrpheusDatabaseHandler
    {
        public static async Task<bool> StoreUserAsync(DUser user)
        {
            if (
                Convert.ToBoolean(
                    await DBEngine.DoesEntryExist(
                        "orpheusdata.userinfo",
                        "userid",
                        user.userId.ToString()
                    )
                )
            )
            {
                Console.WriteLine($"UPDATING USER:{user.username}");
                return await UpdateUserAsync(user);
            }
            Console.WriteLine($"STORING USER:{user.username}");
            NpgsqlCommand cmd = new NpgsqlCommand(
                "INSERT INTO orpheusdata.userinfo (id, userid, username) VALUES (default,$1,$2)"
            )
            {
                Parameters =
                {
                    new NpgsqlParameter() { Value = Convert.ToDecimal(user.userId) },
                    new NpgsqlParameter() { Value = user.username }
                }
            };
            return await DBEngine.RunExecuteNonQueryAsync(cmd);
        }

        public static async Task<bool> UpdateUserAsync(DUser user)
        {
            NpgsqlCommand cmd = new NpgsqlCommand(
                $"UPDATE orpheusdata.userinfo SET username=$1 WHERE userid={user.userId}"
            )
            {
                Parameters =
                {
                    new NpgsqlParameter() { Value = user.username },
                    new NpgsqlParameter() { Value = Convert.ToDecimal(user.userId) }
                }
            };
            return await DBEngine.RunExecuteNonQueryAsync(cmd);
        }

        public static async Task<bool> StoreAdminAsync(DAdmin user)
        {
            if (
                Convert.ToBoolean(
                    await DBEngine.DoesEntryExist(
                        "orpheusdata.admininfo",
                        "userid",
                        user.userID.ToString()
                    )
                )
            )
            {
                Console.WriteLine($"ADMIN ALREADY EXIST:{user.userID}");
                return false;
            }
            Console.WriteLine($"SETTING ADMIN:{user.userID}");
            NpgsqlCommand cmd = new NpgsqlCommand(
                "INSERT INTO orpheusdata.admininfo (adminid, serverid, userid) VALUES (default,$1,$2)"
            )
            {
                Parameters =
                {
                    new NpgsqlParameter() { Value = Convert.ToDecimal(user.serverID) },
                    new NpgsqlParameter() { Value = Convert.ToDecimal(user.userID) }
                }
            };
            return await DBEngine.RunExecuteNonQueryAsync(cmd);
        }

        public static async Task<bool> RemoveAdminAsync(DAdmin user)
        {
            if (
                !Convert.ToBoolean(
                    await DBEngine.DoesEntryExist(
                        "orpheusdata.admininfo",
                        "userid",
                        user.userID.ToString()
                    )
                )
            )
            {
                Console.WriteLine($"ADMIN DOES NOT EXIST:{user.userID}");
                return false;
            }
            Console.WriteLine($"REMOVING ADMIN:{user.userID}");
            NpgsqlCommand cmd = new NpgsqlCommand(
                "DELETE FROM orpheusdata.admininfo WHERE serverid=$1 AND userid=$2"
            )
            {
                Parameters =
                {
                    new NpgsqlParameter() { Value = Convert.ToDecimal(user.serverID) },
                    new NpgsqlParameter() { Value = Convert.ToDecimal(user.userID) }
                }
            };
            return await DBEngine.RunExecuteNonQueryAsync(cmd);
        }

        public static async Task<bool> StoreServerAsync(DServer server)
        {
            if (
                Convert.ToBoolean(
                    await DBEngine.DoesEntryExist(
                        "orpheusdata.serverinfo",
                        "serverid",
                        server.serverID.ToString()
                    )
                )
            )
            {
                //entry already exists
                return await UpdateServerAsync(server);
            }
            NpgsqlCommand cmd = new NpgsqlCommand(
                "INSERT INTO orpheusdata.serverinfo (id, serverid, servername, jailid, jailroleid, jailcourtid) VALUES (default,$1, $2, $3, $4, $5)"
            )
            {
                Parameters =
                {
                    new NpgsqlParameter() { Value = Convert.ToDecimal(server.serverID) },
                    new NpgsqlParameter() { Value = server.serverName },
                    new NpgsqlParameter() { Value = Convert.ToDecimal(server.jailChannelID) },
                    new NpgsqlParameter() { Value = Convert.ToDecimal(server.JailRoleID) },
                    new NpgsqlParameter() { Value = Convert.ToDecimal(server.JailCourtID) }
                }
            };
            return await DBEngine.RunExecuteNonQueryAsync(cmd);
        }

        public static async Task<bool> UpdateServerAsync(DServer server)
        {
            NpgsqlCommand cmd = new NpgsqlCommand(
                $"UPDATE orpheusdata.serverinfo SET servername=$1 WHERE serverid={server.serverID}"
            )
            {
                Parameters =
                {
                    new NpgsqlParameter() { Value = server.serverName }
                }
            };
            return await DBEngine.RunExecuteNonQueryAsync(cmd);
        }

        public static async Task<bool> UpdateServerJailRoleID(ulong serverid, ulong id)
        {
            NpgsqlCommand cmd = new NpgsqlCommand(
                $"UPDATE orpheusdata.serverinfo SET jailroleid=$1 WHERE serverid={serverid}"
            )
            {
                Parameters = { new NpgsqlParameter() { Value = Convert.ToDecimal(id) } }
            };
            return await DBEngine.RunExecuteNonQueryAsync(cmd);
        }

        public static async Task<bool> UpdateServerJailChannelID(ulong serverid, ulong id)
        {
            NpgsqlCommand cmd = new NpgsqlCommand(
                $"UPDATE orpheusdata.serverinfo SET jailid=$1 WHERE serverid={serverid}"
            )
            {
                Parameters = { new NpgsqlParameter() { Value = Convert.ToDecimal(id) } }
            };
            return await DBEngine.RunExecuteNonQueryAsync(cmd);
        }

        public static async Task<bool> UpdateServerJailCourtID(ulong serverid, ulong id)
        {
            NpgsqlCommand cmd = new NpgsqlCommand(
                $"UPDATE orpheusdata.serverinfo SET jailcourtid=$1 WHERE serverid={serverid}"
            )
            {
                Parameters = { new NpgsqlParameter() { Value = Convert.ToDecimal(id) } }
            };
            return await DBEngine.RunExecuteNonQueryAsync(cmd);
        }

        public static async Task<ulong> GetJailIDInfo(ulong serverid, string columnName)
        {
            NpgsqlCommand cmd = new NpgsqlCommand(
                $"SELECT {columnName} FROM orpheusdata.serverinfo WHERE serverid={serverid}"
            );
            return Convert.ToUInt64(await DBEngine.RunExecuteScalarAsync(cmd));
        }

        public static async Task<bool> StoreMsgAsync(DMsg msg)
        {
            //Console.WriteLine("MSG ID:"+msg.dmsgID);
            NpgsqlCommand cmd = new NpgsqlCommand(
                "INSERT INTO orpheusdata.messages (msgid, serverid, userid, channelid, sendtime, dmsgid, messagetext) VALUES "
                    + "(default,$1,$2,$3,$4,$5,$6)"
            )
            {
                Parameters =
                {
                    new NpgsqlParameter() { Value = Convert.ToDecimal(msg.serverID) },
                    new NpgsqlParameter() { Value = Convert.ToDecimal(msg.userID) },
                    new NpgsqlParameter() { Value = Convert.ToDecimal(msg.channelID) },
                    new NpgsqlParameter() { Value = msg.sendingTime },
                    new NpgsqlParameter() { Value = Convert.ToDecimal(msg.dmsgID) },
                    new NpgsqlParameter() { Value = msg.msgText }
                }
            };
            return await DBEngine.RunExecuteNonQueryAsync(cmd);
        }

        public static async Task<bool> StoreAttachmentAsync(DAttachment dAttachment)
        {
            NpgsqlCommand cmd = new NpgsqlCommand(
                "INSERT INTO orpheusdata.attachments (id, serverid, userid, dmsgid, url) VALUES "
                    + "(default,$1,$2,$3,$4)"
            )
            {
                Parameters =
                {
                    new NpgsqlParameter()
                    {
                        NpgsqlDbType = NpgsqlTypes.NpgsqlDbType.Numeric,
                        Value = Convert.ToDecimal(dAttachment.serverID)
                    },
                    new NpgsqlParameter()
                    {
                        NpgsqlDbType = NpgsqlTypes.NpgsqlDbType.Numeric,
                        Value = Convert.ToDecimal(dAttachment.userID)
                    },
                    new NpgsqlParameter()
                    {
                        NpgsqlDbType = NpgsqlTypes.NpgsqlDbType.Numeric,
                        Value = Convert.ToDecimal(dAttachment.msgID)
                    },
                    new NpgsqlParameter()
                    {
                        NpgsqlDbType = NpgsqlTypes.NpgsqlDbType.Varchar,
                        Value = dAttachment.url
                    }
                }
            };
            return await DBEngine.RunExecuteNonQueryAsync(cmd);
        }

        public static string ConvertToUFT8(string s)
        {
            return Encoding.UTF8.GetString(Encoding.UTF8.GetBytes(s));
        }

        /*
                private async Task<long> getTotalUsersAsync()
                {
                    try
                    {
                        using (NpgsqlConnection conn = new NpgsqlConnection(connectionString))
                        {
                            await conn.OpenAsync();
                            string query = "SELECT COUNT(*) FROM orpheusdata.userinfo";
                            using (NpgsqlCommand cmd = new NpgsqlCommand(query, conn))
                            {
                                var usercount = await cmd.ExecuteScalarAsync();
                                cmd.ExecuteReaderAsyn
                                return Convert.ToInt64(usercount);
                            }
                        }
                    }
                    catch (Exception e)
                    {
                        Console.WriteLine(e.ToString());
                        return -1;
                    }
                }
        */
    }
}
