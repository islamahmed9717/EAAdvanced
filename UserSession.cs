using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TelegramEAManager
{
    // User session model
    public class UserSession
    {
        public string PhoneNumber { get; set; } = "";
        public string SessionData { get; set; } = "";
        public DateTime CreatedAt { get; set; }
        public DateTime LastUsed { get; set; }
        public bool IsActive { get; set; }
    }

}
