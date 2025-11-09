using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using NLog;
using nLogViewer.Model;

namespace nLogViewer.Infrastructure.Pipeline;

/// <summary>
/// Pipeline для фоновой обработки логов с использованием каналов
/// Разделяет процесс на три этапа:
/// 1. File Reader (I/O bound) - чтение строк из файла
/// 2. Parser Pool (CPU bound) - параллельный парсинг строк
/// 3. UI Updater (rate-limited) - обновление UI с ограничением ~60 FPS
/// </summary>
internal class LogProcessingPipeline : IDisposable
{
    private static readonly Logger _logger = LogManager.GetCurrentClassLogger();

    // Каналы для передачи данных между этапами
    private readonly Channel<LogBatch> _parsingChannel;
    private readonly Channel<LogBatch> _uiChannel;

    // Задачи для каждого этапа
    private Task _parsingTask;
    private Task _uiUpdateTask;

    // Токен отмены
    private CancellationTokenSource _cancellationTokenSource;
    private bool _disposed;

    // Callback для парсинга одной строки
    private readonly Func<ReadOnlySpan<char>, ILogEntry> _parseLineCallback;

    // Callback для обновления коллекции
    private readonly Action<IReadOnlyList<ILogEntry>> _updateCollectionCallback;

    // Статистика
    private long _totalLinesParsed;
    private long _totalEntriesParsed;
    private long _totalUiUpdates;

    public LogProcessingPipeline(
        Func<ReadOnlySpan<char>, ILogEntry> parseLineCallback,
        Action<IReadOnlyList<ILogEntry>> updateCollectionCallback)
    {
        _parseLineCallback = parseLineCallback ?? throw new ArgumentNullException(nameof(parseLineCallback));
        _updateCollectionCallback = updateCollectionCallback ?? throw new ArgumentNullException(nameof(updateCollectionCallback));

        // Unbounded channel для приема сырых строк
        _parsingChannel = Channel.CreateUnbounded<LogBatch>(new UnboundedChannelOptions
        {
            SingleReader = false, // Несколько парсеров могут читать
            SingleWriter = true
        });

        // Bounded channel для UI updates (ограничиваем backpressure)
        _uiChannel = Channel.CreateBounded<LogBatch>(new BoundedChannelOptions(10)
        {
            FullMode = BoundedChannelFullMode.DropOldest // Сбрасываем старые обновления
        });

        _cancellationTokenSource = new CancellationTokenSource();

        _logger.Info("LogProcessingPipeline создан");
    }

    /// <summary>
    /// Запускает pipeline
    /// </summary>
    public void Start()
    {
        if (_parsingTask != null || _uiUpdateTask != null)
        {
            _logger.Warn("Pipeline уже запущен");
            return;
        }

        var token = _cancellationTokenSource.Token;

        // Запускаем воркеры парсинга (по одному на каждое ядро CPU)
        var parserCount = Environment.ProcessorCount;
        _logger.Info($"Запуск {parserCount} парсеров");

        var parsingTasks = new List<Task>(parserCount);
        for (int i = 0; i < parserCount; i++)
        {
            int workerId = i;
            parsingTasks.Add(Task.Run(() => ParsingWorker(workerId, token), token));
        }

        _parsingTask = Task.WhenAll(parsingTasks);

        // Запускаем UI updater
        _uiUpdateTask = Task.Run(() => UiUpdateWorker(token), token);

        _logger.Info("Pipeline запущен");
    }

    /// <summary>
    /// Останавливает pipeline
    /// </summary>
    public async Task StopAsync()
    {
        _logger.Info("Остановка pipeline...");

        // Завершаем канал парсинга
        _parsingChannel.Writer.Complete();

        // Ждем завершения парсинга
        if (_parsingTask != null)
        {
            await _parsingTask;
        }

        // Завершаем канал UI
        _uiChannel.Writer.Complete();

        // Ждем завершения UI updater
        if (_uiUpdateTask != null)
        {
            await _uiUpdateTask;
        }

        _logger.Info($"Pipeline остановлен. Статистика: {_totalLinesParsed:N0} строк, {_totalEntriesParsed:N0} записей, {_totalUiUpdates:N0} UI обновлений");
    }

