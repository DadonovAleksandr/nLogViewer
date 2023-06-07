
namespace nLogViewer.Model.Filter;

public delegate void RefreshFilter();

internal interface ILogEntryFilter
{
    event RefreshFilter RefreshFilter;
    bool EnableTraceEvent { get; set; }
    bool EnableDebugEvent { get; set; }
    bool EnableInfoEvent { get; set; }
    bool EnableWarnEvent { get; set; }
    bool EnableErrorEvent { get;  set; }
    bool EnableFatalEvent { get; set; }

    bool EnableTextSearch { get; set; }
    string TextSearch { get; set; }
    
    bool CheckFilter(ILogEntry entry);
    void Refresh();
}