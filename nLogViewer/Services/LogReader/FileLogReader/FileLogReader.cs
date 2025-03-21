using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
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

    public FileLogReader(string path, IUserDialogService userDialogService)
    {
        _log.Debug($"Вызов конструктора {GetType().Name} с параметрами");
        _path = path;
        _userDialogService = userDialogService;
    }

    public IEnumerable<ILogEntry> GetAll()
    {
        _log.Trace($"Получение всех записей из файла {_path}");
        var lines = ReadLogFile().ToArray();
        _lineCount += lines.Length;
        return ParseLogEntries(lines);
    }

    public IEnumerable<ILogEntry> GetNew()
    {
        _log.Trace($"Получение новых записей из файла {_path}");
        var lines = ReadLogFile().ToArray();
        _lineCount += lines.Length;
        return ParseLogEntries(lines);
    }

    private IEnumerable<string> ReadLogFile()
    {
        if (!File.Exists(_path))
        {
            _log.Error($"Файл {_path} не существует");
            return Enumerable.Empty<string>();
        }

        FileInfo file = new FileInfo(_path);
        using var sr = new StreamReader(file.Open(FileMode.Open, FileAccess.Read, FileShare.ReadWrite));
        sr.BaseStream.Seek(_pos, SeekOrigin.Begin);
        List<string> lines = new List<string>();
        while (sr.ReadLine() is { } line)
        {
            lines.Add(line);
        }
        _pos = sr.BaseStream.Position;
        return lines;
    }

    private IEnumerable<ILogEntry> ParseLogEntries(IEnumerable<string> lines)
    {
        var logEntries = new List<ILogEntry>();
        StringBuilder currentMessage = new StringBuilder();
        DateTime? dateTime = null;
        LogEntryType type = LogEntryType.Fatal;
        string source = string.Empty;
        int process = 0;
        int thread = 0;

        foreach (var line in lines)
        {
            // Проверяем, начинается ли строка с даты (новая запись)
            if (DateTime.TryParse(line.Split('|')[0].Trim(), out DateTime parsedDateTime))
            {
                // Если уже есть накопленные данные, сохраняем предыдущую запись
                if (dateTime.HasValue)
                {
                    logEntries.Add(new LogEntry(
                        dateTime.Value,
                        type,
                        currentMessage.ToString().Trim(),
                        source,
                        process,
                        thread));
                }

                // Начинаем новую запись
                var parts = line.Split("|").Select(x => x.Trim()).ToArray();
                if (parts.Length < 4)
                {
                    _log.Error($"Ошибка при парсинге события: {line}");
                    _userDialogService.ShowError($"Ошибка при парсинге события: {line}", GetType().Name);
                    continue;
                }

                dateTime = parsedDateTime;
                if (!Enum.TryParse(parts[1], true, out type))
                {
                    _log.Error($"Ошибка при парсинге типа: {parts[1]}");
                    type = LogEntryType.Fatal; // Значение по умолчанию
                }
                currentMessage.Clear().Append(parts[2]);
                source = parts[3];
                process = parts.Length > 5 && int.TryParse(parts[parts.Length - 2], out int p) ? p : 0;
                thread = parts.Length > 5 && int.TryParse(parts[parts.Length - 1], out int t) ? t : 0;
            }
            else
            {
                // Это продолжение сообщения
                currentMessage.AppendLine(line);
            }
        }

        // Добавляем последнюю запись, если она есть
        if (dateTime.HasValue)
        {
            logEntries.Add(new LogEntry(
                dateTime.Value,
                type,
                currentMessage.ToString().Trim(),
                source,
                process,
                thread));
        }

        return logEntries;
    }

    public override string ToString() => $"Объект чтения лога из файла {_path}";
}