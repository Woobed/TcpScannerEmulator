using Microsoft.Extensions.DependencyInjection;
using ScannerEmulator2._0.Reactive;
using ScannerEmulator2._0.Services;
using ScannerEmulator2._0.ViewModels;
using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Controls;

namespace ScannerEmulator2._0.Windows
{
    public partial class TaskWorking : UserControl
    {
        private readonly TaskHandlerService _tasks;
        private readonly LoggerService _logger;

        public TaskWorking()
        {
            InitializeComponent();

            _tasks = App.AppHost.Services.GetRequiredService<TaskHandlerService>();
            _logger = App.AppHost.Services.GetRequiredService<LoggerService>();

            UpdateTasksList();

            Application.Current.Dispatcher.Invoke(() =>
            {
                LogsListBox.ItemsSource = _logger.Logs;
            });

            _tasks.ListInfoChanged += OnTasksListChanged;
            _logger.PropertyChanged += OnLoggerPropertyChanged;
        }

        private void OnTasksListChanged()
        {
            UpdateTasksList();
        }

        private void UpdateTasksList()
        {
            Application.Current.Dispatcher.Invoke(() =>
            {
                TasksPanel.ItemsSource = _tasks.GetTaskList();
            });
        }

        private void OnLoggerPropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(LoggerService.Logs))
            {
                // Прокручиваем к последнему логу при добавлении новых
                Application.Current.Dispatcher.Invoke(() =>
                {
                    if (LogsListBox.Items.Count > 0)
                    {
                        LogsListBox.ScrollIntoView(LogsListBox.Items[LogsListBox.Items.Count - 1]);
                    }
                });
            }
        }

        private void Start_Click(object sender, RoutedEventArgs e)
        {
            if (sender is FrameworkElement fe && fe.Tag is ReactiveProperty<Guid> id)
            {
                if (TasksPanel.ItemsSource is ObservableCollection<TaskViewModel> taskList)
                {
                    var taskVm = taskList.FirstOrDefault(vm => vm.Id == id);
                    if (taskVm != default && taskVm.Settings != null)
                    {
                        _tasks.SetSettings(id.Value, taskVm.Settings);
                    }
                }
                _tasks.Start(id.Value);
            }
        }

        private void Pause_Click(object sender, RoutedEventArgs e)
        {
            if (sender is FrameworkElement fe && fe.Tag is ReactiveProperty<Guid> id)
            {
                _tasks.Pause(id.Value);
            }
        }

        private void Resume_Click(object sender, RoutedEventArgs e)
        {
            if (sender is FrameworkElement fe && fe.Tag is ReactiveProperty<Guid> id)
            {
                _tasks.Resume(id.Value);
            }
        }

        private void Stop_Click(object sender, RoutedEventArgs e)
        {
            if (sender is FrameworkElement fe && fe.Tag is ReactiveProperty<Guid> id)
            {
                _tasks.Stop(id.Value);
            }
        }

        private void Remove_Click(object sender, RoutedEventArgs e)
        {
            if (sender is FrameworkElement fe && fe.Tag is ReactiveProperty<Guid> id)
            {
                _tasks.RemoveTask(id.Value);
            }
        }

        private void ClearLogs_Click(object sender, RoutedEventArgs e)
        {
            _logger.Clear();
        }

        private void UserControl_Unloaded(object sender, RoutedEventArgs e)
        {
            _tasks.ListInfoChanged -= OnTasksListChanged;
            _logger.PropertyChanged -= OnLoggerPropertyChanged;
        }
    }
}