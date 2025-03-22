using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using NLog;
using nLogViewer.Model;
using nLogViewer.Services.UserDialogService;

namespace nLogViewer.Services.LogReader.FileLogReader;

internal class FileLogReader : ILogReader
{
    private static readonly Logger _log = LogManager.GetCurrentClassLogger();
    private readonly IUserDialogService _userDialogService;
    private readonly string _path;
    private long _pos;
    private int _lineCount;
    private static readonly Regex LogEntryPattern = new Regex(@"^(\d{4}-\d{2}-\d{2} \d{2}:\d{2}:\d{2}\.\d{3})\s*\|\s*(\w+)\s*\|\s*(.*?)\s*\|\s*(\w+)(?:\s*\|\s*(\d+)\s*\|\s*(\d+))?$");

    public FileLogReader(string path, IUserDialogService userDialogService)
    {
        _log.Debug($"Вызов конструктора {GetType().Name} с параметрами");
        _path = path;
        _userDialogService = userDialogService;
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

    private IEnumerable<string> ReadLogFile()
    {
        if (!File.Exists(_path))
        {
            _log.Error($"Файл {_path} не существует");
            yield break;
        }

        FileInfo file = new FileInfo(_path);
        using var sr = new StreamReader(file.Open(FileMode.Open, FileAccess.Read, FileShare.ReadWrite));
        sr.BaseStream.Seek(_pos, SeekOrigin.Begin);

        string? line;
        while ((line = sr.ReadLine()) != null)
        {
            _lineCount++;
            _log.Trace($"Прочитана строка {_lineCount}: {line}");
            if (!string.IsNullOrWhiteSpace(line))
            {
                yield return line;
            }
        }
        _pos = sr.BaseStream.Position;
        _log.Trace($"Обновлена позиция в файле: {_pos}");
    }

    private IEnumerable<ILogEntry> ParseLogEntries(IEnumerable<string> lines)
    {
        StringBuilder currentMessage = new StringBuilder();
        DateTime? dateTime = null;
        LogEntryType type = LogEntryType.Fatal;
        string source = string.Empty;
        int process = 0;
        int thread = 0;

        foreach (var line in lines)
        {
            _log.Trace($"Обработка строки: {line}");
            
            var match = LogEntryPattern.Match(line);
            if (match.Success)
            {
                _log.Trace($"Найдено начало нового сообщения: {line}");
                
                // Если есть накопленные данные, возвращаем предыдущую запись
                if (dateTime.HasValue)
                {
                    var entry = new LogEntry(
                        dateTime.Value,
                        type,
                        currentMessage.ToString().Trim(),
                        source,
                        process,
                        thread);
                    _log.Trace($"Возвращаем запись: {entry}");
                    yield return entry;
                    currentMessage.Clear();
                }

                // Начинаем новую запись
                if (DateTime.TryParseExact(match.Groups[1].Value, "yyyy-MM-dd HH:mm:ss.fff", null, System.Globalization.DateTimeStyles.None, out DateTime parsedDateTime))
                {
                    dateTime = parsedDateTime;
                    if (!Enum.TryParse(match.Groups[2].Value, true, out type))
                    {
                        _log.Error($"Ошибка при парсинге типа: {match.Groups[2].Value}");
                        type = LogEntryType.Fatal;
                    }
                    currentMessage.Append(match.Groups[3].Value);
                    source = match.Groups[4].Value;
                    process = match.Groups[5].Success ? int.Parse(match.Groups[5].Value) : 0;
                    thread = match.Groups[6].Success ? int.Parse(match.Groups[6].Value) : 0;
                    _log.Trace($"Начата новая запись: {dateTime}, {type}, {currentMessage}, {source}, {process}, {thread}");
                }
                else
                {
                    _log.Error($"Невозможно распарсить дату: {match.Groups[1].Value}");
                    _userDialogService.ShowError($"Невозможно распарсить дату: {match.Groups[1].Value}", GetType().Name);
                }
            }
            else if (dateTime.HasValue)
            {
                // Продолжение сообщения
                if (currentMessage.Length > 0)
                    currentMessage.AppendLine();
                currentMessage.Append(line);
                _log.Trace($"Добавлена строка к текущему сообщению: {line}");
            }
            else
            {
                _log.Error($"Строка не соответствует формату лога: {line}");
                _userDialogService.ShowError($"Строка не соответствует формату лога: {line}", GetType().Name);
            }
        }

        // Возвращаем последнюю запись, если она есть
        if (dateTime.HasValue)
        {
            var entry = new LogEntry(
                dateTime.Value,
                type,
                currentMessage.ToString().Trim(),
                source,
                process,
                thread);
            _log.Trace($"Возвращаем последнюю запись: {entry}");
            yield return entry;
        }
    }

    public override string ToString() => $"Объект чтения лога из файла {_path}";
}