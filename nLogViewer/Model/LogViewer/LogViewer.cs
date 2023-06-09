using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading;
using NLog;

namespace nLogViewer.Model;

internal class LogViewer
{
    private static readonly Logger _log = LogManager.GetCurrentClassLogger();
    
    private readonly ILogReader _reader;
    private LogViewerState _state;

    public LogViewerState State => _state;
    public bool Pause { get; set; }
    public bool Clear { get; set; }
    public List<ILogEntry> LogEntries { get; private set; }
    
    public LogViewer(ILogReader reader)
    {
        _log.Debug($"Вызов конструктора {GetType().Name} с параметрами: reader - {reader}");
        
        _reader = reader;

        LogEntries = new List<ILogEntry>();
        
        TimerCallback tm = new TimerCallback(Process);
        Timer timer = new Timer(tm, null, 0, 1000);
    }

    public void Start() => _state = LogViewerState.ReadAllMsg; 
    
    public void Stop() => _state = LogViewerState.Stop;
    
    private void Process(object? obj)
    {
        _log.Trace($"Просмотрщик лога в состоянии {_state}");

        switch (_state)
        {
            case LogViewerState.Stop:
                break;
            case LogViewerState.ReadAllMsg:
                LogEntries = _reader.GetAll().ToList();
                _log.Trace($"Считывание всех событий");
                _state = LogViewerState.ReadNewMsg;
                _log.Debug($"Переход в состояние {LogViewerState.ReadNewMsg}");
                break;
            case LogViewerState.ReadNewMsg:
                if (Clear)
                {
                    _log.Debug($"Очистка всех событий");
                    LogEntries.Clear();
                }

                if (Pause)
                {
                    _log.Debug($"Переход в состояние {LogViewerState.Pause}");
                    _state = LogViewerState.Pause;
                    break;
                }
                    
                LogEntries.AddRange(_reader.GetNew());
                _log.Trace($"Считывание новых событий");
                break;
            case LogViewerState.Pause:
                if (!Pause)
                {
                    _log.Debug($"Переход в состояние {LogViewerState.ReadNewMsg}");
                    _state = LogViewerState.ReadNewMsg;
                }
                break;
            default: throw new ArgumentOutOfRangeException();
        }
    }
    
    
}