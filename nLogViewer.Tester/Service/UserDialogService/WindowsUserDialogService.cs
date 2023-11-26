using nLogViewer.Tester.Views;
using System;
using System.Linq;
using System.Threading;
using System.Windows;

namespace nLogViewer.Tester.Service.UserDialogService
{
    internal class WindowsUserDialogService : IUserDialogService
    {
        private static Window ActiveWindow => Application.Current.Windows
                .OfType<Window>()
                .FirstOrDefault(w => w.IsActive);

        private static Window FocusedWindow => Application.Current.Windows
            .OfType<Window>()
            .FirstOrDefault(w => w.IsFocused);

        public static Window CurrentWindow => FocusedWindow ?? ActiveWindow;

        public void ShowInformation(string message) => MessageBox
            .Show(ActiveWindow, message, "Информация", MessageBoxButton.OK, MessageBoxImage.Information);
        public void ShowWarning(string message) => ShowErrorWindow(message, ErrorWindowType.Warning);
        public void ShowError(string message) => ShowErrorWindow(message, ErrorWindowType.Error);

        private void ShowErrorWindow(string msg, ErrorWindowType type)
        {
            Application.Current.Dispatcher.Invoke(() =>
            {
                var dlg = new ErrorWindow(msg, type)
                {
                    Owner = CurrentWindow
                };
                dlg.ShowDialog();
            });
        }

        public bool Confirm(string message, bool exclamation = false) => MessageBox
            .Show(message, "Запрос пользователю", MessageBoxButton.YesNo,
                exclamation ? MessageBoxImage.Exclamation : MessageBoxImage.Question) == MessageBoxResult.Yes;

        public (IProgress<double> Progress, IProgress<string> Status, CancellationToken Cancel, Action Close) ShowProgress(string title)
        {
            var progressWindow = new ProgressWindow { Title = title, Owner = CurrentWindow, WindowStartupLocation = WindowStartupLocation.CenterOwner };
            progressWindow.Show();
            return (progressWindow.ProgressInformer, progressWindow.StatusInformer, progressWindow.Cancellation, progressWindow.Close);
        }
    }
}