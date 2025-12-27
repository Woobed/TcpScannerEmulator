using ScannerEmulator2._0.Abstractions;
using ScannerEmulator2._0.Reactive;
using System.Reflection;
using System.Windows.Input;

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
                if (vmPropInfo != null && vmPropInfo.CanWrite && vmPropInfo.CanRead)
                {
                    // значения реактивных свойств (в этом случае сами объекты ReactiveProperty)
                    var modelReactive = modelPropInfo.GetValue(model);
                    var vmReactive = vmPropInfo.GetValue(vm);

                    if (modelReactive != null && vmReactive != null)
                    {
                        // получаем тип T из Action
                        var valueType = modelPropInfo.PropertyType.GetGenericArguments()[0];

                        var updateModel = CreateDelegate(modelReactive, valueType);
                        var updateVM = CreateDelegate(vmReactive, valueType);

                        if (updateModel != null && updateVM != null)
                        {
                            BindTwoWay(modelReactive, vmReactive, updateModel, updateVM);

                            SyncValues(modelReactive, vmReactive);
                        }
                    }
                }
            }
        }
        private static void SyncValues(object source, object target)
        {
            // достаю значение из модели и присваиваю значение Vm
            var sourceValue = source.GetType().GetProperty("Value")?.GetValue(source);
            var targetValueProp = target.GetType().GetProperty("Value");

            if (sourceValue != null && targetValueProp != null)
            {
                targetValueProp.SetValue(target, sourceValue);
            }
        }
        private static void BindTwoWay(
            object source, object target,
            Delegate sourceToTarget, Delegate targetToSource)
        {
            var modelProp = source.GetType()
                .GetProperty("OnPropertyChanged", BindingFlags.Public | BindingFlags.Instance);
            var vmProp = target.GetType()
                .GetProperty("OnPropertyChanged", BindingFlags.Public | BindingFlags.Instance);

            // значения OnPropertyChanged внутри самих ReactiveProperty (делегаты)
            var modelAction = modelProp?.GetValue(source) as Delegate;
            var vmAction = vmProp?.GetValue(target) as Delegate;


            var sourceNewAction = modelAction != null ? Delegate.Combine(modelAction, targetToSource) : targetToSource;
            modelProp?.SetValue(source, sourceNewAction);

            var targetNewAction = vmAction != null ? Delegate.Combine(vmAction, sourceToTarget) : sourceToTarget;
            vmProp?.SetValue(target, targetNewAction);

        }

        private static Delegate? CreateDelegate(object sourceProp, Type? genericType)
        {
            var valueProp = sourceProp.GetType().GetProperty("Value");
            if (valueProp == null || genericType == null) return null;

            var setMethod = valueProp.GetSetMethod();
            if (setMethod == null) return null;
            
            var actionType = typeof(Action<>).MakeGenericType(genericType);
            return Delegate.CreateDelegate(actionType, sourceProp, setMethod);
        }
    }
}
