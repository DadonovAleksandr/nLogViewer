using System;

namespace nLogViewer.Model;

internal class LogEntry : ILogEntry
{
    public DateTime EventDateTime { get; }
    public LogEntryType EntryType { get; }
    public string Message { get; }
    public string Source { get; }

    public LogEntry(DateTime eventDateTime, LogEntryType entryType, string message, string source)
    {
        EventDateTime = eventDateTime;
        EntryType = entryType;
        Message = message;
        Source = source;
    }

    
}