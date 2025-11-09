using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using NLog;
using nLogViewer.Model;
using nLogViewer.Services.LogReader.Repository;
using nLogViewer.Services.Progress;

namespace nLogViewer.Services.LogReader;

/// <summary>
/// Адаптер для использования ILogRepository как ILogSource
/// </summary>
internal class RepositoryLogSource : ILogSource
{
    private static readonly Logger _log = LogManager.GetCurrentClassLogger();
    private readonly ILogRepository _repository;
    private bool _disposed;

    private static readonly Regex LogEntryPattern = new(
        @"^(\d{4}-\d{2}-\d{2} \d{2}:\d{2}:\d{2}\.\d{4})\s*\|\s*(\w+)\s*\|\s*(.*?)\s*\|\s*([.\w]+)\s*\|\s*(\d+)\s*\|\s*(\d+)?$",
        RegexOptions.Singleline);
    private static readonly Regex DateTimePattern = new(@"^\d{4}-\d{2}-\d{2} \d{2}:\d{2}:\d{2}\.\d{4}");

    public string SourceDescription => _repository.SourceDescription;

    public RepositoryLogSource(ILogRepository repository)
    {
        _repository = repository ?? throw new ArgumentNullException(nameof(repository));
        _log.Debug($"Создан RepositoryLogSource для источника: {repository.SourceDescription}");
    }

    public IEnumerable<ILogEntry> GetAll()
    {
        return GetAllAsync().ToEnumerable();
    }

    public IEnumerable<ILogEntry> GetNew()
    {
        return GetNewAsync().ToEnumerable();
    }

    [Obsolete("Use ClearAsync instead to prevent deadlocks")]
    public bool Clear()
    {
        if (!_repository.SupportsClear)
        {
            _log.Warn($"Источник {_repository.SourceDescription} не поддерживает очистку");
            return false;
        }

        // WARNING: This can cause deadlocks - use ClearAsync instead
        return _repository.ClearAsync().GetAwaiter().GetResult();
    }

