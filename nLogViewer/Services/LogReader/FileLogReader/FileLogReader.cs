using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
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
            _userDialogService.ShowError($"Ошибка очистки: нет прав доступа к файлу {_path}", "Ошибка очистки лога");
            return false;
        }
        catch (IOException ex)
        {
            _log.Error(ex, $"Ошибка ввода-вывода при очистке файла {_path}");
            _userDialogService.ShowError($"Ошибка очистки лог-файла: {ex.Message}", "Ошибка очистки лога");
            return false;
        }
        catch (Exception ex)
        {
            _log.Error(ex, $"Неизвестная ошибка при очистке файла {_path}");
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

        string line;
        while ((line = sr.ReadLine()) != null)
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

        string line;
        while ((line = await sr.ReadLineAsync().ConfigureAwait(false)) != null)
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
        StringBuilder currentMessage = new StringBuilder();

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
                    _userDialogService.ShowError($"Ошибка парсинга записи. Запись будет игнорирована: {line}", GetType().Name);
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
                    _userDialogService.ShowError($"Ошибка парсинга записи: {currentText}", GetType().Name);
                }
                currentMessage.Clear();
            }
            // Если нет соответствия, продолжаем накапливать строки
            else
            {
                _log.Trace($"Строка добавлена к сообщению, ждем завершения: {line}");
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
                _log.Error($"Невалидный остаток лога: {finalText}");
                _userDialogService.ShowError($"Невалидный остаток лога: {finalText}", GetType().Name);
            }
        }
    }

    private bool TryParseLogEntry(Match match, out ILogEntry entry)
    {
        entry = null;
        if (!DateTime.TryParseExact(match.Groups[1].Value, "yyyy-MM-dd HH:mm:ss.ffff", null, System.Globalization.DateTimeStyles.None, out DateTime parsedDateTime))
        {
            _log.Error($"Невозможно распарсить дату: {match.Groups[1].Value}");
            return false;
        }

        DateTime dateTime = new DateTime(
            parsedDateTime.Year,
            parsedDateTime.Month,
            parsedDateTime.Day,
            parsedDateTime.Hour,
            parsedDateTime.Minute,
            parsedDateTime.Second,
            parsedDateTime.Millisecond);

        if (!Enum.TryParse(match.Groups[2].Value, true, out LogEntryType type))
        {
            _log.Error($"Ошибка при парсинге типа: {match.Groups[2].Value}");
            type = LogEntryType.Fatal;
        }

        string message = match.Groups[3].Value.Trim();
        string source = match.Groups[4].Value;
        int process = match.Groups[5].Success ? int.Parse(match.Groups[5].Value) : 0;
        int thread = match.Groups[6].Success ? int.Parse(match.Groups[6].Value) : 0;

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
        int processedEntries = 0;
        
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
            using (var fs = new FileStream(_path, FileMode.Create, FileAccess.Write, FileShare.Read))
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
            _userDialogService.ShowError($"Ошибка очистки: нет прав доступа к файлу {_path}", "Ошибка очистки лога");
            return false;
        }
        catch (IOException ex)
        {
            _log.Error(ex, $"Ошибка ввода-вывода при очистке файла {_path}");
            _userDialogService.ShowError($"Ошибка очистки лог-файла: {ex.Message}", "Ошибка очистки лога");
            return false;
        }
        catch (Exception ex)
        {
            _log.Error(ex, $"Неизвестная ошибка при очистке файла {_path}");
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
            _userDialogService.ShowError($"Ошибка открытия файла лога: {ex.Message}", "Ошибка");
            sr?.Dispose();
            fs?.Dispose();
            throw;
        }

        try
        {
            string line;
            while ((line = await sr.ReadLineAsync(cancellationToken).ConfigureAwait(false)) != null)
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
        StringBuilder currentMessage = new StringBuilder();

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
                    _userDialogService.ShowError($"Ошибка парсинга записи. Запись будет игнорирована: {line}", GetType().Name);
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
                    _userDialogService.ShowError($"Ошибка парсинга записи: {currentText}", GetType().Name);
                }
                currentMessage.Clear();
            }
            // Если нет соответствия, продолжаем накапливать строки
            else
            {
                _log.Trace($"Строка добавлена к сообщению, ждем завершения: {line}");
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
                _log.Error($"Невалидный остаток лога: {finalText}");
                _userDialogService.ShowError($"Невалидный остаток лога: {finalText}", GetType().Name);
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