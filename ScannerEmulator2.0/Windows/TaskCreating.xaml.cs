using ScannerEmulator2._0.Abstractions;
using ScannerEmulator2._0.Factories;
using ScannerEmulator2._0.Services;
using ScannerEmulator2._0.TCPScanner;
using ScannerEmulator2._0.ViewModels;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Shapes;

namespace ScannerEmulator2._0.Windows
{
    /// <summary>
    /// Логика взаимодействия для TaskCreating.xaml
    /// </summary>
    public partial class TaskCreating : UserControl
    {
        private readonly CamerasHanlderService _service;
        private readonly EmulatorFactory _factory;
        private ListEmulatorViewModel _viewModel;

        private string? _selectedFilePath;

        public TaskCreating()
        {
            InitializeComponent();
            _service = new CamerasHanlderService();
            _factory = new EmulatorFactory(_service);
            _viewModel = new(_service, _factory);
            LoadFiles();
            RefreshCamerasList();
        }

        // === Загрузка файлов из папки Files ===
        private void LoadFiles()
        {
            string folderPath = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Files");
            if (!Directory.Exists(folderPath))
                Directory.CreateDirectory(folderPath);

            var files = Directory.GetFiles(folderPath, "*.txt")
                                 .Select(System.IO.Path.GetFileName)
                                 .ToList();

            FilesListBox.ItemsSource = files;
        }

        // === При выборе файла ===
        private void SelectFile_Click(object sender, RoutedEventArgs e)
        {
            if (FilesListBox.SelectedItem == null)
            {
                MessageBox.Show("Выберите файл из списка.");
                return;
            }

            string fileName = FilesListBox.SelectedItem.ToString()!;
            string fullPath = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Files", fileName);

            if (!File.Exists(fullPath))
            {
                MessageBox.Show("Файл не найден.");
                return;
            }

            _selectedFilePath = fullPath;
            FileContentTextBox.Text = File.ReadAllText(fullPath);
            SelectedFileLabel.Text = $"Выбран файл: {fileName}";
        }

        // === Создание камеры ===
        private async void CreateCamera_Click(object sender, RoutedEventArgs e)
        {
            if (!int.TryParse(PortTextBox.Text, out int port))
            {
                MessageBox.Show("Некорректный порт");
                return;
            }

            string ip = IpTextBox.Text.Trim();

            _viewModel.CreateEmulator(ip, port);

            RefreshCamerasList();
        }

        // === Обновление списка камер ===
        private void RefreshCamerasList()
        {
            CamerasListBox.ItemsSource = null;
            var cameras = _service.GetEmulatorList();

            CamerasListBox.ItemsSource = cameras;
        }

        // === Назначить файл камере ===
        private void AssignFile_Click(object sender, RoutedEventArgs e)
        {
            if (_selectedFilePath == null)
            {
                MessageBox.Show("Сначала выберите файл.");
                return;
            }

            var button = (FrameworkElement)sender;
            string name = button.Tag.ToString()!;
            _viewModel.AssignFile(name, _selectedFilePath);
            MessageBox.Show($"Файл назначен камере {name}");
        }

        // === Запуск трансляции ===
        private void StartCamera_Click(object sender, RoutedEventArgs e)
        {
            var button = (FrameworkElement)sender;
            string name = button.Tag.ToString()!;
            var camera = (TcpCameraEmulator)_service.GetEmulator(name);

            if (_viewModel.StartStreaming(name, 500).Result)
            {
                MessageBox.Show($"Трансляция камеры {name} запущена");
            }
            else
            {
                MessageBox.Show($"Файл для отправки не был назначен");
            }
        }

        // === Удаление камеры ===
        private void DeleteCamera_Click(object sender, RoutedEventArgs e)
        {
            var button = (FrameworkElement)sender;
            string name = button.Tag.ToString()!;
            _service.RemoveEmulator(name);
            RefreshCamerasList();
        }
    }
}

