using System;
using System.Collections.Generic;
using System.Linq;
using nLogViewer.Model.AppSettings.AppConfig;

namespace nLogViewer.Model.Filter;


internal class LogEntryFilter : ILogEntryFilter
{
    public event RefreshFilter? RefreshFilter;
    public bool EnableTraceEvent { get; set; }
    public bool EnableDebugEvent { get; set; }
    public bool EnableInfoEvent { get; set; }
    public bool EnableWarnEvent { get; set; }
    public bool EnableErrorEvent { get; set; }
    public bool EnableFatalEvent { get; set; }
    public bool EnableTextSearch { get; set; }
    public string TextSearch { get; set; }

    public LogEntryFilter()
    {
        var filterConfig = AppConfig.GetConfigFromDefaultPath().FilterConfig;
        #region Проверка, что настройик не "пустые"
        var list = new List<bool>
        {
            filterConfig.EnableTraceEvent,    
            filterConfig.EnableDebugEvent,    
            filterConfig.EnableInfoEvent,    
            filterConfig.EnableWarnEvent,    
            filterConfig.EnableErrorEvent,    
            filterConfig.EnableFatalEvent,    
            filterConfig.EnableTextSearch
        };
        var init = list.All(x => x == false);
        #endregion
        
        //Инициализация фильтра из настроек (с учетом того, что сами настройка могут быть неиницилизированны)  
        EnableTraceEvent = init || filterConfig.EnableTraceEvent;
        EnableDebugEvent = init || filterConfig.EnableDebugEvent;
        EnableInfoEvent = init || filterConfig.EnableInfoEvent;
        EnableWarnEvent = init || filterConfig.EnableWarnEvent;
        EnableErrorEvent = init || filterConfig.EnableErrorEvent;
        EnableFatalEvent = init || filterConfig.EnableFatalEvent;

        EnableTextSearch = init || filterConfig.EnableTextSearch;
        TextSearch = filterConfig.TextSearch ?? String.Empty;
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

        var textFilter = !EnableTextSearch || entry.Message.Contains(TextSearch) || entry.Source.Contains(TextSearch);
        
        return typeFilter && textFilter;
    }

    public void Refresh() => RefreshFilter?.Invoke();
}