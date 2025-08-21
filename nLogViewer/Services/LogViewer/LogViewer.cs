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
using nLogViewer.Services.Progress;

namespace nLogViewer.Services.LogViewer;

internal class LogViewer : ILogViewer, IDisposable
{
    private static readonly Logger _logger = LogManager.GetCurrentClassLogger();
    private static readonly IReadOnlyList<ILogEntry> EmptyLogEntries = new List<ILogEntry>().AsReadOnly();
    
    private readonly ILogSource _reader;
    private readonly MemoryConfiguration _memoryConfig;
    private readonly IProgressReporter _progressReporter;
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
    private readonly TimeSpan _errorReportThrottleInterval = TimeSpan.FromMinutes(5); // Показывать сводку ошибок не чаще раз в 5 минут
    private readonly int _maxErrorsBeforeReport = 10; // Показывать сводку после накопления 10 ошибок
    // команды
    private bool _start;
    private bool _stop;
    private bool _pause;

    public event EntriesChanged EntriesChanged;
    public int Count => _memoryConfig?.UseCircularBuffer == true ? _circularBuffer?.Count ?? 0 : _logEntries.Count;
    public LogViewerState State => _state;
    public IReadOnlyList<ILogEntry> LogEntries => _memoryConfig?.UseCircularBuffer == true 
        ? _circularBuffer?.ToList().AsReadOnly() ?? EmptyLogEntries 
        : _logEntries?.AsReadOnly() ?? EmptyLogEntries;
    
    public LogViewer(ILogReaderFactory readerFactory, MemoryConfiguration memoryConfig, IProgressReporter progressReporter = null)
    {
        _logger.Debug($"Вызов конструктора {GetType().Name} с параметрами: readerFactory - {readerFactory}, memoryConfig - {memoryConfig}");
        _reader = readerFactory.Create();
        _memoryConfig = memoryConfig;
        _progressReporter = progressReporter;
        
        Initialize();
    }
    
    public LogViewer(ILogReaderFactory readerFactory, MemoryConfiguration memoryConfig, string filePath, IProgressReporter progressReporter = null)
    {
        _logger.Debug($"Вызов конструктора {GetType().Name} с файлом: {filePath}");
        _reader = readerFactory.Create(filePath);
        _memoryConfig = memoryConfig;
        _progressReporter = progressReporter;
        
        Initialize();
    }
    
    private void Initialize()
    {
        if (_memoryConfig?.UseCircularBuffer == true)
            _circularBuffer = new CircularBuffer<ILogEntry>(_memoryConfig.MaxEntriesInMemory);
        else
            _logEntries = new List<ILogEntry>();
        
        _cancellationTokenSource = new CancellationTokenSource();
        var tm = new TimerCallback(async obj => await ProcessAsync(obj));
        _timer = new Timer(tm, null, 0, 2000);
    }

    public void Start() => _start = true;
    public void Stop() => _stop = true;
    public void Pause() => _pause = true;
    [Obsolete("Use ClearAsync instead to prevent UI blocking")]
    public void Clear()
    {
        ClearInternal();
    }
    
    private void ClearInternal()
    {
        _logger.Debug($"Очистка всех событий (синхронно - НЕ рекомендуется)");
        if (_memoryConfig?.UseCircularBuffer == true)
            _circularBuffer?.Clear();
        else
            _logEntries?.Clear();
        
        // WARNING: Synchronous clear fallback - may cause deadlocks
        #pragma warning disable CS0618 // Type or member is obsolete
        _reader.Clear();
        #pragma warning restore CS0618 // Type or member is obsolete
        _prevEntriesCount = 0;
        
        EntriesChanged?.Invoke();
    }
    
    /// <summary>
    /// Асинхронно очищает все записи в логе
    /// </summary>
    /// <returns>Task для ожидания завершения операции</returns>
    public async Task ClearAsync()
    {
        _logger.Debug($"Асинхронная очистка всех событий");
        
        // Clear memory buffers synchronously (fast operation)
        if (_memoryConfig?.UseCircularBuffer == true)
            _circularBuffer?.Clear();
        else
            _logEntries?.Clear();
        
        _prevEntriesCount = 0;
        
        // Clear underlying reader asynchronously
        await _reader.ClearAsync(_cancellationTokenSource.Token);
        
        // Notify about changes
        EntriesChanged?.Invoke();
    }
    
