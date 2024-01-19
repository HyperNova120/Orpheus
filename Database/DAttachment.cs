using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Orpheus.Database
{
    public class DAttachment
    {
        public ulong msgID { get; set; }
        public ulong userID { get; set; }
        public ulong serverID { get; set; }
        public ulong channelID { get; set; }
        public string url { get; set; }
    }
}
