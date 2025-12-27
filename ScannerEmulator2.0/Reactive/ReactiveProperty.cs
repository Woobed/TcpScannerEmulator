namespace ScannerEmulator2._0.Reactive
{
    public class ReactiveProperty<T>
    {
        public Action<T?>? OnPropertyChanged { get; set; }
        private T? _value { get; set; }
        public T? Value
        {
            get
            {
                return _value;
            }
            set
            {
                _value = value;
                OnPropertyChanged?.Invoke(_value);
            }
        }
    }
}
