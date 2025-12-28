using ScannerEmulator2._0.Enums;

namespace ScannerEmulator2._0.Reactive
{
    public class Button
    {
        public string Name { get; set; }
        public ButtonType Type { get; set; }
        public bool IsVisiable { get; set; }
        public Action Triggered { get; set; }

        public Button(string name, ButtonType type, bool isVisiable = false)
        {
            Name = name;
            Type = type;
            IsVisiable = isVisiable;
        }

        public void Trigger()
        {
            Triggered?.Invoke();
        }
    }
}
