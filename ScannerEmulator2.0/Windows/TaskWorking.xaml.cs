using Microsoft.Extensions.DependencyInjection;
using ScannerEmulator2._0.Services;
using ScannerEmulator2._0.TCPScanner;
using ScannerEmulator2._0.ViewModels;
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
                    // Получаем настройки из ViewModel
                    var settings = cameraViewModel.GetTaskSettings();

                    cam.StartStreaming(settings);
                }
            }
        }

        private void Pause_Click(object sender, RoutedEventArgs e)
        {
            string name = ((FrameworkElement)sender).Tag.ToString();
            var camera = _service.GetEmulator(name);
            if (camera is TcpCameraEmulator cam)
            {
                cam.PauseStreaming();
                MessageBox.Show($"Камера {name} поставлена на паузу");
            }
        }

        private void Resume_Click(object sender, RoutedEventArgs e)
        {
            string name = ((FrameworkElement)sender).Tag.ToString();
            var camera = _service.GetEmulator(name);
            if (camera is TcpCameraEmulator cam)
            {
                cam.ResumeStreaming();
                MessageBox.Show($"Камера {name} продолжила передачу");
            }
        }

        private void Stop_Click(object sender, RoutedEventArgs e)
        {
            string name = ((FrameworkElement)sender).Tag.ToString();
            var camera = _service.GetEmulator(name);
            if (camera is TcpCameraEmulator cam)
            {
                cam.StopStreaming();
                MessageBox.Show($"Камера {name} остановила передачу");
            }
        }
    }
}
