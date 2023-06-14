using System;
using System.Collections.Generic;
using System.Linq;
using nLogViewer.Model;
using nLogViewer.Model.AppSettings.AppConfig;

namespace nLogViewer.Services.Filter;

internal class LogEntryFilter : ILogEntryFilter
{
    public event RefreshFilter? RefreshFilter;

    private bool _enableTraceEvent;
    public bool EnableTraceEvent
    {
        get => _enableTraceEvent;
        set
        {
            if(_enableTraceEvent == value)
                return;
            _enableTraceEvent = value;
            RefreshFilter?.Invoke();
        }
    }
    
    private bool _enableDebugEvent;
    public bool EnableDebugEvent 
    {
        get => _enableDebugEvent;
        set
        {
            if(_enableDebugEvent == value)
                return;
            _enableDebugEvent = value;
            RefreshFilter?.Invoke();
        }
    }
    
    private bool _enableInfoEvent;
    public bool EnableInfoEvent 
    {
        get => _enableInfoEvent;
        set
        {
            if(_enableInfoEvent == value)
                return;
            _enableInfoEvent = value;
            RefreshFilter?.Invoke();
        }
    }
    
    private bool _enableWarnEvent;
    public bool EnableWarnEvent 
    {
        get => _enableWarnEvent;
        set
        {
            if(_enableWarnEvent == value)
                return;
            _enableWarnEvent = value;
            RefreshFilter?.Invoke();
        }
    }
    
    private bool _enableErrorEvent;
    public bool EnableErrorEvent 
    {
        get => _enableErrorEvent;
        set
        {
            if(_enableErrorEvent == value)
                return;
            _enableErrorEvent = value;
            RefreshFilter?.Invoke();
        }
    }
    
    private bool _enableFatalEvent;
    public bool EnableFatalEvent 
    {
        get => _enableFatalEvent;
        set
        {
            if(_enableFatalEvent == value)
                return;
            _enableFatalEvent = value;
            RefreshFilter?.Invoke();
        }
    }
    
    private bool _enableTextSearch;
    public bool EnableTextSearch 
    {
        get => _enableTextSearch;
        set
        {
            if(_enableTextSearch == value)
                return;
            _enableTextSearch = value;
            RefreshFilter?.Invoke();
        }
    }
    
    private string _textSearch;
    public string TextSearch 
    {
        get => _textSearch;
        set
        {
            _textSearch = value;
            RefreshFilter?.Invoke();
        }
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