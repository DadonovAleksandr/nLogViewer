using System.Windows;

namespace nLogViewer.Views;

public partial class ErrorWindow : Window
{
    public ErrorWindow()
    {
        InitializeComponent();
    }
    
    public ErrorWindow(string msg, string title)
    {
        InitializeComponent();
        if (string.IsNullOrEmpty(msg))
            return;
        ExceptionDescription.Text = msg;
        Title = title;
    }
}