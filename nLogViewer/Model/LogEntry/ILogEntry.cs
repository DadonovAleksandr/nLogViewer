using System;

namespace nLogViewer.Model;

internal interface ILogEntry
{
    public DateTime EventDateTime { get; }
    public LogEntryType EntryType { get; }
    public string Message { get; }
    public string Source { get; }
}