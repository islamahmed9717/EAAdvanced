using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TelegramEAManager
{
    // Processed signal model
    public class ProcessedSignal
    {
        public string Id { get; set; } = "";
        public DateTime DateTime { get; set; }
        public long ChannelId { get; set; }
        public string ChannelName { get; set; } = "";
        public string OriginalText { get; set; } = "";
        public string Status { get; set; } = "";
        public string ErrorMessage { get; set; } = "";
        public ParsedSignalData ParsedData { get; set; } = new ParsedSignalData();
    }
}