    /// <summary>
    /// Добавляет батч строк для обработки
    /// </summary>
    /// <param name="rawLines">Сырые строки из файла</param>
    public async Task EnqueueBatchAsync(IReadOnlyList<string> rawLines, CancellationToken cancellationToken = default)
    {
        if (rawLines == null || rawLines.Count == 0)
            return;

        var batch = new LogBatch { RawLines = rawLines };
        await _parsingChannel.Writer.WriteAsync(batch, cancellationToken);
    }

    /// <summary>
    /// Воркер для парсинга строк (CPU-bound, параллельно)
    /// </summary>
    private async Task ParsingWorker(int workerId, CancellationToken cancellationToken)
    {
        _logger.Debug($"Парсер #{workerId} запущен");

        try
        {
            await foreach (var batch in _parsingChannel.Reader.ReadAllAsync(cancellationToken))
            {
                var entries = new ConcurrentBag<ILogEntry>();

                // Параллельный парсинг строк в батче
                Parallel.ForEach(batch.RawLines, new ParallelOptions
                {
                    MaxDegreeOfParallelism = 1, // Каждый воркер обрабатывает строки последовательно
                    CancellationToken = cancellationToken
                }, line =>
                {
                    try
                    {
                        var entry = _parseLineCallback(line.AsSpan());
                        if (entry != null)
                        {
                            entries.Add(entry);
                        }

                        Interlocked.Increment(ref _totalLinesParsed);
                    }
                    catch (Exception ex)
                    {
                        _logger.Warn(ex, $"Ошибка парсинга строки: {line}");
                    }
                });

                if (entries.Count > 0)
                {
                    Interlocked.Add(ref _totalEntriesParsed, entries.Count);

                    // Отправляем распарсенные записи в UI канал
                    var parsedBatch = new LogBatch { Entries = entries.ToList() };
                    await _uiChannel.Writer.WriteAsync(parsedBatch, cancellationToken);
                }
            }
        }
        catch (OperationCanceledException)
        {
            _logger.Debug($"Парсер #{workerId} отменен");
        }
        catch (Exception ex)
        {
            _logger.Error(ex, $"Критическая ошибка в парсере #{workerId}");
        }

        _logger.Debug($"Парсер #{workerId} завершен");
    }

    /// <summary>
    /// Воркер для обновления UI (rate-limited до ~60 FPS)
    /// </summary>
    private async Task UiUpdateWorker(CancellationToken cancellationToken)
    {
        _logger.Debug("UI Updater запущен");

        try
        {
            await foreach (var batch in _uiChannel.Reader.ReadAllAsync(cancellationToken))
            {
                try
                {
                    // Обновляем коллекцию
                    _updateCollectionCallback(batch.Entries);

                    Interlocked.Increment(ref _totalUiUpdates);

                    // Rate limiting: ~60 FPS (16ms между кадрами)
                    await Task.Delay(16, cancellationToken);
                }
                catch (Exception ex)
                {
                    _logger.Error(ex, "Ошибка обновления UI");
                }
            }
        }
        catch (OperationCanceledException)
        {
            _logger.Debug("UI Updater отменен");
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Критическая ошибка в UI Updater");
        }

        _logger.Debug("UI Updater завершен");
    }

    public void Dispose()
    {
        if (_disposed)
            return;

        _logger.Debug("Освобождение ресурсов LogProcessingPipeline");

        _cancellationTokenSource?.Cancel();

        // Пытаемся корректно остановить pipeline
        try
        {
            StopAsync().Wait(TimeSpan.FromSeconds(5));
        }
        catch (Exception ex)
        {
            _logger.Warn(ex, "Ошибка при остановке pipeline");
        }

        _cancellationTokenSource?.Dispose();
        _disposed = true;
    }
}

/// <summary>
/// Батч данных для передачи между этапами pipeline
/// </summary>
internal class LogBatch
{
    /// <summary>
    /// Сырые строки из файла (используется между File Reader и Parser)
    /// </summary>
    public IReadOnlyList<string> RawLines { get; set; }

    /// <summary>
    /// Распарсенные записи (используется между Parser и UI Updater)
    /// </summary>
    public IReadOnlyList<ILogEntry> Entries { get; set; }
}
