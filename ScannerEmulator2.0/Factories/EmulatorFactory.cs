using ScannerEmulator2._0.Services;
using ScannerEmulator2._0.TCPScanner;

namespace ScannerEmulator2._0.Factories
{
    public class EmulatorFactory
    {
        private LoggerService _logger {  get; set; }
        public EmulatorFactory(LoggerService logger) { _logger = logger; }
        public TcpCameraEmulator Create(string ip, int port)
        {
            return new TcpCameraEmulator(ip, port, _logger);
        }
    }
}
