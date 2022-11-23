namespace nLogViewer.Model.AppSettings.AppConfig;

public interface IFilterConfig
{
    bool EnableTraceEvent { get; set; }
    bool EnableDebugEvent { get; set; }
    bool EnableInfoEvent { get; set; }
    bool EnableWarnEvent { get; set; }
    bool EnableErrorEvent { get; set; }
    bool EnableFatalEvent { get; set; } 
    
    bool EnableTextSearch { get; set; }
    string TextSearch { get; set; }
}