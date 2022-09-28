﻿using System;
using System.Collections.ObjectModel;
using System.Threading;
using System.Windows.Input;
using System.Windows.Threading;
using NLog;
using nLogViewer.Infrastructure.Commands;
using nLogViewer.Model;
using nLogViewer.ViewModels.Base;

namespace nLogViewer.ViewModels;

public class LogViewerViewModel : BaseViewModel
{
    private static Logger _logger = LogManager.GetCurrentClassLogger();
    private readonly ILogReader _reader;
    private static LogViewerState _state;
    private DispatcherTimer _timer;

    public ObservableCollection<LogEntryView> LogEntries { get; private set; }
    public LogEntry SelectedEntry { get; set; }
    public int SelectedIndex { get; set; }
    
     public LogViewerViewModel() { }
    public LogViewerViewModel(string path)
    {
        _logger.Debug($"Вызов конструктора {GetType().Name} с параметрами (файл: {path})");
        LogEntries = new ObservableCollection<LogEntryView>();
        _reader = new FileLogReader(path);
        
        _timer = new DispatcherTimer();
        _timer.Tick += new EventHandler(Process);
        _timer.Interval = new TimeSpan(0,0,2);
        _timer.Start();
        
        #region commands
        AutoscrollCommand = new LambdaCommand(OnEnableAutoscrollCommandExecuted, CanEnableAutoscrollCommandExecute);
        ClearCommand = new LambdaCommand(OnClearCommandExecuted, CanClearCommandExecute);
        PauseCommand = new LambdaCommand(OnPauseCommandExecuted, CanPauseCommandExecute);
        #endregion
    }

    private void Process(object sender, EventArgs e)
    {
        _logger.Trace($"Просмотрщик лога в состоянии {_state}");

        switch (_state)
        {
            case LogViewerState.Stop:
                _logger.Debug($"Переход в состояние {LogViewerState.ReadAllMsg}");
                _state = LogViewerState.ReadAllMsg;    
                break;
            case LogViewerState.ReadAllMsg:
                LogEntries.Clear();
                var data = _reader.GetAll();
                foreach (var entry in data)
                    LogEntries.Insert(0, new LogEntryView(entry));
                _logger.Trace($"Считывание всех событий");
                _state = LogViewerState.ReadNewMsg;
                _logger.Debug($"Переход в состояние {LogViewerState.ReadNewMsg}");
                break;
            case LogViewerState.ReadNewMsg:
                var newData = _reader.GetNew();
                foreach (var entry in newData)
                    LogEntries.Insert(0, new LogEntryView(entry));
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
        LogEntries.Clear();
    }
    private bool CanClearCommandExecute(object p) => LogEntries.Count > 0;
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
    private bool CanPauseCommandExecute(object p) => LogEntries.Count > 0;
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