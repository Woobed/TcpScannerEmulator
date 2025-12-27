namespace ScannerEmulator2._0.ReactiveProperty
{
    public class ReactiveProperty<T>
    {
        public Action<T?>? OnProperyChanged { get; set; }
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
                OnProperyChanged?.Invoke(_value);
            }
        }
    }
}
