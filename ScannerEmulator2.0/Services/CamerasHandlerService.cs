using ScannerEmulator2._0.Abstractions;
using ScannerEmulator2._0.Factories;
using ScannerEmulator2._0.Reactive;
using ScannerEmulator2._0.TCPScanner;

namespace ScannerEmulator2._0.Services
{
    public class CamerasHandlerService
    {
        private List<TcpCameraEmulator> tcpCameras { get; set; } = new();
        private List<EmulatorViewModel> vms = new List<EmulatorViewModel>();
        public Action? ListInfoChanged { get; set; }

        private readonly EmulatorFactory _factory;

        public CamerasHandlerService(EmulatorFactory factory)
        {
            _factory = factory;
        }

        public TcpCameraEmulator? GetEmulator(string ip, int port)
        {
            var instance = tcpCameras.Where(t => t.Ip.Value == ip && t.Port.Value == port).FirstOrDefault();
            if (instance == null) return null;
            return instance;
        }

        //public List<EmulatorViewModel> GetEmulatorList(Func<ITcpCameraEmulator, bool>? predicate = null)
        //{
        //    var filteredList = predicate == null ? tcpCameras : tcpCameras.Where(camera => predicate(camera));
        //    return vms;
        //}
        public List<EmulatorViewModel> GetEmulatorList()
        {
            return vms;
        }
        public async Task<bool> CreateEmulator(string ip, int port, Delegate updateMethod)
        {
            if (!tcpCameras.Where(t => t.Ip.Value == ip && t.Port.Value == port).Any())
            {
                var camera = _factory.Create(ip, port);
                tcpCameras.Add(camera);
                var vm = new EmulatorViewModel();
                Mapper.Map(camera, vm);
                vms.Add(vm);

                vm.IsConnected.PropertyChanged += (sender, e) =>
                {
                    if (e.PropertyName == nameof(ReactiveProperty<bool>.Value))
                    {
                        // Преобразуем Delegate в Action<bool> и вызываем
                        if (updateMethod is Action<bool> action)
                        {
                            action(vm.IsConnected.Value);
                        }
                    }
                };

                InvokeListChanged();
                await camera.StartAsync();
                return true;
            }
            return false;
        }
        
        private void InvokeListChanged()
        {
            ListInfoChanged?.Invoke();
        }

        public void RemoveEmulator(string name)
        {
            tcpCameras.FirstOrDefault(t => t.Name.Value == name)?.Stop();
            var result = tcpCameras.RemoveAll(t => t.Name.Value == name);
            if (result != 0)
            {
                var vm = vms.FirstOrDefault(t => t.Name.Value == name);
                vms.RemoveAll(t => t.Name.Value == name);
                vm?.Dispose();
                InvokeListChanged();
            }
        }
    }
}
