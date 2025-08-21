using Microsoft.Extensions.DependencyInjection;
using nLogViewer.ViewModels.LogViewerVM;
using nLogViewer.ViewModels.ProgressVM;
using nLogViewer.ViewModels.SettingsVM;

namespace nLogViewer.ViewModels;

public static class ViewModelRegistration
{
    public static IServiceCollection RegisterViewModels(this IServiceCollection services)
    {
        services.AddSingleton<MainWindowViewModel>();
        services.AddTransient<LogViewerViewModel>();
        services.AddTransient<ProgressViewModel>();
        services.AddTransient<SettingsViewModel>();
        return services;
    }
}