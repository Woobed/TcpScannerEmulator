using ScannerEmulator2._0.Dto;
using ScannerEmulator2._0.Enums;
using ScannerEmulator2._0.Reactive;

namespace ScannerEmulator2._0.ViewModels
{
    public class EmulatorTask
    {
        public Guid Id { get; set; }
        private EmulatorViewModel _emulator { get; set; }
        public string Ip { get; set; }
        public int Port { get; set; }
        public List<Button> Buttons { get; set; }
        public string FileName { get; set; }
        public TaskSettings settings { get; set; }

        public EmulatorTask(string ip, int port)
        {
            Id = Guid.NewGuid();
            Ip = ip;
            Port = port;

            Buttons = new List<Button>()
            {
                new Button("Начать", ButtonType.Start, true),
                new Button("Продолжить", ButtonType.Continue),
                new Button("Пауза", ButtonType.Pause, true),
                new Button("Прервать", ButtonType.Stop, true),
            };
        }

    }
}
