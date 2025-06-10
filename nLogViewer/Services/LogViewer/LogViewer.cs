using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using NLog;
using nLogViewer.Infrastructure.Collections;
using nLogViewer.Infrastructure.Configuration;
using nLogViewer.Model;
using nLogViewer.Services.LogReader;
using nLogViewer.Services.LogReader.Factory;

namespace nLogViewer.Services.LogViewer;

internal class LogViewer : ILogViewer, IDisposable
{
    private static readonly Logger _log = LogManager.GetCurrentClassLogger();
    
    private readonly ILogSource _reader;
    private readonly MemoryConfiguration _memoryConfig;
    private LogViewerState _state;
    private List<ILogEntry> _logEntries;
    private CircularBuffer<ILogEntry> _circularBuffer;
    private int  _prevEntriesCount;
    private Timer _timer;
    private readonly SemaphoreSlim _processLock = new SemaphoreSlim(1, 1);
    private CancellationTokenSource _cancellationTokenSource;
    private bool _disposed;
    private int _totalErrorCount;
    private DateTime _lastErrorReportTime = DateTime.MinValue;
    // команды
    private bool _start;
    private bool _stop;
    private bool _pause;

    public event EntriesChanged EntriesChanged;
    public int Count => _memoryConfig?.UseCircularBuffer == true ? _circularBuffer?.Count ?? 0 : _logEntries.Count;
    public LogViewerState State => _state;
    public List<ILogEntry> LogEntries => _memoryConfig?.UseCircularBuffer == true ? _circularBuffer?.ToList() ?? new List<ILogEntry>() : _logEntries;
    
    public LogViewer(ILogReaderFactory readerFactory, MemoryConfiguration memoryConfig)
    {
        _log.Debug($"Вызов конструктора {GetType().Name} с параметрами: readerFactory - {readerFactory}, memoryConfig - {memoryConfig}");
        
        _reader = readerFactory.Create();
        _memoryConfig = memoryConfig;
        
        if (_memoryConfig?.UseCircularBuffer == true)
        {
            _circularBuffer = new CircularBuffer<ILogEntry>(_memoryConfig.MaxEntriesInMemory);
        }
        else
        {
            _logEntries = new List<ILogEntry>();
        }
        
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
        if (_memoryConfig?.UseCircularBuffer == true)
        {
            _circularBuffer?.Clear();
        }
        else
        {
            _logEntries?.Clear();
        }
        _reader.Clear();
        _prevEntriesCount = 0;
    }
    
    public async Task ClearAsync()
    {
        _log.Debug($"Асинхронная очистка всех событий");
        if (_memoryConfig?.UseCircularBuffer == true)
        {
            _circularBuffer?.Clear();
        }
        else
        {
            _logEntries?.Clear();
        }
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
        _circularBuffer?.Clear();
        _prevEntriesCount = 0;
        _processLock?.Dispose();
        _cancellationTokenSource?.Dispose();
        
        _disposed = true;
    }
    
    public IEnumerable<ILogEntry> GetEntries(int count = 0)
    {
        if (_memoryConfig?.UseCircularBuffer == true)
        {
            if (count == 0)
                return _circularBuffer;
            return _circularBuffer.Skip(Math.Max(0, _circularBuffer.Count - count));
        }
        else
        {
            if (count == 0)
                return LogEntries;
            return LogEntries.Skip(Math.Max(0, Count - count));
        }
    }
    
