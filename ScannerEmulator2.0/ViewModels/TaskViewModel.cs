using ScannerEmulator2._0.Abstractions;
using ScannerEmulator2._0.Enums;
using ScannerEmulator2._0.Reactive;

namespace ScannerEmulator2._0.ViewModels
{
    public class TaskViewModel : IViewModel
    {
        public ReactiveProperty<Guid> Id { get; } = new(Guid.Empty);
        public ReactiveProperty<TaskState> State { get; } = new(TaskState.Created);
        public ReactiveProperty<int> Progress { get; } = new(0);

        public string Name { get; }

        public ReactiveProperty<bool> CanPause { get; } = new(false);
        public ReactiveProperty<bool> CanResume { get; } = new(false);
        public ReactiveProperty<bool> CanStop { get; } = new(false);

        public TaskViewModel(string name)
        {
            Name = name;
            State.PropertyChanged += (_, __) => UpdateButtons();
            UpdateButtons();
        }

        private void UpdateButtons()
        {
            CanPause.Value = State.Value == TaskState.Running;
            CanResume.Value = State.Value == TaskState.Paused;
            CanStop.Value = State.Value is TaskState.Running or TaskState.Paused;
        }
    }
}
