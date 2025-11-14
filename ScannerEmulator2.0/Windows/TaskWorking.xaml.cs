using Microsoft.Extensions.DependencyInjection;
using ScannerEmulator2._0.Services;
using ScannerEmulator2._0.TCPScanner;
using ScannerEmulator2._0.ViewModels;
using System.Collections.Concurrent;
using System.Windows;
using System.Windows.Controls;

namespace ScannerEmulator2._0.Windows
{
    /// <summary>
    /// Логика взаимодействия для TaskWorking.xaml
    /// </summary>
    public partial class TaskWorking : UserControl
    {
        private readonly CamerasHanlderService _service;
        private ConcurrentDictionary<string, Action<int, int>> handlersDictionary;
        public TaskWorking()
        {
            InitializeComponent();
            _service = App.AppHost.Services.GetRequiredService<CamerasHanlderService>();
            _service.ListInfoChanged += LoadActiveCameras;
            handlersDictionary = new();
        }

        private void LoadActiveCameras()
        {
            var cameras = _service.GetEmulatorList(t => t.IsReady);
            ActiveCamerasPanel.ItemsSource = cameras;
        }
        private void Start_Click(object sender, RoutedEventArgs e)
        {
            var button = sender as Button;
            string name = button?.Tag as string;

            // Находим ViewModel камеры
            var cameraViewModel = ActiveCamerasPanel.ItemsSource?
                .OfType<EmulatorViewModel>()
                .FirstOrDefault(vm => vm.Name == name);

            if (cameraViewModel != null)
            {
                var camera = _service.GetEmulator(name);
                if (camera is TcpCameraEmulator cam)
                {
                    if (handlersDictionary.TryRemove(name, out var handlerToRemove))
                    {
                        cam.SendNotification -= handlerToRemove;
                    }
                    // Получаем настройки из ViewModel
                    var settings = cameraViewModel.GetTaskSettings();

                    Action<int, int> handler = (sent, total) =>
                    {
                        HandleSend(sent, total, cameraViewModel);
                    };
                    handlersDictionary.TryAdd(cameraViewModel.Name, handler);
                    cam.SendNotification += handler;
                    _ = cam.StartStreaming(settings);
                }
            }
        }
        private void HandleSend(int sentlines, int finallines, EmulatorViewModel vm)
        {
            vm.SendLines = sentlines.ToString();
            vm.FinalLines = finallines.ToString();
        }
        private void Pause_Click(object sender, RoutedEventArgs e)
        {
            string name = ((FrameworkElement)sender).Tag.ToString();
            var camera = _service.GetEmulator(name);
            if (camera is TcpCameraEmulator cam)
            {
                cam.PauseStreaming();
                //MessageBox.Show($"Камера {name} поставлена на паузу");
            }
        }

        private void Resume_Click(object sender, RoutedEventArgs e)
        {
            string name = ((FrameworkElement)sender).Tag.ToString();
            var camera = _service.GetEmulator(name);
            if (camera is TcpCameraEmulator cam)
            {
                cam.ResumeStreaming();
                //MessageBox.Show($"Камера {name} продолжила передачу");
            }
        }

        private void Stop_Click(object sender, RoutedEventArgs e)
        {
            string name = ((FrameworkElement)sender).Tag.ToString();
            var camera = _service.GetEmulator(name);
            if (camera is TcpCameraEmulator cam)
            {
                if (handlersDictionary.TryRemove(name, out var handlerToRemove))
                {
                    cam.SendNotification -= handlerToRemove;
                }
                var cameraViewModel = ActiveCamerasPanel.ItemsSource?
                .OfType<EmulatorViewModel>()
                .FirstOrDefault(vm => vm.Name == name);
                cameraViewModel.SendLines = "0";
                cameraViewModel.ProgressValue= 0;
                cam.StopStreaming();
                //MessageBox.Show($"Камера {name} остановила передачу");
            }
        }
        private void DeleteTask_Click(object sender, RoutedEventArgs e)
        {
            Stop_Click(sender, e);
            string name = ((FrameworkElement)sender).Tag.ToString();
            var camera = _service.GetEmulator(name);
            if (camera is TcpCameraEmulator cam)
            {
                cam.DropTask();
                LoadActiveCameras();
            }
        }
    }
}
