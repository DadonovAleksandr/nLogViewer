using System;

namespace nLogViewer.Model;

public class LogEntry : ILogEntry
{
    public DateTime EventDateTime { get; }
    public LogEntryType EntryType { get; }
    public string Message { get; }
    public string Source { get; }
    public int ProcessId { get; }
    public int ThreadId { get; }

    public LogEntry(DateTime eventDateTime, LogEntryType entryType, string message, string source, int processId = 0, int threadId = 0)
    {
        EventDateTime = eventDateTime;
        EntryType = entryType;
        Message = message;
        Source = source;
        ProcessId = processId;
        ThreadId = threadId;
    }
}