    public void Dispose()
    {
        if (_disposed)
            return;
            
        _logger.Debug($"Освобождение ресурсов LogViewer");
        
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
            return count == 0 ? _circularBuffer : _circularBuffer.Skip(Math.Max(0, _circularBuffer.Count - count));
        }
        else
        {
            return count == 0 ? LogEntries : LogEntries.Skip(Math.Max(0, Count - count));
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

    private async Task ProcessAsync(object obj)
    {
        // Проверяем, не освобожден ли объект
        if (_disposed)
            return;
            
        // Проверяем, не занят ли уже процесс обработки
        try
        {
            if (!await _processLock.WaitAsync(0))
            {
                _logger.Trace($"Процесс обработки уже выполняется, пропускаем");
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
            _logger.Trace($"Просмотрщик лога в состоянии {_state}");
            if (_reader is null)
            {
                _logger.Warn($"Просмоторщик событий не инициализирован");
                return;
            }

            var cancellationToken = _cancellationTokenSource.Token;

            switch (_state)
            {
                case LogViewerState.Stop:
                    if (_start)
                    {
                        _logger.Debug($"Команда на переход в состояние {LogViewerState.ReadAllMsg}");
                        _state = LogViewerState.ReadAllMsg;
                    }

                    break;
                case LogViewerState.ReadAllMsg:
                    var allEntries = new List<ILogEntry>();
                    var errors = new List<Exception>();
                    var entriesRead = 0;

                    try
                    {
                        await foreach (var entry in _reader.GetAllAsync(_progressReporter, cancellationToken))
                        {
                            try
                            {
                                allEntries.Add(entry);
                                entriesRead++;
                            }
                            catch (Exception ex)
                            {
                                errors.Add(ex);
                                _logger.Warn(ex, $"Ошибка при добавлении записи #{entriesRead}");
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        errors.Add(ex);
                        _logger.Error(ex, "Критическая ошибка при чтении всех записей");
                    }

                    if (_memoryConfig?.UseCircularBuffer == true)
                    {
                        _circularBuffer.Clear();
                        _circularBuffer.AddRange(allEntries);
                        _logger.Trace($"Считывание всех событий ({_circularBuffer.Count} успешно, {errors.Count} ошибок)");
                    }
                    else
                    {
                        _logEntries = allEntries;
                        _logger.Trace($"Считывание всех событий ({_logEntries.Count} успешно, {errors.Count} ошибок)");
                    }

                    if (errors.Count > 0)
                    {
                        _totalErrorCount += errors.Count;
                        var aggregateException = new AggregateException($"Произошло {errors.Count} ошибок при чтении логов", errors);
                        _logger.Error(aggregateException, $"Агрегированные ошибки при чтении всех записей");
                        ReportAccumulatedErrors();
                    }

                    _state = LogViewerState.ReadNewMsg;
                    _logger.Debug($"Переход в состояние {LogViewerState.ReadNewMsg}");
                    break;
                    
                case LogViewerState.ReadNewMsg:
                        if (_stop)
                        {
                            _logger.Debug($"Команда на переход в состояние {LogViewerState.Stop}");
                            _state = LogViewerState.Stop;
                            break;
                        }

                        if (_pause)
                        {
                            _logger.Debug($"Команда на переход в состояние {LogViewerState.Pause}");
                            _state = LogViewerState.Pause;
                            break;
                        }

                        var newErrors = new List<Exception>();
                        var newEntriesCount = 0;

                        try
                        {
                            await foreach (var entry in _reader.GetNewAsync(_progressReporter, cancellationToken))
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
                                    _logger.Warn(ex, $"Ошибка при добавлении новой записи #{Count + newEntriesCount}");
                                }
                            }
                        }
                        catch (Exception ex)
                        {
                            newErrors.Add(ex);
                            _logger.Error(ex, "Критическая ошибка при чтении новых записей");
                        }

                        _logger.Trace($"Считывание новых событий ({newEntriesCount} успешно, {newErrors.Count} ошибок)");

                        if (newErrors.Count > 0)
                        {
                            _totalErrorCount += newErrors.Count;
                            var aggregateException =
                                new AggregateException($"Произошло {newErrors.Count} ошибок при чтении новых логов",
                                    newErrors);
                            _logger.Error(aggregateException, $"Агрегированные ошибки при чтении новых записей");
                            ReportAccumulatedErrors();
                        }

                        break;
                        
                case LogViewerState.Pause:
                        if (_stop)
                        {
                            _logger.Debug($"Команда на переход в состояние {LogViewerState.Stop}");
                            _state = LogViewerState.Stop;
                            break;
                        }

                        if (_start)
                        {
                            _logger.Debug($"Команда на переход в состояние {LogViewerState.ReadNewMsg}");
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
            _logger.Debug("Операция была отменена");
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Ошибка при обработке логов");
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
        _logger.Trace($"Проверка появления новых сообщений: " +
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
            _logger.Error($"Накоплено {_totalErrorCount} ошибок за последние {timeSinceLastReport.TotalMinutes:F1} минут при обработке логов");
            _lastErrorReportTime = now;
            
            // Сбрасываем счетчик после 1000 ошибок, чтобы избежать переполнения
            if (_totalErrorCount >= 1000)
            {
                _logger.Warn($"Счетчик ошибок сброшен после достижения {_totalErrorCount} ошибок");
                _totalErrorCount = 0;
            }
        }
    }
}