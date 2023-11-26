using Microsoft.Extensions.DependencyInjection;
using nLogViewer.Tester.ViewModels.MainWindowVm;

namespace nLogViewer.Tester.ViewModels
{
    internal class ViewModelLocator
    {
        public MainWindowViewModel MainWindowViewModel => App.Host.Services.GetRequiredService<MainWindowViewModel>();
    }
}