using Microsoft.Extensions.DependencyInjection;

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