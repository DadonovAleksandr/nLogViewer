using Microsoft.Extensions.DependencyInjection;
using nLogViewer.Infrastructure.Configuration;
using nLogViewer.Services.Filter;
using nLogViewer.Services.LogReader.Factory;
using nLogViewer.Services.LogReader.FileLogReader;
using nLogViewer.Services.LogReader.Repository;
using nLogViewer.Services.LogViewer;
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
            EnableDataVirtualization = false, // Временно отключаем для отладки
            VirtualizationPageSize = 100,
            MaxCachedPages = 10,
            UseCircularBuffer = false // Временно отключаем для отладки
        });
        
        services.AddSingleton<ILogEntryFilter, LogEntryFilter>();

        // Регистрация репозиториев
        services.AddSingleton<ILogRepositoryFactory, LogRepositoryFactory>();
        
        services.AddTransient<ILogReaderFactory, FileLogReaderFactory>();

        services.AddTransient<ILogViewer, LogViewer.LogViewer>();

        services.AddTransient<IUserDialogService, WindowsUserDialogService>();
        return services;
    }
}