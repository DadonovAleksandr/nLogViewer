using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using NLog;
using nLogViewer.Services.Progress;

namespace nLogViewer.Services.LogReader.Repository;

/// <summary>
/// Композитный репозиторий для работы с несколькими источниками логов одновременно
/// </summary>
internal class CompositeLogRepository : ILogRepository
{
    private static readonly Logger _log = LogManager.GetCurrentClassLogger();
    private readonly List<ILogRepository> _repositories = new();
    private bool _disposed;

    public string SourceId => "composite:" + string.Join(";", _repositories.Select(r => r.SourceId));
    public string SourceDescription => $"Composite ({_repositories.Count} sources)";
    public bool SupportsClear => _repositories.All(r => r.SupportsClear);
    public bool SupportsIncrementalRead => _repositories.All(r => r.SupportsIncrementalRead);

    /// <summary>
    /// Добавляет репозиторий в композит
    /// </summary>
    /// <param name="repository">Репозиторий для добавления</param>
    public void AddRepository(ILogRepository repository)
    {
        if (repository == null)
            throw new ArgumentNullException(nameof(repository));
            
        _log.Debug($"Добавление репозитория {repository.SourceDescription} в композит");
        _repositories.Add(repository);
    }

    /// <summary>
    /// Удаляет репозиторий из композита
    /// </summary>
    /// <param name="sourceId">ID источника для удаления</param>
    /// <returns>true если репозиторий был удален</returns>
    public bool RemoveRepository(string sourceId)
    {
        var repository = _repositories.FirstOrDefault(r => r.SourceId == sourceId);
        if (repository != null)
        {
            _log.Debug($"Удаление репозитория {repository.SourceDescription} из композита");
            repository.Dispose();
            return _repositories.Remove(repository);
        }
        return false;
    }

    /// <summary>
    /// Получает список всех репозиториев
    /// </summary>
    public IReadOnlyList<ILogRepository> Repositories => _repositories.AsReadOnly();

    public async Task<bool> IsAvailableAsync(CancellationToken cancellationToken = default)
    {
        if (_repositories.Count == 0)
            return false;

        var tasks = _repositories.Select(r => r.IsAvailableAsync(cancellationToken));
        var results = await Task.WhenAll(tasks);
        
        // Считаем доступным, если хотя бы один источник доступен
        return results.Any(r => r);
    }

