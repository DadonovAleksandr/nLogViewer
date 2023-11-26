using Microsoft.Extensions.DependencyInjection;
using nLogViewer.Tester.Service.UserDialogService;

namespace nLogViewer.Tester.Service
{
    public static class ServiceRegistration
    {
        public static IServiceCollection RegisterServices(this IServiceCollection services)
        {
            services.AddTransient<IUserDialogService, WindowsUserDialogService>();
            return services;
        }
    }
}