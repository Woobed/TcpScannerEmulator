using ScannerEmulator2._0.Abstractions;
using ScannerEmulator2._0.Dto;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace ScannerEmulator2._0.ViewModels
{
    public class EmulatorViewModel : IViewModel
    {
        public string Name { get; set; } = string.Empty;
        public string Ip { get; set; } = string.Empty;
        public int Port { get; set; } = 0;
        public string FileName { get; set; }  = string.Empty;

        public string DataHeader { get; set; } = string.Empty;
        public string DataTerminator { get; set; } = string.Empty;
        public string DataSeparator { get; set; } = "|";
        public string Delay { get; set; } = "1000";
        public string GroupCount { get; set; } = "1" ;

        //public event PropertyChangedEventHandler PropertyChanged;

        //protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        //{
        //    PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        //}

        public TaskSettings GetTaskSettings()
        {
            return new TaskSettings
            {
                DataHeader = this.DataHeader,
                DataTerminator = this.DataTerminator,
                DataSeparator = this.DataSeparator,
                Delay = int.TryParse(this.Delay, out int delay) ? delay : 1000,
                GroupCount = int.TryParse(this.GroupCount, out int groupCount) ? groupCount : 1

            };
        }
    }
}
