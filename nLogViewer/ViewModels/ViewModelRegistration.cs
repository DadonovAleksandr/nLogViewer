using Microsoft.Extensions.DependencyInjection;
using nLogViewer.ViewModels.LogViewerVM;

namespace nLogViewer.ViewModels;

public static class ViewModelRegistration
{
    public static IServiceCollection RegisterViewModels(this IServiceCollection services)
    {
        services.AddSingleton<MainWindowViewModel>();
        services.AddTransient<LogViewerViewModel>();
        return services;
    }
}