using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TelegramEAManager
{
    public class ApiSettings
    {
        public int ApiId { get; set; }
        public string ApiHash { get; set; } = "";
        public DateTime SavedDate { get; set; }
        public string Username { get; set; } = "";
    }
}
