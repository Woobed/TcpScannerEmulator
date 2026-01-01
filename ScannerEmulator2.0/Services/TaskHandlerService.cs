using ScannerEmulator2._0.Dto;
using ScannerEmulator2._0.ViewModels;

namespace ScannerEmulator2._0.Services
{
    public class TaskHandlerService
    {
        private List<CameraSendTask> Tasks { get; set; } = new();
        private List<TaskViewModel> vms { get; set; } = new();
        public Action? ListInfoChanged { get; set; }

        private CameraSendTask? GetTask(Guid id)
        {
            var instance = Tasks.Where(t => t.Id.Value == id).FirstOrDefault();
            if (instance == null) return null;
            return instance;
        }

        public List<TaskViewModel> GetTaskList()
        {
            return vms;
        }

        public void CreateTask(string path, string name)
        {
            var task = new CameraSendTask(path);
            var vm = new TaskViewModel(name);

            Mapper.Map(task, vm);
            
            Tasks.Add(task);
            vms.Add(vm);
            
            ListInfoChanged?.Invoke();
        }
        public void Pause(Guid id) => GetTask(id).Pause();
        public void Start(Guid id) => GetTask(id).Start();
        public void Resume(Guid id) => GetTask(id).Resume();
        public void Stop(Guid id) => GetTask(id).Stop();

        public void RemoveTask(Guid id)
        {
            var result = Tasks.RemoveAll(i => i.Id.Value == id);
            if (result != 0)
            {
                vms.RemoveAll(i => i.Id.Value == id);
                ListInfoChanged?.Invoke();
            }
        }
    }
}
