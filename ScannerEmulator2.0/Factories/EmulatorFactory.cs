using ScannerEmulator2._0.Services;
using ScannerEmulator2._0.TCPScanner;
using System.Net;

namespace ScannerEmulator2._0.Factories
{
    public class EmulatorFactory
    {
        private CamerasHanlderService camerasHanlderService;
        
        public EmulatorFactory(CamerasHanlderService service)
        {
            camerasHanlderService = service;
        }

        public void Create(string ip, int port)
        {
            camerasHanlderService.AddEmulator(new TcpCameraEmulator(ip,port));
        }
    }
}
