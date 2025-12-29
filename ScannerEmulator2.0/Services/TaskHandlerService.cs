using ScannerEmulator2._0.Dto;
using ScannerEmulator2._0.ViewModels;

namespace ScannerEmulator2._0.Services
{
    public class TaskHandlerService
    {
        private List<CameraSendTask> Tasks { get; set; } = new();
        private List<TaskViewModel> vms { get; set; } = new();
        public Action? ListInfoChanged { get; set; }

        public CameraSendTask? GetTask(Guid id)
        {
            var instance = Tasks.Where(t => t.Id.Value == id).FirstOrDefault();
            if (instance == null) return null;
            return instance;
        }

        //public List<TaskViewModel> GetTaskList(Func<TaskViewModel, bool>? predicate = null)
        //{
        //    return predicate == null ? Tasks : Tasks.Where(Task => predicate(Task)).ToList();
        //}
        public List<TaskViewModel> GetTaskList()
        {
            return vms;
        }

        public void CreateTask(string path, string ip, int port)
        {
            var task = new CameraSendTask(path);
            var vm = new TaskViewModel(ip, port);

            Mapper.Map(task, vm);
            
            Tasks.Add(task);
            vms.Add(vm);
            
            ListInfoChanged?.Invoke();
        }

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
