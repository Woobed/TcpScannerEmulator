using ScannerEmulator2._0.Abstractions;
using ScannerEmulator2._0.Reactive;
using System.ComponentModel;
using System.Reflection;

namespace ScannerEmulator2._0.Services
{
    static public class Mapper
    {
        public static void Map(IModel model, IViewModel vm)
        {
            var modelReactiveProps = model.GetType()
                .GetProperties(BindingFlags.Public | BindingFlags.Instance)
                .Where(p => p.PropertyType.IsGenericType &&
                    p.PropertyType.GetGenericTypeDefinition() == typeof(ReactiveProperty<>));

            var VMReactiveProps = vm.GetType()
                .GetProperties(BindingFlags.Public | BindingFlags.Instance)
                .Where(p => p.PropertyType.IsGenericType &&
                    p.PropertyType.GetGenericTypeDefinition() == typeof(ReactiveProperty<>));

            foreach (var modelPropInfo in modelReactiveProps)
            {
                var vmPropInfo = VMReactiveProps.FirstOrDefault(p => p.Name == modelPropInfo.Name);
                if (vmPropInfo != null && vmPropInfo.CanRead)
                {
                    var modelReactive = modelPropInfo.GetValue(model);
                    var vmReactive = vmPropInfo.GetValue(vm);

                    if (modelReactive != null && vmReactive != null)
                    {
                        // Синхронизируем начальные значения
                        SyncInitialValue(modelReactive, vmReactive);

                        // Создаем двустороннюю привязку
                        BindTwoWay(modelReactive, vmReactive);
                    }
                }
            }
        }

        private static void SyncInitialValue(object source, object target)
        {
            var sourceValue = GetReactivePropertyValue(source);
            if (sourceValue != null)
            {
                SetReactivePropertyValue(target, sourceValue);
            }
        }

        private static void BindTwoWay(object source, object target)
        {
            // Создаем обработчики событий
            PropertyChangedEventHandler sourceHandler = null;
            PropertyChangedEventHandler targetHandler = null;

            // Обработчик для изменений в source
            sourceHandler = (s, e) =>
            {
                if (e.PropertyName == "Value")
                {
                    // Временно отписываемся от target, чтобы избежать цикла
                    var targetAsNotify = target as INotifyPropertyChanged;
                    if (targetAsNotify != null && targetHandler != null)
                    {
                        targetAsNotify.PropertyChanged -= targetHandler;

                        try
                        {
                            var sourceValue = GetReactivePropertyValue(source);
                            SetReactivePropertyValue(target, sourceValue);
                        }
                        finally
                        {
                            // Снова подписываемся
                            targetAsNotify.PropertyChanged += targetHandler;
                        }
                    }
                }
            };

            // Обработчик для изменений в target
            targetHandler = (s, e) =>
            {
                if (e.PropertyName == "Value")
                {
                    // Временно отписываемся от source, чтобы избежать цикла
                    var sourceAsNotify = source as INotifyPropertyChanged;
                    if (sourceAsNotify != null && sourceHandler != null)
                    {
                        sourceAsNotify.PropertyChanged -= sourceHandler;

                        try
                        {
                            var targetValue = GetReactivePropertyValue(target);
                            SetReactivePropertyValue(source, targetValue);
                        }
                        finally
                        {
                            // Снова подписываемся
                            sourceAsNotify.PropertyChanged += sourceHandler;
                        }
                    }
                }
            };

            // Подписываемся на события
            var sourceAsNotify = source as INotifyPropertyChanged;
            var targetAsNotify = target as INotifyPropertyChanged;

            if (sourceAsNotify != null)
            {
                sourceAsNotify.PropertyChanged += sourceHandler;
            }

            if (targetAsNotify != null)
            {
                targetAsNotify.PropertyChanged += targetHandler;
            }
        }

        private static object GetReactivePropertyValue(object reactiveProperty)
        {
            return reactiveProperty.GetType()
                .GetProperty("Value", BindingFlags.Public | BindingFlags.Instance)
                ?.GetValue(reactiveProperty);
        }

        private static void SetReactivePropertyValue(object reactiveProperty, object value)
        {
            var valueProp = reactiveProperty.GetType()
                .GetProperty("Value", BindingFlags.Public | BindingFlags.Instance);

            if (valueProp != null && value != null)
            {
                // Конвертируем значение к нужному типу, если необходимо
                var targetType = valueProp.PropertyType;
                if (value.GetType() != targetType)
                {
                    value = Convert.ChangeType(value, targetType);
                }

                valueProp.SetValue(reactiveProperty, value);
            }
        }
    }
}