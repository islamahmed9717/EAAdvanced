using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TelegramEAManager
{
    public class ChannelInfo
    {
        public long Id { get; set; }  // Changed from int to long
        public string Title { get; set; } = "";
        public string Username { get; set; } = "";
        public string Type { get; set; } = "";
        public int MembersCount { get; set; }
        public long AccessHash { get; set; }
        public DateTime LastActivity { get; set; }
    }
}
