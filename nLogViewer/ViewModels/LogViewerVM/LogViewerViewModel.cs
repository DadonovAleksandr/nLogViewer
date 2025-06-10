using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Windows.Data;
using System.Windows.Input;
using Microsoft.Extensions.DependencyInjection;
using nLogViewer.Infrastructure.Collections;
using nLogViewer.Infrastructure.Commands;
using nLogViewer.Infrastructure.Configuration;
using nLogViewer.Services.Filter;
using nLogViewer.Services.LogViewer;
using nLogViewer.ViewModels.Base;

namespace nLogViewer.ViewModels.LogViewerVM;

internal class LogViewerViewModel : BaseViewModel
{
    private readonly ILogViewer _viewer;
    private readonly MemoryConfiguration _memoryConfig;
    private ILogEntryFilter? _filter;
    private VirtualizingLogCollection _virtualLogEntries;
    private readonly CollectionViewSource _filtredLogEntries = new();
    
    public ICollectionView FiltredLogEntries => _filtredLogEntries.View;
    public LogEntryView SelectedEntry { get; set; }
    public int SelectedIndex { get; set; }

    public LogViewerViewModel(ILogViewer viewer, MemoryConfiguration memoryConfig)
    {
        _log.Debug($"Вызов конструктора {GetType().Name}");
         
        _viewer = viewer;
        _memoryConfig = memoryConfig;
        _viewer.EntriesChanged += ViewerEntriesRefresh;
        _viewer.Start();
        
        _filter = App.Host.Services.GetService<ILogEntryFilter>();
        _filter.RefreshFilter += OnFilterRefresh;
        
        // Инициализируем виртуализированную коллекцию
        if (_memoryConfig?.EnableDataVirtualization == true)
        {
            _virtualLogEntries = new VirtualizingLogCollection(
                (startIndex, count) => 
                {
                    var entries = _viewer.GetEntriesRange(startIndex, count);
                    return entries;
                },
                _viewer.Count,
                _memoryConfig);
            _filtredLogEntries.Source = _virtualLogEntries;
        }
        else
        {
            _filtredLogEntries.Source = new List<LogEntryView>();
        }
        
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

    private void OnFilterRefresh()
    {
        _log.Debug("Обновление фильтра событий");
        FiltredLogEntriesRefresh();
    }
    
    private void FiltredLogEntriesRefresh() => 
        _filtredLogEntries.Dispatcher.BeginInvoke(new Action(() => _filtredLogEntries.View.Refresh()));

    private void ViewerEntriesRefresh() => 
        _filtredLogEntries.Dispatcher.BeginInvoke(new Action(ViewerEntriesChange));

    private void ViewerEntriesChange()
    {
        if (_memoryConfig?.EnableDataVirtualization == true)
        {
            // Для виртуализации просто обновляем общее количество
            if (_virtualLogEntries != null && _filtredLogEntries.Source != null)
            {
                _virtualLogEntries.UpdateTotalCount(_viewer.Count);
                _log.Debug($"Обновлено общее количество записей: {_viewer.Count}");
            }
        }
        else
        {
            // Для обычного режима работаем как раньше
            var logEntries = _filtredLogEntries.Source as List<LogEntryView>;
            if (logEntries != null)
            {
                int newEntriesCount = _viewer.Count - logEntries.Count;
                _log.Debug($"Получено уведомление о новых событиях ({newEntriesCount})");
                var newEntries = _viewer.GetEntries(newEntriesCount);
                foreach (var entry in newEntries)
                {
                    logEntries.Insert(0, new LogEntryView(entry));
                }
            }
        }
        // Для виртуализированной коллекции не нужно обновлять View,
        // так как она сама уведомляет об изменениях
        if (_memoryConfig?.EnableDataVirtualization != true)
        {
            FiltredLogEntriesRefresh();
        }
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
        _viewer.Clear();
        
        if (_memoryConfig?.EnableDataVirtualization == true)
        {
            _virtualLogEntries?.InvalidateCache();
        }
        else
        {
            var logEntries = _filtredLogEntries.Source as List<LogEntryView>;
            logEntries?.Clear();
        }
        
        _filtredLogEntries.View.Refresh();
    }
    private bool CanClearCommandExecute(object p) => _viewer.Count > 0;
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
    private bool CanPauseCommandExecute(object p) => _viewer.Count > 0;
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

    public void Dispose()
    {
        _log.Debug($"Освобождение ресурсов LogViewerViewModel");
        _viewer.EntriesChanged -= ViewerEntriesRefresh;
        if (_filter != null)
        {
            _filter.RefreshFilter -= OnFilterRefresh;
        }
        _filtredLogEntries.Filter -= LogEntriesFilter;
        _viewer.Dispose();
    }
}