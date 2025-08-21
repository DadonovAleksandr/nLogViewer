using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using NLog;
using nLogViewer.Infrastructure.Extensions;
using nLogViewer.Model;
using nLogViewer.Services.Progress;
using nLogViewer.Services.UserDialogService;

namespace nLogViewer.Services.LogReader.FileLogReader;

internal class FileLogReader : ILogSource
{
    private static readonly Logger _log = LogManager.GetCurrentClassLogger();
    private readonly IUserDialogService _userDialogService;
    private readonly string _path;
    private long _pos;
    private int _lineCount;
    private bool _disposed;
    private static readonly Regex LogEntryPattern = new(
        @"^(\d{4}-\d{2}-\d{2} \d{2}:\d{2}:\d{2}\.\d{4})\s*\|\s*(\w+)\s*\|\s*(.*?)\s*\|\s*([.\w]+)\s*\|\s*(\d+)\s*\|\s*(\d+)?$",
        RegexOptions.Singleline);
    private static readonly Regex DateTimePattern = new(@"^\d{4}-\d{2}-\d{2} \d{2}:\d{2}:\d{2}\.\d{4}");

    public FileLogReader(string path, IUserDialogService userDialogService)
    {
        _log.Debug($"Вызов конструктора {GetType().Name} с параметрами");
        _userDialogService = userDialogService;
        if (string.IsNullOrEmpty(path))
        {
            _log.Error($"Не задан путь для файла лога");
            throw new ArgumentException("Не задан путь для файла лога", nameof(path));
        }
        _path = path;
    }

    public IEnumerable<ILogEntry> GetAll()
    {
        _log.Trace($"Получение всех записей из файла {_path}");
        return ParseLogEntries(ReadLogFile());
    }

    public IEnumerable<ILogEntry> GetNew()
    {
        _log.Trace($"Получение новых записей из файла {_path}");
        return ParseLogEntries(ReadLogFile());
    }

    public bool Clear()
    {
        _log.Trace($"Попытка очистки лог-файла {_path}");
        try
        {
            if (!File.Exists(_path))
            {
                _log.Warn($"Файл {_path} не существует, очистка не требуется");
                return true;
            }

            // Открываем файл с доступом для записи и обнуляем его содержимое
            using (var fs = new FileStream(_path, FileMode.Create, FileAccess.Write, FileShare.Read))
            {
                // Просто открытие в режиме Create автоматически очистит файл
                _log.Info($"Файл лога {_path} успешно очищен");
            }

            // Сбрасываем позицию чтения
            _pos = 0;
            _lineCount = 0;
            return true;
        }
        catch (UnauthorizedAccessException ex)
        {
            _log.Error(ex, $"Нет прав на очистку файла {_path}");
            // Критические ошибки операций FS показываем пользователю (не спам, редкие события)
            _userDialogService.ShowError($"Ошибка очистки: нет прав доступа к файлу {_path}", "Ошибка очистки лога");
            return false;
        }
        catch (IOException ex)
        {
            _log.Error(ex, $"Ошибка ввода-вывода при очистке файла {_path}");
            // Критические ошибки операций FS показываем пользователю (не спам, редкие события)
            _userDialogService.ShowError($"Ошибка очистки лог-файла: {ex.Message}", "Ошибка очистки лога");
            return false;
        }
        catch (Exception ex)
        {
            _log.Error(ex, $"Неизвестная ошибка при очистке файла {_path}");
            // Критические ошибки операций FS показываем пользователю (не спам, редкие события)
            _userDialogService.ShowError($"Неизвестная ошибка при очистке лога: {ex.Message}", "Ошибка очистки лога");
            return false;
        }
    }
    
    private IEnumerable<string> ReadLogFile()
    {
        if (!File.Exists(_path))
        {
            _log.Error($"Файл {_path} не существует");
            yield break;
        }

        var file = new FileInfo(_path);
        using var sr = new StreamReader(file.Open(FileMode.Open, FileAccess.Read, FileShare.ReadWrite));
        sr.BaseStream.Seek(_pos, SeekOrigin.Begin);

        while (sr.ReadLine() is { } line)
        {
            _lineCount++;
            _log.Trace($"Прочитана строка {_lineCount}: {line}");
            yield return line;
        }
        _pos = sr.BaseStream.Position;
        _log.Trace($"Обновлена позиция в файле: {_pos}");
    }
    
