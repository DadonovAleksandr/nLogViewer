using System;
using System.IO;
using System.Runtime.CompilerServices;
using System.Windows;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using NLog;
using NLog.Extensions.Logging;
using nLogViewer.Model;
using nLogViewer.Model.AppSettings.AppConfig;
using nLogViewer.Services;
using nLogViewer.ViewModels;

namespace nLogViewer
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        private static Logger _log = LogManager.GetCurrentClassLogger();
        public static bool IsDesighnMode { get; private set; } = true;
        
        private static IHost _host;
        public static IHost Host => _host ??= Program.CreateHostBuilder(Environment.GetCommandLineArgs()).Build();
        protected override async void OnStartup(StartupEventArgs e)
        {
            _log.Debug($"Запуск приложения: {AppConst.Get().AppName} {ProjectVersion.Get()}");
            IsDesighnMode = false;
            var host = Host;
            base.OnStartup(e);

            await host.StartAsync().ConfigureAwait(false);
        }                       

        protected override async void OnExit(ExitEventArgs e)
        {
            base.OnExit(e);
            var host = Host;
            await host.StopAsync().ConfigureAwait(false);
            host.Dispose();
            _host = null;
        }

        public static void ConfigureServices(HostBuilderContext host, IServiceCollection services)
        {
            var configuration = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
                .AddJsonFile($"appsettings.{host.HostingEnvironment.EnvironmentName}.json", optional: true, reloadOnChange: true)
                .AddEnvironmentVariables()
                .Build();

            services.AddSingleton<IConfiguration>(configuration);

            LogManager.Configuration = new NLog.Config.XmlLoggingConfiguration("nlog.config");
            var logLevel = configuration.GetValue<string>("Logging:LogLevel:Default") ?? "Info";
            LogManager.Configuration.Variables["logLevel"] = logLevel;

            services
                .RegisterServices()
                .RegisterViewModels()
                .AddLogging(builder =>
                {
                    builder.ClearProviders();
                    builder.AddNLog();
                });
        }

        public static string CurrentDirectory => IsDesighnMode
            ? Path.GetDirectoryName(GetSourceCodePath())
            : Environment.CurrentDirectory;

        private static string GetSourceCodePath([CallerFilePath] string Path = null) => Path;

    }
}