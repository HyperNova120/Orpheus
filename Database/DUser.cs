using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Orpheus.Database
{
    public class DUser
    {
        public required string username { get; set; }
        public ulong userId { get; set; }
    }
}