    public IEnumerable<ILogEntry> GetEntriesRange(int startIndex, int count)
    {
        if (_memoryConfig?.UseCircularBuffer == true)
        {
            return _circularBuffer?.GetRange(startIndex, count) ?? Enumerable.Empty<ILogEntry>();
        }
        else
        {
            if (startIndex < 0 || startIndex >= _logEntries.Count)
                return Enumerable.Empty<ILogEntry>();
                
            return _logEntries
                .Skip(startIndex)
                .Take(count);
        }
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
                    var errors = new List<Exception>();
                    var entriesRead = 0;

                    try
                    {
                        await foreach (var entry in _reader.GetAllAsync(cancellationToken))
                        {
                            try
                            {
                                allEntries.Add(entry);
                                entriesRead++;
                            }
                            catch (Exception ex)
                            {
                                errors.Add(ex);
                                _log.Warn(ex, $"Ошибка при добавлении записи #{entriesRead}");
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        errors.Add(ex);
                        _log.Error(ex, "Критическая ошибка при чтении всех записей");
                    }

                    if (_memoryConfig?.UseCircularBuffer == true)
                    {
                        _circularBuffer.Clear();
                        _circularBuffer.AddRange(allEntries);
                        _log.Trace($"Считывание всех событий ({_circularBuffer.Count} успешно, {errors.Count} ошибок)");
                    }
                    else
                    {
                        _logEntries = allEntries;
                        _log.Trace($"Считывание всех событий ({_logEntries.Count} успешно, {errors.Count} ошибок)");
                    }

                    if (errors.Count > 0)
                    {
                        _totalErrorCount += errors.Count;
                        var aggregateException = new AggregateException($"Произошло {errors.Count} ошибок при чтении логов", errors);
                        _log.Error(aggregateException, $"Агрегированные ошибки при чтении всех записей");
                        ReportAccumulatedErrors();
                    }

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

                        var newErrors = new List<Exception>();
                        var newEntriesCount = 0;

                        try
                        {
                            await foreach (var entry in _reader.GetNewAsync(cancellationToken))
                            {
                                try
                                {
                                    if (_memoryConfig?.UseCircularBuffer == true)
                                    {
                                        _circularBuffer.Add(entry);
                                    }
                                    else
                                    {
                                        _logEntries.Add(entry);
                                    }

                                    newEntriesCount++;
                                }
                                catch (Exception ex)
                                {
                                    newErrors.Add(ex);
                                    _log.Warn(ex, $"Ошибка при добавлении новой записи #{Count + newEntriesCount}");
                                }
                            }
                        }
                        catch (Exception ex)
                        {
                            newErrors.Add(ex);
                            _log.Error(ex, "Критическая ошибка при чтении новых записей");
                        }

                        _log.Trace($"Считывание новых событий ({newEntriesCount} успешно, {newErrors.Count} ошибок)");

                        if (newErrors.Count > 0)
                        {
                            _totalErrorCount += newErrors.Count;
                            var aggregateException =
                                new AggregateException($"Произошло {newErrors.Count} ошибок при чтении новых логов",
                                    newErrors);
                            _log.Error(aggregateException, $"Агрегированные ошибки при чтении новых записей");
                            ReportAccumulatedErrors();
                        }

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
                        
                default: 
                    throw new ArgumentOutOfRangeException();
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
        var currentCount = Count;
        _log.Trace($"Проверка появления новых сообщений: " +
                   $"новых сообщений ({currentCount - _prevEntriesCount}) =  " +
                   $"новое значение счетчика ({currentCount}) - предъидущее значение счетчика ({_prevEntriesCount})");
        if(currentCount <= _prevEntriesCount)
            return;

        _prevEntriesCount = currentCount;
        EntriesChanged?.Invoke();
    }
    
    private void ReportAccumulatedErrors()
    {
        var now = DateTime.UtcNow;
        var timeSinceLastReport = now - _lastErrorReportTime;
        
        // Сообщаем о накопленных ошибках каждые 5 минут или при достижении 100 ошибок
        if (timeSinceLastReport.TotalMinutes >= 5 || _totalErrorCount >= 100)
        {
            _log.Error($"Накоплено {_totalErrorCount} ошибок за последние {timeSinceLastReport.TotalMinutes:F1} минут при обработке логов");
            _lastErrorReportTime = now;
            
            // Сбрасываем счетчик после 1000 ошибок, чтобы избежать переполнения
            if (_totalErrorCount >= 1000)
            {
                _log.Warn($"Счетчик ошибок сброшен после достижения {_totalErrorCount} ошибок");
                _totalErrorCount = 0;
            }
        }
    }
}