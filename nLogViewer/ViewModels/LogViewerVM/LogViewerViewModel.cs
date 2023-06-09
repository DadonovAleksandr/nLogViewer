using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Threading;
using Microsoft.Extensions.DependencyInjection;
using nLogViewer.Infrastructure.Commands;
using nLogViewer.Model;
using nLogViewer.Services.Filter;
using nLogViewer.Services.LogReader;
using nLogViewer.Services.LogViewer;
using nLogViewer.ViewModels.Base;

namespace nLogViewer.ViewModels.LogViewerVM;

internal class LogViewerViewModel : BaseViewModel
{
    private ILogReader _reader;
    public ILogReader Reader
    {
        get => _reader;
        set => _reader = value;
    }
    
    private static LogViewerState _state;
    private DispatcherTimer _timer;
    private ILogEntryFilter? _filter;
    private List<LogEntryView> _logEntries = new();
    private readonly CollectionViewSource _filtredLogEntries = new();
    
    public ICollectionView FiltredLogEntries => _filtredLogEntries.View;
    public LogEntry SelectedEntry { get; set; }
    public int SelectedIndex { get; set; }

    public LogViewerViewModel()
    {
        _logger.Debug($"Вызов конструктора {GetType().Name}");
         
        _timer = new DispatcherTimer();
        _timer.Tick += new EventHandler(Process);
        _timer.Interval = new TimeSpan(0,0,2);
        _timer.Start();

        _filter = App.Host.Services.GetService<ILogEntryFilter>();
        _filter.RefreshFilter += () =>
        {
            _logger.Debug("Обновление фильтра событий");
            _filtredLogEntries.View.Refresh();
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
    
    private void Process(object sender, EventArgs e)
    {
        _logger.Trace($"Просмотрщик лога в состоянии {_state}");
        if (_reader is null)
        {
            _logger.Warn($"Просмоторщик событий не инициализирован");
            return;
        }
        
        switch (_state)
        {
            case LogViewerState.Stop:
                _logger.Debug($"Переход в состояние {LogViewerState.ReadAllMsg}");
                _state = LogViewerState.ReadAllMsg;    
                break;
            case LogViewerState.ReadAllMsg:
                _logEntries.Clear();
                var data = _reader.GetAll();
                foreach (var entry in data)
                {
                    _logEntries.Insert(0, new LogEntryView(entry));       
                }
                _filtredLogEntries.View.Refresh();
                _logger.Trace($"Считывание всех событий");
                _state = LogViewerState.ReadNewMsg;
                _logger.Debug($"Переход в состояние {LogViewerState.ReadNewMsg}");
                break;
            case LogViewerState.ReadNewMsg:
                var newData = _reader.GetNew();
                foreach (var entry in newData)
                {
                    _logEntries.Insert(0, new LogEntryView(entry));
                }
                if(newData.Any())
                    _filtredLogEntries.View.Refresh();
                _logger.Trace($"Считывание новых событий");
                break;
            case LogViewerState.Pause:
                break;
            default: throw new ArgumentOutOfRangeException();
        }
    }
    
    #region Commands
    
    #region Включение автопрокрутки
    public ICommand AutoscrollCommand { get; }
    private void OnEnableAutoscrollCommandExecuted(object p)
    {
        //EnabledAutoscroll = !EnabledAutoscroll;
    }
    private bool CanEnableAutoscrollCommandExecute(object p) => true;
    #endregion
    
    #region Очистка лога
    public ICommand ClearCommand { get; }
    private void OnClearCommandExecuted(object p)
    {
        _logger.Debug($"Очистка всех событий");
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
            _logger.Debug($"Команда: Включить паузу");
            _logger.Debug($"Переход в состояние {LogViewerState.Pause}");
            _state = LogViewerState.Pause;
        }
        else
        {
            _logger.Debug($"Команда: Отключить паузу");
            _logger.Debug($"Переход в состояние {LogViewerState.ReadNewMsg}");
            _state = LogViewerState.ReadNewMsg;
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
    public bool EnabledPause
    {
        get => _pause;
        set => Set(ref _pause, value);
    }
    
    #endregion

}