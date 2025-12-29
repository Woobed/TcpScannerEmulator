using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace ScannerEmulator2._0.Reactive
{
    public class ReactiveProperty<T> : INotifyPropertyChanged
    {
        private T? _value;

        public event PropertyChangedEventHandler? PropertyChanged;

        public ReactiveProperty(T value)
        {
            _value = value;
        }

        public T? Value
        {
            get => _value;
            set
            {
                if (!EqualityComparer<T>.Default.Equals(_value, value))
                {
                    _value = value;
                    OnPropertyChanged(nameof(Value));
                }
            }
        }

        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = "")
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}