    public async IAsyncEnumerable<string> ReadAllLinesAsync([EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        _log.Trace($"Чтение всех строк из {_repositories.Count} источников");
        
        // Читаем из всех источников параллельно и объединяем результаты
        var tasks = _repositories.Select(r => ReadAllFromRepositoryAsync(r, cancellationToken)).ToList();
        
        // Объединяем все строки из всех источников
        var allLines = new List<(DateTime timestamp, string line, string sourceId)>();
        
        foreach (var task in tasks)
        {
            await foreach (var item in task)
            {
                allLines.Add(item);
            }
        }
        
        // Сортируем по времени и возвращаем
        foreach (var item in allLines.OrderBy(x => x.timestamp))
        {
            yield return item.line;
        }
    }

    public async IAsyncEnumerable<string> ReadAllLinesAsync(IProgressReporter progressReporter, [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        _log.Trace($"Чтение всех строк из {_repositories.Count} источников с отчетом о прогрессе");
        
        // Читаем из всех источников параллельно и объединяем результаты
        var tasks = _repositories.Select(r => ReadAllFromRepositoryAsync(r, progressReporter, cancellationToken)).ToList();
        
        // Объединяем все строки из всех источников
        var allLines = new List<(DateTime timestamp, string line, string sourceId)>();
        var processedRepositories = 0;
        
        foreach (var task in tasks)
        {
            await foreach (var item in task)
            {
                allLines.Add(item);
            }
            
            processedRepositories++;
            if (progressReporter != null)
            {
                var progress = (double)processedRepositories / _repositories.Count * 50; // Первые 50% на чтение
                progressReporter.ReportPercentage((int)progress, $"Обработан источник {processedRepositories}/{_repositories.Count}");
            }
        }
        
        // Сортируем по времени и возвращаем
        var sortedLines = allLines.OrderBy(x => x.timestamp).ToList();
        var processedLines = 0;
        
        foreach (var item in sortedLines)
        {
            processedLines++;
            if (progressReporter != null && processedLines % 100 == 0)
            {
                var progress = 50 + (double)processedLines / sortedLines.Count * 50; // Вторые 50% на сортировку и возврат
                progressReporter.ReportPercentage((int)progress, $"Отсортировано {processedLines}/{sortedLines.Count} строк");
            }
            
            yield return item.line;
        }
        
        if (progressReporter != null)
        {
            progressReporter.ReportPercentage(100, $"Завершено. Обработано {sortedLines.Count} строк из {_repositories.Count} источников");
        }
    }

    public async IAsyncEnumerable<string> ReadNewLinesAsync([EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        _log.Trace($"Чтение новых строк из {_repositories.Count} источников");
        
        // Для новых строк читаем из всех источников и возвращаем по мере поступления
        var channels = _repositories
            .Where(r => r.SupportsIncrementalRead)
            .Select(r => CreateChannelForRepository(r, cancellationToken))
            .ToList();

        // Читаем из всех каналов пока есть данные
        while (channels.Count > 0)
        {
            cancellationToken.ThrowIfCancellationRequested();
            
            // Ждем данные из любого канала
            var completedTask = await Task.WhenAny(channels.Select(c => c.Reader.WaitToReadAsync(cancellationToken).AsTask()));
            
            // Обрабатываем все доступные данные
            for (int i = channels.Count - 1; i >= 0; i--)
            {
                var channel = channels[i];
                while (channel.Reader.TryRead(out var line))
                {
                    yield return line;
                }
                
                // Удаляем завершенные каналы
                if (channel.Reader.Completion.IsCompleted)
                {
                    channels.RemoveAt(i);
                }
            }
        }
    }

    public async IAsyncEnumerable<string> ReadNewLinesAsync(IProgressReporter progressReporter, [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        _log.Trace($"Чтение новых строк из {_repositories.Count} источников с отчетом о прогрессе");
        
        // Для новых строк читаем из всех источников и возвращаем по мере поступления
        var channels = _repositories
            .Where(r => r.SupportsIncrementalRead)
            .Select(r => CreateChannelForRepository(r, progressReporter, cancellationToken))
            .ToList();

        var processedLines = 0;
        
        // Читаем из всех каналов пока есть данные
        while (channels.Count > 0)
        {
            cancellationToken.ThrowIfCancellationRequested();
            
            // Ждем данные из любого канала
            var completedTask = await Task.WhenAny(channels.Select(c => c.Reader.WaitToReadAsync(cancellationToken).AsTask()));
            
            // Обрабатываем все доступные данные
            for (int i = channels.Count - 1; i >= 0; i--)
            {
                var channel = channels[i];
                while (channel.Reader.TryRead(out var line))
                {
                    processedLines++;
                    if (progressReporter != null && processedLines % 10 == 0)
                    {
                        progressReporter.ReportPercentage(50, $"Обработано {processedLines} новых строк");
                    }
                    
                    yield return line;
                }
                
                // Удаляем завершенные каналы
                if (channel.Reader.Completion.IsCompleted)
                {
                    channels.RemoveAt(i);
                }
            }
        }
        
        if (progressReporter != null)
        {
            progressReporter.ReportPercentage(100, $"Завершено. Обработано {processedLines} новых строк");
        }
    }

    public async Task<bool> ClearAsync(CancellationToken cancellationToken = default)
    {
        if (!SupportsClear)
        {
            _log.Warn("Не все источники поддерживают очистку");
            return false;
        }

        _log.Debug($"Очистка {_repositories.Count} источников");
        
        var tasks = _repositories.Select(r => r.ClearAsync(cancellationToken));
        var results = await Task.WhenAll(tasks);
        
        return results.All(r => r);
    }

    public void ResetPosition()
    {
        _log.Debug($"Сброс позиции чтения для {_repositories.Count} источников");
        foreach (var repository in _repositories)
        {
            repository.ResetPosition();
        }
    }

    private async IAsyncEnumerable<(DateTime timestamp, string line, string sourceId)> ReadAllFromRepositoryAsync(
        ILogRepository repository,
        [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        await foreach (var line in repository.ReadAllLinesAsync(cancellationToken))
        {
            // Пытаемся извлечь временную метку из строки для сортировки
            var timestamp = ExtractTimestamp(line);
            yield return (timestamp, line, repository.SourceId);
        }
    }

    private async IAsyncEnumerable<(DateTime timestamp, string line, string sourceId)> ReadAllFromRepositoryAsync(
        ILogRepository repository,
        IProgressReporter progressReporter,
        [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        await foreach (var line in repository.ReadAllLinesAsync(progressReporter, cancellationToken))
        {
            // Пытаемся извлечь временную метку из строки для сортировки
            var timestamp = ExtractTimestamp(line);
            yield return (timestamp, line, repository.SourceId);
        }
    }

    private System.Threading.Channels.Channel<string> CreateChannelForRepository(ILogRepository repository, CancellationToken cancellationToken)
    {
        var channel = System.Threading.Channels.Channel.CreateUnbounded<string>();
        
        // Запускаем фоновую задачу для чтения из репозитория
        _ = Task.Run(async () =>
        {
            try
            {
                await foreach (var line in repository.ReadNewLinesAsync(cancellationToken))
                {
                    await channel.Writer.WriteAsync(line, cancellationToken);
                }
            }
            catch (Exception ex)
            {
                _log.Error(ex, $"Ошибка чтения из репозитория {repository.SourceDescription}");
            }
            finally
            {
                channel.Writer.Complete();
            }
        }, cancellationToken);
        
        return channel;
    }

    private System.Threading.Channels.Channel<string> CreateChannelForRepository(ILogRepository repository, IProgressReporter progressReporter, CancellationToken cancellationToken)
    {
        var channel = System.Threading.Channels.Channel.CreateUnbounded<string>();
        
        // Запускаем фоновую задачу для чтения из репозитория
        _ = Task.Run(async () =>
        {
            try
            {
                await foreach (var line in repository.ReadNewLinesAsync(progressReporter, cancellationToken))
                {
                    await channel.Writer.WriteAsync(line, cancellationToken);
                }
            }
            catch (Exception ex)
            {
                _log.Error(ex, $"Ошибка чтения из репозитория {repository.SourceDescription}");
            }
            finally
            {
                channel.Writer.Complete();
            }
        }, cancellationToken);
        
        return channel;
    }

    private DateTime ExtractTimestamp(string line)
    {
        // Простая попытка извлечь дату из начала строки
        if (line.Length >= 23 && DateTime.TryParse(line.Substring(0, 23), out var timestamp))
        {
            return timestamp;
        }
        return DateTime.Now;
    }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    protected virtual void Dispose(bool disposing)
    {
        if (_disposed)
            return;

        if (disposing)
        {
            _log.Debug($"Освобождение ресурсов CompositeLogRepository ({_repositories.Count} источников)");
            foreach (var repository in _repositories)
            {
                repository.Dispose();
            }
            _repositories.Clear();
        }

        _disposed = true;
    }
}