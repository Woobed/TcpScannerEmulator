using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ScannerEmulator2._0.Dto
{
    public class TaskSettings
    {
        public string DataHeader { get; set; } = string.Empty;
        public string DataTerminator { get; set; } = "|";
        public string DataSeparator { get; set; } = string.Empty;

        public int Delay { get; set; } = 1000;
        public int GroupCount { get; set; } = 1;
    }
}
