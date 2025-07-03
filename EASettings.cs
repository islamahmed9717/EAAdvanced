using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TelegramEAManager
{
    // EA settings model
    public class EASettings
    {
        public string ChannelIds { get; set; } = "";
        public string SignalFilePath { get; set; } = "TelegramSignals.txt";
        public string RiskMode { get; set; } = "FIXED_LOT";
        public double FixedLotSize { get; set; } = 0.01;
        public double RiskPercent { get; set; } = 2.0;
        public double RiskAmount { get; set; } = 100.0;
        public bool UseTrailingStop { get; set; } = false;
        public int TrailingStartPips { get; set; } = 20;
        public int TrailingStepPips { get; set; } = 5;
        public bool MoveSLToBreakeven { get; set; } = true;
        public int BreakevenAfterPips { get; set; } = 15;
        public bool SendNotifications { get; set; } = true;
        public string MT4FilesPath { get; set; } = "";
    }
}
