using CommunityToolkit.Mvvm.Input;
using ScannerEmulator2._0.Abstractions;
using ScannerEmulator2._0.Factories;
using ScannerEmulator2._0.Services;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;

namespace ScannerEmulator2._0.ViewModels
{
    internal class TaskCreatingViewModel
    {
        private readonly CamerasHanlderService _cameraService;
        private readonly EmulatorFactory _factory;

        public ObservableCollection<ITcpCameraEmulator> Cameras { get; } = new();

        public ICommand AddCameraCommand { get; }

        public TaskCreatingViewModel(CamerasHanlderService service, EmulatorFactory factory)
        {
            _cameraService = service;
            _factory = factory;
            AddCameraCommand = new RelayCommand(AddCamera);
        }

        private void AddCamera()
        {
            _factory.Create("127.0.0.1", 5001);

            // получаем из Singleton-сервиса уже созданные камеры
            var emulator = _cameraService.GetEmulator("Camera1");
            if (emulator != null)
                Cameras.Add(emulator);
        }
    }
}
