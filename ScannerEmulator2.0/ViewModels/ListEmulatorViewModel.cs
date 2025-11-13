using ScannerEmulator2._0.Factories;
using ScannerEmulator2._0.Services;
using ScannerEmulator2._0.TCPScanner;
using System.Windows.Documents;

namespace ScannerEmulator2._0.ViewModels
{
    class ListEmulatorViewModel
    {
        public List<EmulatorViewModel> emulators { get; set; }

        public Action<List<TcpCameraEmulator>> ListChanged { get; set; }

        private CamerasHanlderService _service;
        private EmulatorFactory _factory;

        public ListEmulatorViewModel(CamerasHanlderService service, EmulatorFactory factory)
        {
            emulators = new List<EmulatorViewModel>();
            _service = service;
            _factory = factory;
        }

        
        public async void CreateEmulator(string ip,int port)
        {
            var name = _factory.Create(ip, port);
            var camera = _service.GetEmulator(name);
            await camera.StartAsync();
        }
        public List<EmulatorViewModel> GetEmulatorList()
        {
            return _service.GetEmulatorList();
        }
        public async void AssignFile(string name, string path)
        {
            var camera = _service.GetEmulator(name);
            camera.SetFile(path);
        }
    }
}
