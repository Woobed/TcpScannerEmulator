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

        public string Create(string ip, int port)
        {
            if (camerasHanlderService.GetEmulatorList().Where(t => t.Ip == ip && t.Port == port).Any())
            {
                return string.Empty;
            }
            return camerasHanlderService.AddEmulator(new TcpCameraEmulator(ip,port));
        }
    }
}
