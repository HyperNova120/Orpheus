using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using Npgsql;

namespace Orpheus.Database
{
    public class OrpheusDatabaseHandler
    {
        public async Task<bool> StoreUserAsync(DUser user)
        {
            user.username = user.username.Replace("'", "''");
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
                Console.WriteLine($"UPDATING:{user.username}");
                return await UpdateUserAsync(user);
            }
            string query =
                "INSERT INTO orpheusdata.userinfo (userid, username) "
                + $"VALUES ('{user.userId}', '{user.username}');";
            Console.WriteLine($"STORING:{user.username}");
            return await DBEngine.RunExecuteNonQueryAsync(query);
        }

        public async Task<bool> UpdateUserAsync(DUser user)
        {
            user.username = user.username.Replace("'", "''");
            string query =
                "UPDATE orpheusdata.userinfo SET "
                + $"username = '{user.username}' "
                + $"WHERE userid = '{user.userId}';";
            return await DBEngine.RunExecuteNonQueryAsync(query);
        }

        public async Task<bool> StoreServerAsync(DServer server)
        {
            server.serverName = server.serverName.Replace("'", "''");
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
            string query =
                "INSERT INTO orpheusdata.serverinfo (serverid, servername, jailid, jailroleid, jailcourtid) VALUES "
                + $"('{server.serverID}',"
                + $"'{server.serverName}',"
                + $"'{server.jailChannelID}',"
                + $"'{server.JailRoleID}',"
                + $"'{server.JailCourtID}');";
            return await DBEngine.RunExecuteNonQueryAsync(query);
        }

        public async Task<bool> UpdateServerAsync(DServer server)
        {
            server.serverName = server.serverName.Replace("'", "''");
            string query =
                $"UPDATE orpheusdata.serverinfo SET "
                + $"servername = '{server.serverName}', "
                + $"jailid = '{server.jailChannelID}', "
                + $"jailroleid = '{server.JailRoleID}', "
                + $"jailcourtid = '{server.JailRoleID}' "
                + $"WHERE serverid = '{server.serverID}';";
            return await DBEngine.RunExecuteNonQueryAsync(query);
        }

        public async Task<bool> StoreMsgAsync(DMsg msg)
        {
            string query =
                "INSERT INTO orpheusdata.messages (msgid, serverid, userid, channelid, sendtime, dmsgid, messagetext) VALUES "
                + $"(default,"
                + $"'{msg.serverID}',"
                + $"'{msg.userID}',"
                + $"'{msg.channelID}',"
                + $"'{msg.sendingTime}',"
                + $"'{msg.dmsgID}',"
                + $"'{msg.msgText}');";
            return await DBEngine.RunExecuteNonQueryAsync(query);
        }

        public async Task<bool> StoreAttachmentAsync(DAttachment dAttachment)
        {
            string query =
                "INSERT INTO orpheusdata.attachments (id, serverid, userid, dmsgid, url) VALUES "
                + $"(default, "
                + $"'{dAttachment.serverID}',"
                + $"'{dAttachment.userID}',"
                + $"'{dAttachment.msgID}',"
                + $"'{dAttachment.url}');";
            return await DBEngine.RunExecuteNonQueryAsync(query);
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
