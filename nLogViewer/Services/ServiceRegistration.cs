using Microsoft.Extensions.DependencyInjection;
using nLogViewer.Services.Filter;

namespace nLogViewer.Services;

public static class ServiceRegistration
{
    public static IServiceCollection RegisterServices(this IServiceCollection services)
    {
        services.AddSingleton<ILogEntryFilter, LogEntryFilter>();
        return services;
    }
}