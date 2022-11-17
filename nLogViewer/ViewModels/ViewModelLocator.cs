using Microsoft.Extensions.DependencyInjection;

namespace nLogViewer.ViewModels;

internal class ViewModelLocator
{
    public MainWindowViewModel MainWindowViewModel => App.Host.Services.GetRequiredService<MainWindowViewModel>();
}