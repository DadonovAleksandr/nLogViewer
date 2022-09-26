using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading;
using NLog;

namespace nLogViewer.Model;

internal class LogViewer
{
    private static Logger _logger = LogManager.GetCurrentClassLogger();
    
    private readonly ILogReader _reader;
    private LogViewerState _state;

    public LogViewerState State => _state;
    public bool Pause { get; set; }
    public bool Clear { get; set; }
    public List<ILogEntry> LogEntries { get; private set; }
    
    public LogViewer(ILogReader reader)
    {
        _logger.Debug($"Вызов конструктора {GetType().Name} с параметрами: reader - {reader}");
        
        _reader = reader;

        LogEntries = new List<ILogEntry>();
        
        TimerCallback tm = new TimerCallback(Process);
        Timer timer = new Timer(tm, null, 0, 1000);
    }

    public void Start() => _state = LogViewerState.ReadAllMsg; 
    
    public void Stop() => _state = LogViewerState.Stop;
    
    private void Process(object? obj)
    {
        _logger.Trace($"Просмотрщик лога в состоянии {_state}");

        switch (_state)
        {
            case LogViewerState.Stop:
                break;
            case LogViewerState.ReadAllMsg:
                LogEntries = _reader.GetAll().ToList();
                _logger.Trace($"Считывание всех событий");
                _state = LogViewerState.ReadNewMsg;
                _logger.Debug($"Переход в состояние {LogViewerState.ReadNewMsg}");
                break;
            case LogViewerState.ReadNewMsg:
                if (Clear)
                {
                    _logger.Debug($"Очистка всех событий");
                    LogEntries.Clear();
                }

                if (Pause)
                {
                    _logger.Debug($"Переход в состояние {LogViewerState.Pause}");
                    _state = LogViewerState.Pause;
                    break;
                }
                    
                LogEntries.AddRange(_reader.GetNew());
                _logger.Trace($"Считывание новых событий");
                break;
            case LogViewerState.Pause:
                if (!Pause)
                {
                    _logger.Debug($"Переход в состояние {LogViewerState.ReadNewMsg}");
                    _state = LogViewerState.ReadNewMsg;
                }
                break;
            default: throw new ArgumentOutOfRangeException();
        }
    }
    
    
}