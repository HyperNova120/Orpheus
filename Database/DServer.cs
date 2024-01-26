
namespace Orpheus.Database
{
    public class DServer
    {
        public ulong serverID { get; set; }
        public required string serverName { get; set; }
        public ulong jailChannelID { get; set; }
        public ulong JailRoleID { get; set; }
        public ulong JailCourtID { get; set; }
        public ulong JailCourtRoleID { get; set; }
    }
}
