using System.Linq;
using System.Windows;
using nLogViewer.Views;

namespace nLogViewer.Services.UserDialogService;

internal class WindowsUserDialogService : IUserDialogService
{
    private static Window ActiveWindow => Application.Current.Windows.OfType<Window>().FirstOrDefault(w => w.IsActive);
    
    public void ShowInformation(string message, string title) => MessageBox
        .Show(message, title, MessageBoxButton.OK, MessageBoxImage.Information);

    public void ShowWarning(string message, string title) => MessageBox
        .Show(message, title, MessageBoxButton.OK, MessageBoxImage.Warning);

    public void ShowError(string message, string title)
    {
        Application.Current.Dispatcher.Invoke(() => 
        {
            var dlg = new ErrorWindow(message, title)
            {
                Owner = ActiveWindow
            };
            dlg.ShowDialog();
        });
    }
    
    public bool Confirm(string message, string title, bool exclamation = false) => MessageBox
        .Show(message, title, MessageBoxButton.YesNo, 
            exclamation ? MessageBoxImage.Exclamation : MessageBoxImage.Question) == MessageBoxResult.Yes;
}