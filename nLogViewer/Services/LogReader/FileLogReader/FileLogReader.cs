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
            yield return line;
        }
        _pos = sr.BaseStream.Position;
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
            // Проверяем, начинается ли строка с даты (новая запись)
            if (TryParseDateTime(line.Split('|')[0].Trim(), out DateTime parsedDateTime))
            {
                // Если есть накопленные данные, возвращаем предыдущую запись
                if (dateTime.HasValue)
                {
                    yield return new LogEntry(
                        dateTime.Value,
                        type,
                        currentMessage.ToString().Trim(),
                        source,
                        process,
                        thread);
                }

                // Начинаем новую запись
                var parts = line.Split("|", StringSplitOptions.TrimEntries);
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
                    type = LogEntryType.Fatal;
                }
                currentMessage.Clear().Append(parts[2]);
                source = parts[3];
                process = parts.Length > 5 && int.TryParse(parts[^2], out int p) ? p : 0;
                thread = parts.Length > 5 && int.TryParse(parts[^1], out int t) ? t : 0;
            }
            else
            {
                // Продолжение сообщения
                currentMessage.AppendLine(line);
            }
        }

        // Возвращаем последнюю запись, если она есть
        if (dateTime.HasValue)
        {
            yield return new LogEntry(
                dateTime.Value,
                type,
                currentMessage.ToString().Trim(),
                source,
                process,
                thread);
        }
    }

    private static bool TryParseDateTime(string input, out DateTime result)
    {
        return DateTime.TryParse(input, out result);
    }

    public override string ToString() => $"Объект чтения лога из файла {_path}";
}