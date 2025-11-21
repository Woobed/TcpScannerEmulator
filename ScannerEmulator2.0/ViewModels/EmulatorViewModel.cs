using ScannerEmulator2._0.Abstractions;
using ScannerEmulator2._0.Dto;
using System.ComponentModel;
using System.Runtime.CompilerServices;

public class EmulatorViewModel : INotifyPropertyChanged, IViewModel
{
    public string Name { get; set; } = string.Empty;
    public string Ip { get; set; } = string.Empty;
    public int Port { get; set; }
    public string FileName { get; set; } = string.Empty;
    public string DataHeader { get; set; } = string.Empty;
    public string DataTerminator { get; set; } = string.Empty;
    public string DataSeparator { get; set; } = "|";
    public string Delay { get; set; } = "1000";
    public string GroupCount { get; set; } = "1";

    private string _sendLines = "0";
    private string _finalLines = "0";
    private int _progressValue = 0;

    public string SendLines
    {
        get => _sendLines;
        set
        {
            _sendLines = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(ProgressText));
            UpdateProgressValue();
        }
    }

    public string FinalLines
    {
        get => _finalLines;
        set
        {
            _finalLines = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(ProgressText));
            UpdateProgressValue();
        }
    }

    public int ProgressValue
    {
        get => _progressValue;
        set
        {
            _progressValue = value;
            OnPropertyChanged();
        }
    }

    public string ProgressText => $"{SendLines}/{FinalLines}";

    private void UpdateProgressValue()
    {
        if (int.TryParse(SendLines, out int sent) &&
            int.TryParse(FinalLines, out int total) &&
            total > 0)
        {
            ProgressValue = (int)((double)sent / (double)total * 100);
        }
        else
        {
            ProgressValue = 0;
        }
    }

    public event PropertyChangedEventHandler PropertyChanged;
    protected void OnPropertyChanged([CallerMemberName] string name = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }

    public TaskSettings GetTaskSettings() =>
        new TaskSettings
        {
            DataHeader = DataHeader,
            DataTerminator = DataTerminator,
            DataSeparator = DataSeparator,
            Delay = int.TryParse(Delay, out int d) ? d : 1000,
            GroupCount = int.TryParse(GroupCount, out int g) ? g : 1
        };
}
