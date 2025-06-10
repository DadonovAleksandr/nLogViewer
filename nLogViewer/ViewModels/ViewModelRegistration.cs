using Microsoft.Extensions.DependencyInjection;
using nLogViewer.ViewModels.LogViewerVM;
using nLogViewer.ViewModels.ProgressVM;

namespace nLogViewer.ViewModels;

public static class ViewModelRegistration
{
    public static IServiceCollection RegisterViewModels(this IServiceCollection services)
    {
        services.AddSingleton<MainWindowViewModel>();
        services.AddTransient<LogViewerViewModel>();
        services.AddTransient<ProgressViewModel>();
        return services;
    }
}