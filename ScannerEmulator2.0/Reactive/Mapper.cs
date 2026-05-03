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
                        SyncInitialValue(modelReactive, vmReactive);

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
            PropertyChangedEventHandler sourceHandler = null;
            PropertyChangedEventHandler targetHandler = null;

            sourceHandler = (s, e) =>
            {
                if (e.PropertyName == "Value")
                {
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
                            targetAsNotify.PropertyChanged += targetHandler;
                        }
                    }
                }
            };

            targetHandler = (s, e) =>
            {
                if (e.PropertyName == "Value")
                {
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
                            sourceAsNotify.PropertyChanged += sourceHandler;
                        }
                    }
                }
            };

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