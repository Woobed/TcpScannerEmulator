using Microsoft.Extensions.DependencyInjection;
using ScannerEmulator2._0.Services;
using ScannerEmulator2._0.ViewModels;
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

            // Получаем сервис
            _tasks = App.AppHost.Services.GetRequiredService<TaskHandlerService>();

            // Привязываем ItemsSource к VM
            this.DataContext = new TaskWorkingViewModel(_tasks);

            // Подписка на обновление списка задач
            _tasks.ListInfoChanged += () =>
            {
                if (this.DataContext is TaskWorkingViewModel vm)
                {
                    vm.Refresh();
                }
            };
        }

        private void Start_Click(object sender, RoutedEventArgs e)
        {
            if (sender is FrameworkElement fe && fe.Tag is Guid id)
            {
                _tasks.Start(id);
            }
        }

        private void Pause_Click(object sender, RoutedEventArgs e)
        {
            if (sender is FrameworkElement fe && fe.Tag is Guid id)
            {
                _tasks.Pause(id);
            }
        }

        private void Resume_Click(object sender, RoutedEventArgs e)
        {
            if (sender is FrameworkElement fe && fe.Tag is Guid id)
            {
                _tasks.Resume(id);
            }
        }

        private void Stop_Click(object sender, RoutedEventArgs e)
        {
            if (sender is FrameworkElement fe && fe.Tag is Guid id)
            {
                _tasks.Stop(id);
            }
        }
    }

    /// <summary>
    /// VM для страницы TaskWorking
    /// </summary>
    public class TaskWorkingViewModel
    {
        private readonly TaskHandlerService _service;
        public List<TaskViewModel> TaskList { get; set; }

        public TaskWorkingViewModel(TaskHandlerService service)
        {
            _service = service;
            TaskList = _service.GetTaskList();
        }

        public void Refresh()
        {
            TaskList = _service.GetTaskList();
            // Перепривязываем ItemsSource
            Application.Current.Dispatcher.Invoke(() =>
            {
                // Через DataContext
                // В XAML ItemsSource привязан к TaskList
            });
        }
    }
}
