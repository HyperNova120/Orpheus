using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Orpheus.Database
{
    public class DMsg
    {
        public ulong serverID { get; set; }
        public ulong userID { get; set; }
        public ulong channelID { get; set; }
        public DateTime sendingTime { get; set; }
        public string msgText { get; set; }
        public ulong dmsgID { get; set; }
    }
}
