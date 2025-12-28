using ScannerEmulator2._0.Abstractions;
using ScannerEmulator2._0.Factories;

namespace ScannerEmulator2._0.Services
{
    public class CamerasHandlerService
    {
        private List<ITcpCameraEmulator> tcpCameras { get; set; } = new();
        public Action? ListInfoChanged { get; set; }

        private readonly EmulatorFactory _factory;

        public CamerasHandlerService(EmulatorFactory factory)
        {
            _factory = factory;
        }

        public ITcpCameraEmulator? GetEmulator(string ip, int port)
        {
            var instance = tcpCameras.Where(t => t.Ip == ip && t.Port == port).FirstOrDefault();
            if (instance == null) return null;
            return instance;
        }
        
        public List<EmulatorViewModel> GetEmulatorList(Func<ITcpCameraEmulator, bool>? predicate = null)
        {
            List<EmulatorViewModel> list = new List<EmulatorViewModel>();
            var filteredList = predicate == null ? tcpCameras : tcpCameras.Where(camera => predicate(camera));
            foreach (var camera in filteredList)
            {
                var type = camera.GetType();
                var vm = new EmulatorViewModel();
                Mapper.Map(camera, vm);
                list.Add(vm);
            }
            return list;
        }
        
        public bool CreateEmulator(string ip, int port)
        {
            if (!tcpCameras.Where(t => t.Ip == ip && t.Port == port).Any())
            {
                tcpCameras.Add(_factory.Create(ip, port));
                InvokeListChanged();
                return true;
            }
            return false;
        }
        
        private void InvokeListChanged()
        {
            ListInfoChanged?.Invoke();
        }

        public void RemoveEmulator(string ip, int port)
        {
            var result = tcpCameras.RemoveAll(t => t.Ip == ip && t.Port == port);
            if (result != 0)
            {
                InvokeListChanged();
            }
        }
    }
}
