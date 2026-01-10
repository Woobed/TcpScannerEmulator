using ScannerEmulator2._0.Dto;
using ScannerEmulator2._0.ViewModels;
using System.Collections.ObjectModel;
using System.Threading.Tasks;

namespace ScannerEmulator2._0.Services
{
    public class TaskHandlerService
    {
        private readonly List<CameraSendTask> _tasks = new();
        private readonly ObservableCollection<TaskViewModel> _vms = new();
        private readonly CamerasHandlerService _camerasHandlerService;

        public Action? ListInfoChanged { get; set; }

        public TaskHandlerService(CamerasHandlerService camerasHandler)
        {
            _camerasHandlerService = camerasHandler;
        }

        private CameraSendTask? GetTask(Guid id)
        {
            return _tasks.FirstOrDefault(t => t.Id.Value == id);
        }

        public ObservableCollection<TaskViewModel> GetTaskList()
        {
            return _vms;
        }
        public void SetSettings(Guid id, TaskSettings settings)
        {
            var task = _tasks.FirstOrDefault(t => t.Id.Value == id);
            task?.SetSettings(settings);
        }
        public void CreateTask(string path, string cameraName)
        {
            var camera = _camerasHandlerService.GetEmulator(cameraName);
            if (camera == null)
            {
                throw new InvalidOperationException($"Camera '{cameraName}' not found");
            }

            var task = new CameraSendTask(path);
            task.AssignToEmulator(camera);

            var vm = new TaskViewModel();
            vm.Name.Value = cameraName;
            Mapper.Map(task, vm);

            _tasks.Add(task);
            _vms.Add(vm);

            ListInfoChanged?.Invoke();
        }

        public void Start(Guid id)
        {
            var task = GetTask(id);
            if (task != null)
            {
                task.StartExecution();
            }
        }

        public void Pause(Guid id)
        {
            var task = GetTask(id);
            task?.Pause();
        }

        public void Resume(Guid id)
        {
            var task = GetTask(id);
            task?.Resume();
        }

        public void Stop(Guid id)
        {
            var task = GetTask(id);
            if (task != null)
            {
                task.Stop();
            }
        }

        public void RemoveTask(Guid id)
        {
            var task = GetTask(id);
            if (task != null)
            {
                task.Stop();
                _tasks.Remove(task);

                var vm = _vms.FirstOrDefault(v => v.Id.Value == id);
                if (vm != null)
                {
                    _vms.Remove(vm);
                }

                ListInfoChanged?.Invoke();
            }
        }
    }
}