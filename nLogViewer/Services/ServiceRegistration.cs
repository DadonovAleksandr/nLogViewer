using Microsoft.Extensions.DependencyInjection;
using nLogViewer.Services.Filter;
using nLogViewer.Services.LogReader;
using nLogViewer.Services.LogReader.FileLogReader;
using nLogViewer.Services.LogViewer;
using nLogViewer.Services.UserDialogService;

namespace nLogViewer.Services;

public static class ServiceRegistration
{
    public static IServiceCollection RegisterServices(this IServiceCollection services)
    {
        services.AddSingleton<ILogEntryFilter, LogEntryFilter>();

        services.AddSingleton<ILogReaderConfiguration, FileLogReaderConfiguration>();
        services.AddTransient<ILogReader, FileLogReader>();

        services.AddTransient<ILogViewer, LogViewer.LogViewer>();

        services.AddTransient<IUserDialogService, WindowsUserDialogService>();
        return services;
    }
}