using Microsoft.Extensions.DependencyInjection;
using ScannerEmulator2._0.Services;
using ScannerEmulator2._0.TCPScanner;
using System.Windows;
using System.Windows.Controls;

namespace ScannerEmulator2._0.Windows
{
    public partial class TaskWorking : UserControl
    {
        private readonly CamerasHanlderService _service;

        private readonly Dictionary<string, Action<int, int>> handlersDictionary = new();

        public TaskWorking()
        {
            InitializeComponent();
            _service = App.AppHost.Services.GetRequiredService<CamerasHanlderService>();

            _service.ListInfoChanged += LoadActiveCameras;
        }

        private void LoadActiveCameras()
        {
            var cameras = _service.GetEmulatorList(t => t.IsReady);
            ActiveCamerasPanel.ItemsSource = cameras;
        }


        private void Start_Click(object sender, RoutedEventArgs e)
        {
            string name = (sender as Button)?.Tag as string;
            if (name == null) return;

            var cameraViewModel = ActiveCamerasPanel.ItemsSource?
                .OfType<EmulatorViewModel>()
                .FirstOrDefault(vm => vm.Name == name);

            if (cameraViewModel == null) return;

            var emulator = _service.GetEmulator(name);
            if (emulator is not TcpCameraEmulator cam) return;

            if (handlersDictionary.TryGetValue(name, out var oldHandler))
            {
                cam.SendNotification -= oldHandler;
                handlersDictionary.Remove(name);
            }

            var settings = cameraViewModel.GetTaskSettings();

            Action<int, int> handler = (sent, total) =>
            {
                HandleSend(sent, total, cameraViewModel);
            };

            handlersDictionary[name] = handler;
            cam.SendNotification += handler;

            _ = cam.StartStreaming(settings);
        }


        private void HandleSend(int sent, int total, EmulatorViewModel vm)
        {
            Application.Current.Dispatcher.Invoke(() =>
            {
                vm.SendLines = sent.ToString();
                vm.FinalLines = total.ToString();

                if (total > 0)
                    vm.ProgressValue = (int)((double)sent / total * 100);
                else
                    vm.ProgressValue = 0;
            });
        }

        private void Pause_Click(object sender, RoutedEventArgs e)
        {
            string name = ((FrameworkElement)sender).Tag.ToString();
            if (_service.GetEmulator(name) is TcpCameraEmulator cam)
                cam.PauseStreaming();
        }

        private void Resume_Click(object sender, RoutedEventArgs e)
        {
            string name = ((FrameworkElement)sender).Tag.ToString();
            if (_service.GetEmulator(name) is TcpCameraEmulator cam)
                cam.ResumeStreaming();
        }

        private void Stop_Click(object sender, RoutedEventArgs e)
        {
            string name = ((FrameworkElement)sender).Tag.ToString();

            var emulator = _service.GetEmulator(name);
            if (emulator is not TcpCameraEmulator cam) return;

            // Безопасная отписка
            if (handlersDictionary.TryGetValue(name, out var handler))
            {
                cam.SendNotification -= handler;
                handlersDictionary.Remove(name);
            }

            cam.StopStreaming();

            // Обнуляем интерфейс
            var vm = ActiveCamerasPanel.ItemsSource?
                .OfType<EmulatorViewModel>()
                .FirstOrDefault(v => v.Name == name);

            if (vm != null)
            {
                vm.SendLines = "0";
                vm.FinalLines = "0";
                vm.ProgressValue = 0;
            }
        }

        private void DeleteTask_Click(object sender, RoutedEventArgs e)
        {
            Stop_Click(sender, e);

            string name = ((FrameworkElement)sender).Tag.ToString();
            if (_service.GetEmulator(name) is TcpCameraEmulator cam)
            {
                cam.DropTask();
                LoadActiveCameras();
            }
        }
    }
}
