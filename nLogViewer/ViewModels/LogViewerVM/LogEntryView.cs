using System;
using System.Linq;
using System.Windows.Media;
using nLogViewer.Model;

namespace nLogViewer.ViewModels.LogViewerVM;

internal class LogEntryView : ILogEntry
{
    public DateTime EventDateTime { get; }
    public LogEntryType EntryType { get; }
    public string Message { get; }
    public string Source { get; }
    public int ProcessId { get; }
    public int ThreadId { get; }
    public Brush Background { get; }
    
    public LogEntryView(ILogEntry entry)
    {
        EventDateTime = entry.EventDateTime;
        EntryType = entry.EntryType;
        Message = entry.Message;
        Source = entry.Source.Split(".").Last();
        ProcessId = entry.ProcessId;
        ThreadId = entry.ThreadId;
        
        Background = entry.EntryType switch
        {
            LogEntryType.Trace => new SolidColorBrush(Colors.LightGray),
            LogEntryType.Debug => new SolidColorBrush(Colors.LightBlue),
            LogEntryType.Info => new SolidColorBrush(Colors.White),
            LogEntryType.Warn => new SolidColorBrush(Colors.Yellow),
            LogEntryType.Error => new SolidColorBrush(Colors.Red),
            LogEntryType.Fatal => new SolidColorBrush(Colors.DarkRed),
            _ => throw new ArgumentOutOfRangeException()
        };

    }
}