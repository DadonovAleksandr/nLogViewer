using System;
using System.Windows.Controls;
using System.Windows.Data;
using nLogViewer.Model;
using nLogViewer.ViewModels;

namespace nLogViewer.Views;

public partial class LogViewerView : UserControl
{
    public LogViewerView()
    {
        InitializeComponent();
    }

    private void LogEntryCollection_OnFilter(object sender, FilterEventArgs e)
    {
        if(!(e.Item is LogEntryView entry)) return;
            
        var filterText = LogFilterText.Text;
        if(filterText.Length == 0) return;
        
        if(entry.Message.Contains(filterText, StringComparison.Ordinal)) return;
        if(entry.Source.Contains(filterText, StringComparison.Ordinal)) return;

        e.Accepted = false;
    }

    private void LogFilterText_OnTextChanged(object sender, TextChangedEventArgs e)
    {
        var textBox = (TextBox)sender;
        var collection = (CollectionViewSource)textBox.FindResource("LogEntryCollection");
        collection.View.Refresh();
    }
}