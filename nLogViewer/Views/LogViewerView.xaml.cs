using System.Windows.Controls;

namespace nLogViewer.Views;

public partial class LogViewerView : UserControl
{
    public LogViewerView()
    {
        InitializeComponent();
    }
    
    public void Dispose()
    {
        if (DataContext is ViewModels.LogViewerVM.LogViewerViewModel viewModel)
        {
            viewModel.Dispose();
        }
    }
}