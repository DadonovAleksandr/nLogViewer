using Microsoft.Extensions.DependencyInjection;
using nLogViewer.Infrastructure.Configuration;
using nLogViewer.Model.AppSettings.AppConfig;
using nLogViewer.Services.Filter;
using nLogViewer.Services.LogReader.Factory;
using nLogViewer.Services.LogReader.FileLogReader;
using nLogViewer.Services.LogReader.Repository;
using nLogViewer.Services.LogViewer;
using nLogViewer.Services.Progress;
using nLogViewer.Services.UserDialogService;

namespace nLogViewer.Services;

public static class ServiceRegistration
{
    public static IServiceCollection RegisterServices(this IServiceCollection services)
    {
        // Конфигурация памяти
        services.AddSingleton<MemoryConfiguration>(provider => new MemoryConfiguration
        {
            MaxEntriesInMemory = 100_000,
            EnableDataVirtualization = false,
            VirtualizationPageSize = 100,
            MaxCachedPages = 10,
            UseCircularBuffer = true // Включено для оптимизации памяти (Фаза 1)
        });
        
        services.AddSingleton<ILogEntryFilter, LogEntryFilter>();
        services.AddSingleton<IAppConfig>(provider => AppConfig.GetConfigFromDefaultPath());
        services.AddSingleton<IVirtualizationService, VirtualizationService>();

        // Регистрация репозиториев
        services.AddSingleton<ILogRepositoryFactory, LogRepositoryFactory>();
        
        // Регистрация фабрики LogViewer
        services.AddSingleton<ILogViewerFactory, LogViewerFactory>();
        
        services.AddTransient<ILogReaderFactory, FileLogReaderFactory>();

        services.AddTransient<ILogViewer, LogViewer.LogViewer>();

        services.AddTransient<IUserDialogService, WindowsUserDialogService>();
        
        // Служба прогресса
        services.AddTransient<IProgressReporter, ProgressReporter>();
        services.AddTransient<IWindowProgressService, WindowProgressService>();
        
        return services;
    }
}