using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using ScannerEmulator2._0.Factories;
using ScannerEmulator2._0.Services;
using System.Configuration;
using System.Data;
using System.Windows;

namespace ScannerEmulator2._0
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        public static IHost AppHost { get; private set; }

        public App()
        {
            AppHost = Host.CreateDefaultBuilder()
                .ConfigureServices((context, services) =>
                {
                    
                    services.AddSingleton<MainWindow>();
                    //services.AddSingleton<TaskCreating>();
                    services.AddSingleton<CamerasHanlderService>();
                    services.AddSingleton<EmulatorFactory>();
                    
                })
                .Build();
        }

        protected override async void OnStartup(StartupEventArgs e)
        {
            await AppHost.StartAsync();

            var mainWindow = AppHost.Services.GetRequiredService<MainWindow>();
            mainWindow.Show();

            base.OnStartup(e);
        }

        protected override async void OnExit(ExitEventArgs e)
        {
            await AppHost.StopAsync();
            AppHost.Dispose();
            base.OnExit(e);
        }
    }
}
