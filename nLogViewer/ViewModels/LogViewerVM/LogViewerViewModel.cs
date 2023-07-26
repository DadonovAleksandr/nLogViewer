using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Windows.Data;
using System.Windows.Input;
using Microsoft.Extensions.DependencyInjection;
using nLogViewer.Infrastructure.Commands;
using nLogViewer.Model;
using nLogViewer.Services.Filter;
using nLogViewer.Services.LogViewer;
using nLogViewer.ViewModels.Base;

namespace nLogViewer.ViewModels.LogViewerVM;

internal class LogViewerViewModel : BaseViewModel
{
    private readonly ILogViewer _viewer;
    private ILogEntryFilter? _filter;
    private List<LogEntryView> _logEntries = new();
    private readonly CollectionViewSource _filtredLogEntries = new();
    
    public ICollectionView FiltredLogEntries => _filtredLogEntries.View;
    public LogEntry SelectedEntry { get; set; }
    public int SelectedIndex { get; set; }

    public LogViewerViewModel(ILogViewer viewer)
    {
        _log.Debug($"Вызов конструктора {GetType().Name}");
         
        _viewer = viewer;
        _viewer.EntriesChanged += ViewerEntriesRefresh;
        _viewer.Start();
        
        _filter = App.Host.Services.GetService<ILogEntryFilter>();
        _filter.RefreshFilter += () =>
        {
            _log.Debug("Обновление фильтра событий");
            FiltredLogEntriesRefresh();
        };
        _filtredLogEntries.Source = _logEntries;
        _filtredLogEntries.Filter += LogEntriesFilter;

        #region commands
        AutoscrollCommand = new LambdaCommand(OnEnableAutoscrollCommandExecuted, CanEnableAutoscrollCommandExecute);
        ClearCommand = new LambdaCommand(OnClearCommandExecuted, CanClearCommandExecute);
        PauseCommand = new LambdaCommand(OnPauseCommandExecuted, CanPauseCommandExecute);
        #endregion
    }
    
    private void LogEntriesFilter(object sender, FilterEventArgs e)
    {
        if(!(e.Item is LogEntryView entry)) return;
        if(_filter.CheckFilter(entry)) return;
        
        e.Accepted = false;
    }

    private void FiltredLogEntriesRefresh() => 
        _filtredLogEntries.Dispatcher.BeginInvoke(new Action(() => _filtredLogEntries.View.Refresh()));

    private void ViewerEntriesRefresh() => 
        _filtredLogEntries.Dispatcher.BeginInvoke(new Action(ViewerEntriesChange));

    private void ViewerEntriesChange()
    {
        int newEntriesCount = _viewer.Count - _logEntries.Count;
        _log.Debug($"Получено уведомление о новых событиях ({newEntriesCount})");
        var newEntries = _viewer.GetEntries(newEntriesCount);
        foreach (var entry in newEntries)
        {
            _logEntries.Insert(0, new LogEntryView(entry));
        }
        FiltredLogEntriesRefresh();
    }
    
    #region Commands
    
    #region Включение автопрокрутки
    public ICommand AutoscrollCommand { get; }
    private void OnEnableAutoscrollCommandExecuted(object p) { }
    private bool CanEnableAutoscrollCommandExecute(object p) => true;
    #endregion
    
    #region Очистка лога
    public ICommand ClearCommand { get; }
    private void OnClearCommandExecuted(object p)
    {
        _log.Debug($"Очистка всех событий");
        _logEntries.Clear();
        _filtredLogEntries.View.Refresh();
    }
    private bool CanClearCommandExecute(object p) => _logEntries.Count > 0;
    #endregion

    #region Пауза отображения лога
    public ICommand PauseCommand { get; }
    private void OnPauseCommandExecuted(object p)
    {
        if (EnabledPause)
        {
            _log.Debug($"Команда: Отключить паузу");
            _viewer.Start();
        }
        else
        {
            _log.Debug($"Команда: Включить паузу");
            _viewer.Pause();
        }
    }
    private bool CanPauseCommandExecute(object p) => _logEntries.Count > 0;
    #endregion

    #endregion

    #region Toolbar
    
    private bool _autoscroll;
    /// <summary>
    /// Включение автопрокрутки
    /// </summary>
    public bool EnabledAutoscroll
    {
        get => _autoscroll;
        set => Set(ref _autoscroll, value);
    }
    
    private bool _pause;

    /// <summary>
    /// Включение паузы
    /// </summary>
    public bool EnabledPause => _viewer.State == LogViewerState.Pause;

    // {
    //     get => _pause;
    //     set => Set(ref _pause, value);
    // }

    #endregion

}