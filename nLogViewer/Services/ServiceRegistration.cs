using Microsoft.Extensions.DependencyInjection;
using nLogViewer.Services.Filter;
using nLogViewer.Services.UserDialogService;

namespace nLogViewer.Services;

public static class ServiceRegistration
{
    public static IServiceCollection RegisterServices(this IServiceCollection services)
    {
        services.AddSingleton<ILogEntryFilter, LogEntryFilter>();

        services.AddTransient<IUserDialogService, WindowsUserDialogService>();
        return services;
    }
}