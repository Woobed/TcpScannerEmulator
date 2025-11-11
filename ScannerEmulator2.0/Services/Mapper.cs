using ScannerEmulator2._0.Abstractions;
using System.Reflection;

namespace ScannerEmulator2._0.Services
{
    static public class Mapper/*<TModel, TViewmodel> where TModel : class where TViewmodel : class*/
    {
        // простейший mapper
        public static void Map(IModel model, IViewModel vm)
        {
            var modelProps = model.GetType().GetProperties(BindingFlags.Public | BindingFlags.Instance);
            var VMProps = vm.GetType().GetProperties(BindingFlags.Public | BindingFlags.Instance);

            foreach (var prop in modelProps)
            {
                var vmprop = VMProps.FirstOrDefault(t => t.Name == prop.Name);
                if (vmprop != null && vmprop.CanWrite && prop.CanRead)
                {
                    vmprop.SetValue(vm,prop.GetValue(model));
                }
            }
        }
    }
}
