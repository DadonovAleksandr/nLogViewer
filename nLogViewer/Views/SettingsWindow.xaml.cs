using System.Windows;
using nLogViewer.ViewModels.SettingsVM;

namespace nLogViewer.Views;

public partial class SettingsWindow : Window
{
    public SettingsWindow(SettingsViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;
        
        // Подписываемся на событие закрытия из ViewModel
        viewModel.CloseRequested += () =>
        {
            DialogResult = viewModel.DialogResult;
            Close();
        };
    }
}