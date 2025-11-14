using Microsoft.Extensions.DependencyInjection;
using ScannerEmulator2._0.Abstractions;
using ScannerEmulator2._0.Factories;
using ScannerEmulator2._0.Services;
using ScannerEmulator2._0.TCPScanner;
using ScannerEmulator2._0.ViewModels;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
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
        private Timer autoSaveTimer;
        private bool isTextChanged = false;

        public TaskCreating()
        {
            InitializeComponent();
            _service = App.AppHost.Services.GetRequiredService<CamerasHanlderService>();
            _factory  = App.AppHost.Services.GetRequiredService<EmulatorFactory>();
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
        }
        private void FilesListBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (FilesListBox.SelectedItem == null) return;

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
        }

        private void DeleteFile_Click(object sender, RoutedEventArgs e)
        {
            if (_selectedFilePath == null) {
                MessageBox.Show("Выберете файл для удаления");
                return;
            }
            File.Delete( _selectedFilePath );
            SelectedFileLabel.Text = $"{System.IO.Path.GetFileName(_selectedFilePath)} удален";
            LoadFiles();
            _selectedFilePath = null;
        }
        private void CreateNewFile_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrEmpty(NewFileName.Text))
            {
                MessageBox.Show("Введите название файла");
                return;
            }
            try
            {
                // Создаем директорию, если она не существует
                string filesDirectory = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Files");
                if (!Directory.Exists(filesDirectory))
                {
                    Directory.CreateDirectory(filesDirectory);
                }

                string filepath = System.IO.Path.Combine(filesDirectory, NewFileName.Text + ".txt");

                // Проверяем, не существует ли уже файл
                if (File.Exists(filepath))
                {
                    MessageBox.Show("Файл с таким именем уже существует");
                    return;
                }

                // Создаем пустой файл и сразу закрываем поток
                using (File.Create(filepath)) { }

                // Записываем пустое содержимое в файл
                File.WriteAllText(filepath, "");

                // Загружаем содержимое (будет пустая строка)
                FileContentTextBox.Text = File.ReadAllText(filepath);
                _selectedFilePath = filepath;
                SelectedFileLabel.Text = $"Выбран файл: {System.IO.Path.GetFileName(filepath)}";
                FileContentTextBox.Text = File.ReadAllText(filepath);
                LoadFiles();
                NewFileName.Text = "";
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при создании файла: {ex.Message}", "Ошибка");
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




        private void FileContentTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (string.IsNullOrEmpty(_selectedFilePath)) return;

            isTextChanged = true;
            AutoSaveStatusText.Text = "Изменения не сохранены...";
            AutoSaveStatusText.Foreground = Brushes.OrangeRed;

            autoSaveTimer?.Dispose();
            autoSaveTimer = new System.Threading.Timer(_ =>
            {
                Dispatcher.Invoke(() => AutoSaveFile());
            }, null, 1500, System.Threading.Timeout.Infinite);
        }

        private void FileContentTextBox_LostFocus(object sender, RoutedEventArgs e)
        {
            // Сохраняем при потере фокуса
            if (isTextChanged)
            {
                AutoSaveFile();
            }
        }

        private void AutoSaveFile()
        {
            if (!isTextChanged || string.IsNullOrEmpty(_selectedFilePath)) return;

            try
            {
                File.WriteAllText(_selectedFilePath, FileContentTextBox.Text);
                isTextChanged = false;

                AutoSaveStatusText.Text = "Автосохранено";
                AutoSaveStatusText.Foreground = Brushes.Green;

                // Через 3 секунды очищаем статус
                var clearTimer = new System.Threading.Timer(_ =>
                {
                    Dispatcher.Invoke(() =>
                    {
                        if (!isTextChanged)
                            AutoSaveStatusText.Text = "";
                    });
                }, null, 3000, System.Threading.Timeout.Infinite);
            }
            catch (Exception ex)
            {
                AutoSaveStatusText.Text = $"Ошибка сохранения: {ex.Message}";
                AutoSaveStatusText.Foreground = Brushes.Red;
            }
        }
    }
}

