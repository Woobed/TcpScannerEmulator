using ScannerEmulator2._0.ViewModels;

namespace ScannerEmulator2._0.Services
{
    public class TaskHandlerService
    {
        private List<EmulatorTask> Tasks { get; set; } = new();
        public Action? ListInfoChanged { get; set; }

        public EmulatorTask? GetTask(Guid id)
        {
            var instance = Tasks.Where(t => t.Id == id).FirstOrDefault();
            if (instance == null) return null;
            return instance;
        }

        public List<EmulatorTask> GetTaskList(Func<EmulatorTask, bool>? predicate = null)
        {
            return predicate == null ? Tasks : Tasks.Where(Task => predicate(Task)).ToList();
        }

        public void CreateTask(string ip, int port)
        {
            Tasks.Add(new EmulatorTask(ip, port));
            ListInfoChanged?.Invoke();
        }

        public void RemoveTask(Guid id)
        {
            var result = Tasks.RemoveAll(i => i.Id == id);
            if (result != 0)
            {
                ListInfoChanged?.Invoke();
            }
        }
    }
}
