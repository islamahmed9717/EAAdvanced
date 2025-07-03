using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TelegramEAManager
{
    // Parsed signal data
    public class ParsedSignalData
    {
        public string Symbol { get; set; } = "";
        public string Direction { get; set; } = "";
        public string OriginalSymbol { get; set; } = "";
        public string FinalSymbol { get; set; } = "";
        public double EntryPrice { get; set; }
        public double StopLoss { get; set; }
        public double TakeProfit1 { get; set; }
        public double TakeProfit2 { get; set; }
        public double TakeProfit3 { get; set; }
    }
}
