using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TelegramEAManager
{
    public class SymbolMapping
    {
        public Dictionary<string, string> Mappings { get; set; } = new Dictionary<string, string>();
        public string Prefix { get; set; } = "";
        public string Suffix { get; set; } = "";
        public List<string> SkipPrefixSuffix { get; set; } = new List<string>();
        public List<string> ExcludedSymbols { get; set; } = new List<string>();
        public List<string> AllowedSymbols { get; set; } = new List<string>();
    }
}
