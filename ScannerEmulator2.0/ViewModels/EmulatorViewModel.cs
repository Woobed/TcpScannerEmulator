using ScannerEmulator2._0.Abstractions;

namespace ScannerEmulator2._0.ViewModels
{
    public class EmulatorViewModel : IViewModel
    {
        public string Name { get; set; } = string.Empty;
        public string Ip { get; set; } = string.Empty;
        public int Port { get; set; } = 0;

        
    }
}
