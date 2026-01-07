using Microsoft.Extensions.DependencyInjection;
using ScannerEmulator2._0.Reactive;
using ScannerEmulator2._0.Services;
using System.Windows;
using System.Windows.Controls;

namespace ScannerEmulator2._0.Windows
{
    public partial class TaskWorking : UserControl
    {
        private readonly TaskHandlerService _tasks;

        public TaskWorking()
        {
            InitializeComponent();

            _tasks = App.AppHost.Services.GetRequiredService<TaskHandlerService>();

            this.DataContext = _tasks;

            _tasks.ListInfoChanged += OnTasksListChanged;

            UpdateTasksList();
        }

        private void OnTasksListChanged()
        {
            UpdateTasksList();
        }

        private void UpdateTasksList()
        {
            Application.Current.Dispatcher.Invoke(() =>
            {
                if (TasksPanel != null)
                {
                    TasksPanel.ItemsSource = null;
                    TasksPanel.ItemsSource = _tasks.GetTaskList();
                }
            });
        }

        private void Start_Click(object sender, RoutedEventArgs e)
        {
            if (sender is FrameworkElement fe && fe.Tag is ReactiveProperty<Guid> id)
            {
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
        private void UserControl_Unloaded(object sender, RoutedEventArgs e)
        {
            _tasks.ListInfoChanged -= OnTasksListChanged;
        }
    }
}