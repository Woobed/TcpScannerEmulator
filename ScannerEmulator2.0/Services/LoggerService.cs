using System.Collections.ObjectModel;
using ScannerEmulator2._0.Dto;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows;

namespace ScannerEmulator2._0.Services
{
    public class LoggerService : INotifyPropertyChanged
    {
        private readonly object _lock = new();
        private readonly ObservableCollection<string> _logs = new();
        private const int MaxLogs = 1000;

        public event PropertyChangedEventHandler? PropertyChanged;

        public ObservableCollection<string> Logs
        {
            get => _logs;
        }

        public void Log(Log log)
        {
            var message = $"[{DateTime.UtcNow:HH:mm:ss}][{log.CameraName}][{log.FileName}] {log.Message}";

            Application.Current.Dispatcher.Invoke(() =>
            {
                lock (_lock)
                {
                    _logs.Add(message);

                    while (_logs.Count > MaxLogs)
                    {
                        _logs.RemoveAt(0);
                    }
                }

                OnPropertyChanged(nameof(Logs));
            });
        }

        public void Clear()
        {
            Application.Current.Dispatcher.Invoke(() =>
            {
                lock (_lock)
                {
                    _logs.Clear();
                }
                OnPropertyChanged(nameof(Logs));
            });
        }

        protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}