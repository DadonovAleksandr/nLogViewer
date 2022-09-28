namespace nLogViewer.Model.Filter;

public class LogEntryFilter : ILogEntryFilter
{
    public bool EnableTraceEvent;
    public bool EnableDebugEvent;
    public bool EnableInfoEvent;
    public bool EnableWarnEvent;
    public bool EnableErrorEvent;
    public bool EnableFatalEvent;
    
    public LogEntryFilter()
    {
        EnableTraceEvent = true;
        EnableDebugEvent = true;
        EnableInfoEvent = true;
        EnableWarnEvent = true;
        EnableErrorEvent = true;
        EnableFatalEvent = true;
    }

    public bool CheckFilter(ILogEntry entry)
    {
        var typeFilter = entry.EntryType switch
        {
            LogEntryType.Trace => EnableTraceEvent,
            LogEntryType.Debug => EnableDebugEvent,
            LogEntryType.Info => EnableInfoEvent,
            LogEntryType.Warn => EnableWarnEvent,
            LogEntryType.Error => EnableErrorEvent,
            LogEntryType.Fatal => EnableFatalEvent,
            _ => false
        };
        
        return typeFilter;
    }
}