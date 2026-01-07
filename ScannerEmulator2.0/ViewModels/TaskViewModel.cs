using ScannerEmulator2._0.Abstractions;
using ScannerEmulator2._0.Enums;
using ScannerEmulator2._0.Reactive;
using System.ComponentModel;

namespace ScannerEmulator2._0.ViewModels
{
    public class TaskViewModel : IViewModel, INotifyPropertyChanged
    {
        public ReactiveProperty<Guid> Id { get; } = new(Guid.Empty);
        public ReactiveProperty<TaskState> State { get; } = new(TaskState.Created);
        public ReactiveProperty<int> Progress { get; } = new(0);
        public ReactiveProperty<string> Name { get; } = new(string.Empty);

        public ReactiveProperty<bool> CanStart { get; } = new(false);
        public ReactiveProperty<bool> CanPause { get; } = new(false);
        public ReactiveProperty<bool> CanResume { get; } = new(false);
        public ReactiveProperty<bool> CanStop { get; } = new(false);

        public TaskViewModel()
        {
            UpdateButtonStates();

            State.PropertyChanged += (s, e) =>
            {
                UpdateButtonStates();
            };
        }

        private void UpdateButtonStates()
        {
            CanStart.Value = State.Value == TaskState.Created || State.Value == TaskState.Stopped;
            CanPause.Value = State.Value == TaskState.Running;
            CanResume.Value = State.Value == TaskState.Paused;
            CanStop.Value = State.Value == TaskState.Running || State.Value == TaskState.Paused;
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        protected virtual void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}