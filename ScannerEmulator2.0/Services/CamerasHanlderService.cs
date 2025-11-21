using ScannerEmulator2._0.Abstractions;

namespace ScannerEmulator2._0.Services
{
    public class CamerasHanlderService
    {
        private List<ITcpCameraEmulator> tcpCameras { get; set; } = new();
        public Action ListInfoChanged {  get; set; }

        public ITcpCameraEmulator GetEmulator(string name)
        {
            var instance = tcpCameras.Where(t => t.Name == name).FirstOrDefault();
            if (instance == null) return default;
            return instance;
        }
        public List<EmulatorViewModel> GetEmulatorList(Func<ITcpCameraEmulator,bool> predicate = null)
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
        public string AddEmulator(ITcpCameraEmulator instance)
        {
            instance.InfoChanged += InvokeListChanged;
            tcpCameras.Add(instance);
            return instance.Name;
        }
        private void InvokeListChanged()
        {
            ListInfoChanged.Invoke();
        }
        public void RemoveEmulator(string name)
        {
            tcpCameras.FirstOrDefault(i => i.Name == name).Stop();
            tcpCameras.RemoveAll(i => i.Name == name);
            ListInfoChanged.Invoke();
        }

    }
}
