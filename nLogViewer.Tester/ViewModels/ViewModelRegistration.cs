using Microsoft.Extensions.DependencyInjection;
using nLogViewer.Tester.ViewModels.MainWindowVm;

namespace nLogViewer.Tester.ViewModels
{
    public static class ViewModelRegistration
    {
        public static IServiceCollection RegisterViewModels(this IServiceCollection services)
        {
            services.AddSingleton<MainWindowViewModel>();
            return services;
        }
    }
}