    public async IAsyncEnumerable<ILogEntry> GetAllAsync([EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        _log.Trace($"Асинхронное получение всех записей из источника {_repository.SourceDescription}");
        
        var lines = _repository.ReadAllLinesAsync(cancellationToken);
        await foreach (var entry in ParseLogEntriesAsync(lines, cancellationToken))
        {
            yield return entry;
        }
    }

    public async IAsyncEnumerable<ILogEntry> GetNewAsync([EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        _log.Trace($"Асинхронное получение новых записей из источника {_repository.SourceDescription}");
        
        if (!_repository.SupportsIncrementalRead)
        {
            _log.Warn($"Источник {_repository.SourceDescription} не поддерживает инкрементальное чтение");
            yield break;
        }
        
        var lines = _repository.ReadNewLinesAsync(cancellationToken);
        await foreach (var entry in ParseLogEntriesAsync(lines, cancellationToken))
        {
            yield return entry;
        }
    }

    public async Task<bool> ClearAsync(CancellationToken cancellationToken = default)
    {
        if (!_repository.SupportsClear)
        {
            _log.Warn($"Источник {_repository.SourceDescription} не поддерживает очистку");
            return false;
        }

        return await _repository.ClearAsync(cancellationToken);
    }
    
    public async IAsyncEnumerable<ILogEntry> GetAllAsync(IProgressReporter progressReporter, [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        _log.Trace($"Асинхронное получение всех записей с прогрессом из источника {_repository.SourceDescription}");
        
        var lines = _repository.ReadAllLinesAsync(progressReporter, cancellationToken);
        await foreach (var entry in ParseLogEntriesAsync(lines, cancellationToken))
        {
            yield return entry;
        }
    }

    public async IAsyncEnumerable<ILogEntry> GetNewAsync(IProgressReporter progressReporter, [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        _log.Trace($"Асинхронное получение новых записей с прогрессом из источника {_repository.SourceDescription}");
        
        if (!_repository.SupportsIncrementalRead)
        {
            _log.Warn($"Источник {_repository.SourceDescription} не поддерживает инкрементальное чтение");
            yield break;
        }
        
        var lines = _repository.ReadNewLinesAsync(progressReporter, cancellationToken);
        await foreach (var entry in ParseLogEntriesAsync(lines, cancellationToken))
        {
            yield return entry;
        }
    }

    private async IAsyncEnumerable<ILogEntry> ParseLogEntriesAsync(
        IAsyncEnumerable<string> lines,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        var currentMessage = new StringBuilder();

        await foreach (var line in lines.WithCancellation(cancellationToken))
        {
            _log.Trace($"Обработка строки: {line}");

            // Добавляем новую строку к текущему сообщению
            if (currentMessage.Length > 0)
                currentMessage.AppendLine();
            else if (!DateTimePattern.Match(line).Success)
            {
                _log.Warn($"Строка не начинается с даты, пропускаем: {line}");
                continue;
            }
            
            currentMessage.Append(line);

            // Проверяем весь накопленный текст на соответствие паттерну
            string currentText = currentMessage.ToString().Trim();
            var match = LogEntryPattern.Match(currentText);
            
            if (match.Success)
            {
                _log.Trace($"Найдена полная запись: {currentText}");
                if (TryParseLogEntry(match, out var entry))
                {
                    yield return entry;
                }
                else
                {
                    _log.Error($"Ошибка парсинга записи: {currentText}");
                }
                currentMessage.Clear();
            }
        }

        // Проверяем остаток, если он есть
        if (currentMessage.Length > 0)
        {
            string finalText = currentMessage.ToString().Trim();
            var match = LogEntryPattern.Match(finalText);
            if (match.Success && TryParseLogEntry(match, out var entry))
            {
                _log.Trace($"Возвращаем последнюю запись: {finalText}");
                yield return entry;
            }
            else
            {
                _log.Warn($"Неполная запись в конце источника: {finalText}");
            }
        }
    }

    private bool TryParseLogEntry(Match match, out ILogEntry entry)
    {
        entry = null;
        
        if (!DateTime.TryParseExact(match.Groups[1].Value, "yyyy-MM-dd HH:mm:ss.ffff", 
            null, System.Globalization.DateTimeStyles.None, out DateTime parsedDateTime))
        {
            _log.Error($"Невозможно распарсить дату: {match.Groups[1].Value}");
            return false;
        }

        if (!Enum.TryParse(match.Groups[2].Value, true, out LogEntryType type))
        {
            _log.Error($"Ошибка при парсинге типа: {match.Groups[2].Value}");
            type = LogEntryType.Fatal;
        }

        string message = match.Groups[3].Value.Trim();
        string source = match.Groups[4].Value;
        int process = match.Groups[5].Success ? int.Parse(match.Groups[5].Value) : 0;
        int thread = match.Groups[6].Success ? int.Parse(match.Groups[6].Value) : 0;

        entry = new LogEntry(parsedDateTime, type, message, source, process, thread);
        return true;
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
            _log.Debug($"Освобождение ресурсов RepositoryLogSource");
            _repository?.Dispose();
        }

        _disposed = true;
    }
}

/// <summary>
/// Расширения для преобразования IAsyncEnumerable в IEnumerable
/// </summary>
internal static class AsyncEnumerableExtensions
{
    /// <summary>
    /// WARNING: Blocking sync conversion of IAsyncEnumerable - can cause deadlocks
    /// </summary>
    [Obsolete("This method can cause deadlocks. Use async enumeration where possible.")]
    public static IEnumerable<T> ToEnumerable<T>(this IAsyncEnumerable<T> source)
    {
        var enumerator = source.GetAsyncEnumerator();
        try
        {
            // WARNING: These blocking calls can cause deadlocks in UI thread
            while (enumerator.MoveNextAsync().GetAwaiter().GetResult())
            {
                yield return enumerator.Current;
            }
        }
        finally
        {
            enumerator.DisposeAsync().GetAwaiter().GetResult();
        }
    }
}