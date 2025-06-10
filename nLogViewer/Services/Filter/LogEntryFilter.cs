using System;
using System.Collections.Generic;
using System.Linq;
using nLogViewer.Model;
using nLogViewer.Model.AppSettings.AppConfig;

namespace nLogViewer.Services.Filter;

internal class LogEntryFilter : FilterBase, ILogEntryFilter
{
    public bool EnableTraceEvent
    {
        get => GetProperty<bool>();
        set => SetProperty(value);
    }
    
    public bool EnableDebugEvent 
    {
        get => GetProperty<bool>();
        set => SetProperty(value);
    }
    
    public bool EnableInfoEvent 
    {
        get => GetProperty<bool>();
        set => SetProperty(value);
    }
    
    public bool EnableWarnEvent 
    {
        get => GetProperty<bool>();
        set => SetProperty(value);
    }
    
    public bool EnableErrorEvent 
    {
        get => GetProperty<bool>();
        set => SetProperty(value);
    }
    
    public bool EnableFatalEvent 
    {
        get => GetProperty<bool>();
        set => SetProperty(value);
    }
    
    public bool EnableTextSearch 
    {
        get => GetProperty<bool>();
        set => SetProperty(value);
    }
    
    public string TextSearch 
    {
        get => GetProperty<string>(string.Empty);
        set => SetProperty(value);
    }
    

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
}