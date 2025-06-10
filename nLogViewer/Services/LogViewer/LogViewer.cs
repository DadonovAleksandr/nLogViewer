using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using NLog;
using nLogViewer.Model;
using nLogViewer.Services.LogReader;
using nLogViewer.Services.LogReader.Factory;

namespace nLogViewer.Services.LogViewer;

internal class LogViewer : ILogViewer, IDisposable
{
    private static readonly Logger _log = LogManager.GetCurrentClassLogger();
    
    private readonly ILogReader _reader;
    private LogViewerState _state;
    private List<ILogEntry> _logEntries;
    private int  _prevEntriesCount;
    private Timer _timer;
    private readonly SemaphoreSlim _processLock = new SemaphoreSlim(1, 1);
    private CancellationTokenSource _cancellationTokenSource;
    private bool _disposed;
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
        _cancellationTokenSource = new CancellationTokenSource();

        TimerCallback tm = new TimerCallback(async obj => await ProcessAsync(obj));
        _timer = new Timer(tm, null, 0, 2000);
    }

    public void Start() => _start = true;
    public void Stop() => _stop = true;
    public void Pause() => _pause = true;
    public void Clear()
    {
        _log.Debug($"Очистка всех событий");
        _logEntries.Clear();
        _reader.Clear();
        _prevEntriesCount = 0;
    }
    
    public async Task ClearAsync()
    {
        _log.Debug($"Асинхронная очистка всех событий");
        _logEntries.Clear();
        await _reader.ClearAsync(_cancellationTokenSource.Token);
        _prevEntriesCount = 0;
    }
    
    public void Dispose()
    {
        if (_disposed)
            return;
            
        _log.Debug($"Освобождение ресурсов LogViewer");
        
        // Сначала отменяем операции и останавливаем таймер
        _cancellationTokenSource?.Cancel();
        _timer?.Change(Timeout.Infinite, 0);
        _timer?.Dispose();
        _timer = null;
        
        // Ждем завершения текущей операции
        try
        {
            _processLock?.Wait(TimeSpan.FromSeconds(5));
        }
        catch (ObjectDisposedException)
        {
            // Игнорируем, если уже освобожден
        }
        
        // Теперь освобождаем остальные ресурсы
        _reader?.Dispose();
        _logEntries?.Clear();
        _prevEntriesCount = 0;
        _processLock?.Dispose();
        _cancellationTokenSource?.Dispose();
        
        _disposed = true;
    }
    
    public IEnumerable<ILogEntry> GetEntries(int count = 0)
    {
        if (count == 0)
            return LogEntries;
        return LogEntries.Skip(Math.Max(0, Count - count));
    }

    private async Task ProcessAsync(object? obj)
    {
        // Проверяем, не освобожден ли объект
        if (_disposed)
            return;
            
        // Проверяем, не занят ли уже процесс обработки
        try
        {
            if (!await _processLock.WaitAsync(0))
            {
                _log.Trace($"Процесс обработки уже выполняется, пропускаем");
                return;
            }
        }
        catch (ObjectDisposedException)
        {
            // Объект уже освобожден
            return;
        }
        
        try
        {
            _log.Trace($"Просмотрщик лога в состоянии {_state}");
            if (_reader is null)
            {
                _log.Warn($"Просмоторщик событий не инициализирован");
                return;
            }
            
            var cancellationToken = _cancellationTokenSource.Token;
            
            switch (_state)
            {
                case LogViewerState.Stop:
                    if (_start)
                    {
                        _log.Debug($"Команда на переход в состояние {LogViewerState.ReadAllMsg}");
                        _state = LogViewerState.ReadAllMsg;
                    }
                    break;
                case LogViewerState.ReadAllMsg:
                    var allEntries = new List<ILogEntry>();
                    await foreach (var entry in _reader.GetAllAsync(cancellationToken))
                    {
                        allEntries.Add(entry);
                    }
                    _logEntries = allEntries;
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
                    await foreach (var entry in _reader.GetNewAsync(cancellationToken))
                    {
                        _logEntries.Add(entry);
                    }
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
        catch (OperationCanceledException)
        {
            _log.Debug("Операция была отменена");
        }
        catch (Exception ex)
        {
            _log.Error(ex, "Ошибка при обработке логов");
        }
        finally
        {
            if (!_disposed)
            {
                try
                {
                    _processLock?.Release();
                }
                catch (ObjectDisposedException)
                {
                    // Игнорируем, если объект уже освобожден
                }
            }
        }
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