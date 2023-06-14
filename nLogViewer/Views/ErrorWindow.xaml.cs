using System.Windows;

namespace nLogViewer.Views;

public partial class ErrorWindow : Window
{
    public ErrorWindow()
    {
        InitializeComponent();
    }
    
    public ErrorWindow(string errMsg)
    {
        InitializeComponent();
        if (string.IsNullOrEmpty(errMsg))
            return;
        ExceptionDescription.Text = errMsg;
    }
}