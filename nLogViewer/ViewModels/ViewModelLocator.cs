using Microsoft.Extensions.DependencyInjection;
using nLogViewer.ViewModels.LogViewerVM;

namespace nLogViewer.ViewModels;

internal class ViewModelLocator
{
    public MainWindowViewModel MainWindowViewModel => App.Host.Services.GetRequiredService<MainWindowViewModel>();
    public LogViewerViewModel LogViewerViewModel => App.Host.Services.GetRequiredService<LogViewerViewModel>();
}