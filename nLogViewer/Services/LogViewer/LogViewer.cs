using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using NLog;
using nLogViewer.Model;
using nLogViewer.Services.LogReader;
using nLogViewer.Services.LogReader.Factory;

namespace nLogViewer.Services.LogViewer;

internal class LogViewer : ILogViewer
{
    private static readonly Logger _log = LogManager.GetCurrentClassLogger();
    
    private readonly ILogReader _reader;
    private LogViewerState _state;
    private List<ILogEntry> _logEntries;
    private int  _prevEntriesCount;
    private Timer _timer;
    // команды
    private bool _start;
    private bool _stop;
    private bool _pause;

    public event EntriesChanged EntriesChanged;
    public int Count => _logEntries.Count;
    public LogViewerState State => _state;
    public List<ILogEntry> LogEntries => _logEntries;
    
    public LogViewer(ILogReaderFactory readerFactory)
    {
        _log.Debug($"Вызов конструктора {GetType().Name} с параметрами: readerFactory - {readerFactory}");
        
        _reader = readerFactory.Create();
        _logEntries = new List<ILogEntry>();

        TimerCallback tm = new TimerCallback(Process);
        _timer = new Timer(tm, null, 0, 2000);
    }

    public void Start() => _start = true;
    public void Stop() => _stop = true;
    public void Pause() => _pause = true;
    public void Clear()
    {
        _log.Debug($"Очистка всех событий");
        _logEntries.Clear();
    }
    
    public IEnumerable<ILogEntry> GetEntries(int count = 0)
    {
        if (count == 0)
            return LogEntries;
        return LogEntries.Skip(Math.Max(0, Count - count));

    }
    private void Process(object? obj)
    {
        _log.Trace($"Просмотрщик лога в состоянии {_state}");
        if (_reader is null)
        {
            _log.Warn($"Просмоторщик событий не инициализирован");
            return;
        }
        
        switch (_state)
        {
            case LogViewerState.Stop:
                if (_start)
                {
                    _log.Debug($"Команда на переход в состояние {LogViewerState.ReadAllMsg}");
                    _state = LogViewerState.ReadAllMsg;
                    break;
                }
                break;
            case LogViewerState.ReadAllMsg:
                _logEntries = _reader.GetAll().ToList();
                _log.Trace($"Считывание всех событий ({_logEntries.Count})");
                _state = LogViewerState.ReadNewMsg;
                _log.Debug($"Переход в состояние {LogViewerState.ReadNewMsg}");
                break;
            case LogViewerState.ReadNewMsg:
                if (_stop)
                {
                    _log.Debug($"Команда на переход в состояние {LogViewerState.Stop}");
                    _state = LogViewerState.Stop;
                    break;
                }
                if (_pause)
                {
                    _log.Debug($"Команда на переход в состояние {LogViewerState.Pause}");
                    _state = LogViewerState.Pause;
                    break;
                } 
                _logEntries.AddRange(_reader.GetNew());
                _log.Trace($"Считывание новых событий");
                break;
            case LogViewerState.Pause:
                if (_stop)
                {
                    _log.Debug($"Команда на переход в состояние {LogViewerState.Stop}");
                    _state = LogViewerState.Stop;
                    break;
                }
                if (_start)
                {
                    _log.Debug($"Команда на переход в состояние {LogViewerState.ReadNewMsg}");
                    _state = LogViewerState.ReadNewMsg;
                    break;
                }    
                break;
            default: throw new ArgumentOutOfRangeException();
        }
        
        // сброс команд
        _start = false;
        _stop = false;
        _pause = false;
        
        CheckEntriesChange();
    }

    private void CheckEntriesChange()
    {
        _log.Trace($"Проверка появления новых сообщений: " +
                   $"новых сообщений ({_logEntries.Count - _prevEntriesCount}) =  " +
                   $"новое значение счетчика ({_logEntries.Count}) - предъидущее значение счетчика ({_prevEntriesCount})");
        if(_logEntries.Count <= _prevEntriesCount)
            return;

        _prevEntriesCount = _logEntries.Count;
        EntriesChanged?.Invoke();
    }
}