    private async IAsyncEnumerable<string> ReadLogFileAsync([EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        if (!File.Exists(_path))
        {
            _log.Error($"Файл {_path} не существует");
            yield break;
        }

        var file = new FileInfo(_path);
        using var sr = new StreamReader(file.Open(FileMode.Open, FileAccess.Read, FileShare.ReadWrite));
        sr.BaseStream.Seek(_pos, SeekOrigin.Begin);

        while (await sr.ReadLineAsync(cancellationToken).ConfigureAwait(false) is { } line)
        {
            cancellationToken.ThrowIfCancellationRequested();
            _lineCount++;
            _log.Trace($"Прочитана строка {_lineCount}: {line}");
            yield return line;
        }
        _pos = sr.BaseStream.Position;
        _log.Trace($"Обновлена позиция в файле: {_pos}");
    }

    private IEnumerable<ILogEntry> ParseLogEntries(IEnumerable<string> lines)
    {
        _log.Trace($"Парсинг записей из строк");
        var currentMessage = new StringBuilder();

        foreach (var line in lines)
        {
            _log.Trace($"Обработка строки: {line}");

            // Добавляем новую строку к текущему сообщению
            if (currentMessage.Length > 0)
                currentMessage.AppendLine();
            else
                if (!DateTimePattern.Match(line).Success)
                {
                    _log.Error($"Ошибка парсинга записи. Запись будет игнорирована: {line}");
                    // Убрано индивидуальное показание ошибок - используется агрегация
                    continue;
                }
            currentMessage.Append(line);

            // Проверяем весь накопленный текст на соответствие паттерну
            var currentText = currentMessage.ToString();
            var currentTextSpan = currentText.AsSpan().Trim();
            
            // Try fast parsing first (avoids regex overhead)
            if (TryParseLogEntryFast(currentTextSpan, out var fastEntry))
            {
                _log.Trace($"Найдена полная запись (быстрый парсинг): {currentTextSpan.ToString()}");
                yield return fastEntry;
                currentMessage.Clear();
            }
            else
            {
                // Fallback to regex parsing for complex multi-line entries
                var match = LogEntryPattern.Match(currentText.Trim());
                if (match.Success)
                {
                    _log.Trace($"Найдена полная запись (регекс): {currentText.Trim()}");
                    if (TryParseLogEntry(match, out var entry))
                    {
                        yield return entry;
                    }
                    else
                    {
                        _log.Error($"Ошибка парсинга записи: {currentText.Trim()}");
                        // Убрано индивидуальное показание ошибок парсинга - используется агрегация
                    }
                    currentMessage.Clear();
                }
                // Если нет соответствия, продолжаем накапливать строки
                else
                {
                    _log.Trace($"Строка добавлена к сообщению, ждем завершения: {line}");
                }
            }
        }

        // Проверяем остаток, если он есть
        if (currentMessage.Length > 0)
        {
            var finalText = currentMessage.ToString();
            var finalTextSpan = finalText.AsSpan().Trim();
            
            // Try fast parsing first
            if (TryParseLogEntryFast(finalTextSpan, out var fastEntry))
            {
                _log.Trace($"Возвращаем последнюю запись (быстрый парсинг): {finalTextSpan.ToString()}");
                yield return fastEntry;
            }
            else
            {
                // Fallback to regex parsing
                var match = LogEntryPattern.Match(finalText.Trim());
                if (match.Success && TryParseLogEntry(match, out var entry))
                {
                    _log.Trace($"Возвращаем последнюю запись (регекс): {finalText.Trim()}");
                    yield return entry;
                }
                else
                {
                    _log.Error($"Невалидный остаток лога: {finalText.Trim()}");
                    // Убрано индивидуальное показание ошибок парсинга - используется агрегация
                }
            }
        }
    }

    private bool TryParseLogEntry(Match match, out ILogEntry entry)
    {
        entry = null;
        
        // Use spans to avoid string allocations
        var dateTimeSpan = match.Groups[1].ValueSpan;
        if (!DateTime.TryParseExact(dateTimeSpan, "yyyy-MM-dd HH:mm:ss.ffff".AsSpan(), null, DateTimeStyles.None, out DateTime parsedDateTime))
        {
            _log.Error($"Невозможно распарсить дату: {match.Groups[1].Value}");
            return false;
        }

        // Avoid unnecessary DateTime construction - use parsed value directly
        var dateTime = parsedDateTime;

        var typeSpan = match.Groups[2].ValueSpan;
        if (!Enum.TryParse(typeSpan, true, out LogEntryType type))
        {
            _log.Error($"Ошибка при парсинге типа: {match.Groups[2].Value}");
            type = LogEntryType.Fatal;
        }

        // Use ValueSpan and trim without allocating new strings
        var messageSpan = match.Groups[3].ValueSpan.Trim();
        var message = messageSpan.ToString(); // Only allocate when necessary
        
        var sourceSpan = match.Groups[4].ValueSpan;
        var source = sourceSpan.ToString(); // Only allocate when necessary
        
        var process = match.Groups[5].Success ? int.Parse(match.Groups[5].ValueSpan) : 0;
        var thread = match.Groups[6].Success ? int.Parse(match.Groups[6].ValueSpan) : 0;

        entry = new LogEntry(dateTime, type, message, source, process, thread);
        return true;
    }

    internal bool TryParseLogEntryFast(ReadOnlySpan<char> logLine, out ILogEntry entry)
    {
        entry = null;
        
        // Fast path: parse without regex for better performance
        // Expected format: "2024-08-21 10:30:45.1234 | INFO | Message text | Source | 1234 | 5678"
        
        int pipeIndex1 = logLine.IndexOf('|');
        if (pipeIndex1 == -1) return false;
        
        // Extract and parse datetime
        var dateTimeSpan = logLine[..pipeIndex1].Trim();
        if (!DateTime.TryParseExact(dateTimeSpan, "yyyy-MM-dd HH:mm:ss.ffff".AsSpan(), null, DateTimeStyles.None, out DateTime dateTime))
        {
            return false;
        }
        
        // Find remaining pipe separators
        var remaining = logLine[(pipeIndex1 + 1)..];
        int pipeIndex2 = remaining.IndexOf('|');
        if (pipeIndex2 == -1) return false;
        
        // Extract and parse log level
        var levelSpan = remaining[..pipeIndex2].Trim();
        if (!Enum.TryParse(levelSpan, true, out LogEntryType type))
        {
            type = LogEntryType.Fatal;
        }
        
        remaining = remaining[(pipeIndex2 + 1)..];
        int pipeIndex3 = remaining.IndexOf('|');
        if (pipeIndex3 == -1) return false;
        
        // Extract message
        var messageSpan = remaining[..pipeIndex3].Trim();
        var message = messageSpan.ToString();
        
        remaining = remaining[(pipeIndex3 + 1)..];
        int pipeIndex4 = remaining.IndexOf('|');
        if (pipeIndex4 == -1) return false;
        
        // Extract source
        var sourceSpan = remaining[..pipeIndex4].Trim();
        var source = sourceSpan.ToString();
        
        remaining = remaining[(pipeIndex4 + 1)..];
        int pipeIndex5 = remaining.IndexOf('|');
        
        int process = 0, thread = 0;
        
        if (pipeIndex5 == -1)
        {
            // Only process ID, no thread ID
            var processSpan = remaining.Trim();
            if (!processSpan.IsEmpty)
            {
                int.TryParse(processSpan, out process);
            }
        }
        else
        {
            // Both process and thread IDs
            var processSpan = remaining[..pipeIndex5].Trim();
            if (!processSpan.IsEmpty)
            {
                int.TryParse(processSpan, out process);
            }
            
            var threadSpan = remaining[(pipeIndex5 + 1)..].Trim();
            if (!threadSpan.IsEmpty)
            {
                int.TryParse(threadSpan, out thread);
            }
        }
        
        entry = new LogEntry(dateTime, type, message, source, process, thread);
        return true;
    }

    public async IAsyncEnumerable<ILogEntry> GetAllAsync([EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        _log.Trace($"Асинхронное получение всех записей из файла {_path}");
        await foreach (var entry in ParseLogEntriesAsync(ReadLogFileAsync(cancellationToken), cancellationToken).ConfigureAwait(false))
        {
            yield return entry;
        }
    }
    
    public async IAsyncEnumerable<ILogEntry> GetAllAsync(IProgressReporter progressReporter, [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        _log.Trace($"Асинхронное получение всех записей с прогрессом из файла {_path}");
        
        // Получаем размер файла для расчёта прогресса
        long fileSize = 0;
        if (File.Exists(_path))
        {
            var fileInfo = new FileInfo(_path);
            fileSize = fileInfo.Length;
        }
        
        long processedBytes = 0;
        var processedEntries = 0;
        
        // Отправляем начальный прогресс
        progressReporter?.Report(0, fileSize, "Начинаем обработку файла...");
        
        await foreach (var line in ReadLogFileAsync(cancellationToken, progressReporter).ConfigureAwait(false))
        {
            await foreach (var entry in ParseLogEntriesAsync(new[] { line }.ToAsyncEnumerable(), cancellationToken).ConfigureAwait(false))
            {
                processedEntries++;
                
                // Отчитываемся каждые 100 записей
                if (processedEntries % 100 == 0)
                {
                    progressReporter?.Report(processedBytes, fileSize, $"Обработано {processedEntries} записей");
                }
                
                yield return entry;
            }
            
            processedBytes += Encoding.UTF8.GetByteCount(line) + Environment.NewLine.Length;
        }
        
        // Отчитываемся о завершении
        progressReporter?.Report(fileSize, fileSize, $"Завершено. Обработано {processedEntries} записей");
    }

    public async IAsyncEnumerable<ILogEntry> GetNewAsync([EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        _log.Trace($"Асинхронное получение новых записей из файла {_path}");
        await foreach (var entry in ParseLogEntriesAsync(ReadLogFileAsync(cancellationToken), cancellationToken).ConfigureAwait(false))
        {
            yield return entry;
        }
    }
    
    public async IAsyncEnumerable<ILogEntry> GetNewAsync(IProgressReporter progressReporter, [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        _log.Trace($"Асинхронное получение новых записей с прогрессом из файла {_path}");
        
        int processedEntries = 0;
        await foreach (var entry in ParseLogEntriesAsync(ReadLogFileAsync(cancellationToken), cancellationToken).ConfigureAwait(false))
        {
            processedEntries++;
            
            // Отчитываемся каждые 10 записей (чаще, так как новых обычно меньше)
            if (processedEntries % 10 == 0)
            {
                progressReporter?.ReportPercentage(50, $"Обработано {processedEntries} новых записей");
            }
            
            yield return entry;
        }
        
        // Отчитываемся о завершении
        progressReporter?.ReportPercentage(100, $"Завершено. Обработано {processedEntries} новых записей");
    }

    public async Task<bool> ClearAsync(CancellationToken cancellationToken = default)
    {
        _log.Trace($"Асинхронная попытка очистки лог-файла {_path}");
    
        try
        {
            if (!File.Exists(_path))
            {
                _log.Warn($"Файл {_path} не существует, очистка не требуется");
                return true;
            }

            // Открываем файл с доступом для записи и обнуляем его содержимое
            await using (var fs = new FileStream(_path, FileMode.Create, FileAccess.Write, FileShare.Read))
            {
                await fs.FlushAsync(cancellationToken).ConfigureAwait(false);
                _log.Info($"Файл лога {_path} успешно очищен");
            }

            // Сбрасываем позицию чтения
            _pos = 0;
            _lineCount = 0;
        
            return true;
        }
        catch (UnauthorizedAccessException ex)
        {
            _log.Error(ex, $"Нет прав на очистку файла {_path}");
            // Критические ошибки операций FS показываем пользователю (не спам, редкие события)
            _userDialogService.ShowError($"Ошибка очистки: нет прав доступа к файлу {_path}", "Ошибка очистки лога");
            return false;
        }
        catch (IOException ex)
        {
            _log.Error(ex, $"Ошибка ввода-вывода при очистке файла {_path}");
            // Критические ошибки операций FS показываем пользователю (не спам, редкие события)
            _userDialogService.ShowError($"Ошибка очистки лог-файла: {ex.Message}", "Ошибка очистки лога");
            return false;
        }
        catch (Exception ex)
        {
            _log.Error(ex, $"Неизвестная ошибка при очистке файла {_path}");
            // Критические ошибки операций FS показываем пользователю (не спам, редкие события)
            _userDialogService.ShowError($"Неизвестная ошибка при очистке лога: {ex.Message}", "Ошибка очистки лога");
            return false;
        }
    }
    
    private async IAsyncEnumerable<string> ReadLogFileAsync([EnumeratorCancellation] CancellationToken cancellationToken = default, IProgressReporter progressReporter = null)
    {
        if (!File.Exists(_path))
        {
            _log.Debug($"Файл лога не найден {_path}");
            yield break;
        }

        FileStream fs = null;
        StreamReader sr = null;
        
        try
        {
            fs = new FileStream(_path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
            sr = new StreamReader(fs, Encoding.UTF8);

            // Переходим к сохранённой позиции
            if (_pos > 0 && fs.Length >= _pos)
            {
                fs.Seek(_pos, SeekOrigin.Begin);
            }
        }
        catch (Exception ex)
        {
            _log.Error(ex, $"Ошибка открытия файла {_path}");
            // Критические ошибки открытия файла показываем пользователю (не спам, редкие события)
            _userDialogService.ShowError($"Ошибка открытия файла лога: {ex.Message}", "Ошибка");
            sr?.Dispose();
            fs?.Dispose();
            throw;
        }

        try
        {
            while (await sr.ReadLineAsync(cancellationToken).ConfigureAwait(false) is { } line)
            {
                cancellationToken.ThrowIfCancellationRequested();
                
                _lineCount++;
                yield return line;
                
                // Отчитываемся о прогрессе каждые 1000 строк
                if (progressReporter != null && _lineCount % 1000 == 0)
                {
                    progressReporter.Report(fs.Position, fs.Length, $"Обработано {_lineCount} строк");
                }
            }

            // Сохраняем позицию для следующего чтения
            _pos = fs.Position;
            _log.Trace($"Прочитано {_lineCount} строк из файла {_path}, новая позиция: {_pos}");
        }
        finally
        {
            sr?.Dispose();
            fs?.Dispose();
        }
    }
    
    private async IAsyncEnumerable<ILogEntry> ParseLogEntriesAsync(IAsyncEnumerable<string> lines, [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        _log.Trace($"Асинхронный парсинг записей из строк");
        var currentMessage = new StringBuilder();

        await foreach (var line in lines.WithCancellation(cancellationToken).ConfigureAwait(false))
        {
            _log.Trace($"Обработка строки: {line}");

            // Добавляем новую строку к текущему сообщению
            if (currentMessage.Length > 0)
                currentMessage.AppendLine();
            else
                if (!DateTimePattern.Match(line).Success)
                {
                    _log.Error($"Ошибка парсинга записи. Запись будет игнорирована: {line}");
                    // Убрано индивидуальное показание ошибок - используется агрегация
                    continue;
                }
            currentMessage.Append(line);

            // Проверяем весь накопленный текст на соответствие паттерну
            var currentText = currentMessage.ToString();
            var currentTextSpan = currentText.AsSpan().Trim();
            
            // Try fast parsing first (avoids regex overhead)
            if (TryParseLogEntryFast(currentTextSpan, out var fastEntry))
            {
                _log.Trace($"Найдена полная запись (быстрый парсинг): {currentTextSpan.ToString()}");
                yield return fastEntry;
                currentMessage.Clear();
            }
            else
            {
                // Fallback to regex parsing for complex multi-line entries
                var match = LogEntryPattern.Match(currentText.Trim());
                if (match.Success)
                {
                    _log.Trace($"Найдена полная запись (регекс): {currentText.Trim()}");
                    if (TryParseLogEntry(match, out var entry))
                    {
                        yield return entry;
                    }
                    else
                    {
                        _log.Error($"Ошибка парсинга записи: {currentText.Trim()}");
                        // Убрано индивидуальное показание ошибок парсинга - используется агрегация
                    }
                    currentMessage.Clear();
                }
                // Если нет соответствия, продолжаем накапливать строки
                else
                {
                    _log.Trace($"Строка добавлена к сообщению, ждем завершения: {line}");
                }
            }
        }

        // Проверяем остаток, если он есть
        if (currentMessage.Length > 0)
        {
            var finalText = currentMessage.ToString();
            var finalTextSpan = finalText.AsSpan().Trim();
            
            // Try fast parsing first
            if (TryParseLogEntryFast(finalTextSpan, out var fastEntry))
            {
                _log.Trace($"Возвращаем последнюю запись (быстрый парсинг): {finalTextSpan.ToString()}");
                yield return fastEntry;
            }
            else
            {
                // Fallback to regex parsing
                var match = LogEntryPattern.Match(finalText.Trim());
                if (match.Success && TryParseLogEntry(match, out var entry))
                {
                    _log.Trace($"Возвращаем последнюю запись (регекс): {finalText.Trim()}");
                    yield return entry;
                }
                else
                {
                    _log.Error($"Невалидный остаток лога: {finalText.Trim()}");
                    // Убрано индивидуальное показание ошибок парсинга - используется агрегация
                }
            }
        }
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
            _log.Debug($"Освобождение ресурсов FileLogReader для файла {_path}");
            // Здесь можно освободить управляемые ресурсы, если они будут добавлены в будущем
        }
        
        _disposed = true;
    }

    public override string ToString() => $"Объект чтения лога из файла {_path}";
}