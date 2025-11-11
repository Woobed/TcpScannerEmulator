using ScannerEmulator2._0.Abstractions;
using ScannerEmulator2._0.ViewModels;
using System.Diagnostics;

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
            //var type = instance.GetType();
            //var vm = new EmulatorViewModel();
            //Mapper.Map(instance, vm);
            return instance;
        }
        public List<EmulatorViewModel> GetEmulatorList()
        {
            List<EmulatorViewModel> list = new List<EmulatorViewModel>();
            foreach (var camera in tcpCameras)
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
            tcpCameras.Add(instance);
            return instance.Name;
        }
        public void RemoveEmulator(string name)
        {
            tcpCameras.FirstOrDefault(i => i.Name == name).Stop();
            tcpCameras.RemoveAll(i => i.Name == name);
        }

    }
}
