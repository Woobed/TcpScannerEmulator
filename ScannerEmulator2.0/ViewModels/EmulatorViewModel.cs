using ScannerEmulator2._0.Abstractions;
using ScannerEmulator2._0.Reactive;

public class EmulatorViewModel : IViewModel
{
    public ReactiveProperty<string> Name { get; set; } = new(string.Empty);
    public ReactiveProperty<string> Ip { get; set; } = new(string.Empty);
    public ReactiveProperty<int> Port { get; set; } = new(0);
    public ReactiveProperty<bool> IsConnected { get; set; } = new(false);

    public void Dispose()
    {
        Name = new(string.Empty);
        Ip = new(string.Empty);
        Port = new(0);
        IsConnected = new(false);
    }
}
