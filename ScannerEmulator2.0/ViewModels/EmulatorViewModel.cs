using ScannerEmulator2._0.Abstractions;
using ScannerEmulator2._0.Dto;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace ScannerEmulator2._0.ViewModels
{
    public class EmulatorViewModel : IViewModel, INotifyPropertyChanged
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


        private string _sendLines = "0";
        private string _finalLines = "0";
        private int _progressValue = 0;

        public string SendLines
        {
            get { return _sendLines; }
            set
            {
                _sendLines = value;
                OnPropertyChanged(nameof(SendLines));
                OnPropertyChanged(nameof(ProgressText));
                UpdateProgressValue();
            }
        }

        public string FinalLines
        {
            get { return _finalLines; }
            set
            {
                _finalLines = value;
                OnPropertyChanged(nameof(FinalLines));
                OnPropertyChanged(nameof(ProgressText));
                UpdateProgressValue();
            }
        }
        public int ProgressValue
        {
            get { return _progressValue; }
            set
            {
                _progressValue = value;
                OnPropertyChanged(nameof(ProgressValue));
            }
        }

        // Для TextBlock (текстовое представление)
        public string ProgressText => $"{SendLines}/{FinalLines}";

        private void UpdateProgressValue()
        {
            if (int.TryParse(SendLines, out int sent) && int.TryParse(FinalLines, out int total) && total > 0)
            {
                ProgressValue = (int)((double)sent / total * 100);
            }
            else
            {
                ProgressValue = 0;
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

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
