using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TelegramEAManager
{
    // Application settings
    public class AppSettings
    {
        public List<string> SavedAccounts { get; set; } = new List<string>();
        public string LastPhoneNumber { get; set; } = "";
        public string MT4Path { get; set; } = "";
        public DateTime LastUsed { get; set; } = DateTime.Now;
    }

}
