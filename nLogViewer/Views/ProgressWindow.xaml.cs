using System.Windows;
using nLogViewer.ViewModels.ProgressVM;

namespace nLogViewer.Views;

/// <summary>
/// Логика взаимодействия для ProgressWindow.xaml
/// </summary>
public partial class ProgressWindow : Window
{
    public ProgressWindow()
    {
        InitializeComponent();
    }
    
    public ProgressWindow(ProgressViewModel viewModel) : this()
    {
        DataContext = viewModel;
    }
}