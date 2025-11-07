using ScannerEmulator2._0.Abstractions;

namespace ScannerEmulator2._0.Services
{
    public class CamerasHanlderService
    {
        private List<ITcpCameraEmulator> tcpCameras { get; set; } = new();

        public CamerasHanlderService()
        {
            
        }

        public ITcpCameraEmulator GetEmulator(string name)
        {
            var instance = tcpCameras.Where(t => t.Name == name).FirstOrDefault();
            if (instance == null) return default;
            return instance;
        }
        public void AddEmulator(ITcpCameraEmulator instance)
        {
            tcpCameras.Add(instance);
        }
        public void RemoveEmulator(string name)
        {
            tcpCameras.RemoveAll(i => i.Name == name);
        }

    }
}
