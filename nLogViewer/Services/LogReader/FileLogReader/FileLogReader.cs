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
            
            // Разбиваем строку на части
            var parts = line.Split('|', StringSplitOptions.TrimEntries);
            if (parts.Length < 1)
            {
                _log.Error($"Строка не содержит разделитель '|': {line}");
                _userDialogService.ShowError($"Строка не содержит разделитель '|': {line}", GetType().Name);
                continue;
            }

            // Проверяем, начинается ли строка с даты (новая запись)
            if (TryParseDateTime(parts[0], out DateTime parsedDateTime))
            {
                _log.Trace($"Найдена дата: {parsedDateTime}");
                
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
            else if (dateTime.HasValue)
            {
                // Продолжение сообщения
                if (currentMessage.Length > 0)
                    currentMessage.AppendLine();
                currentMessage.Append(line);
            }
            else
            {
                _log.Error($"Невозможно распарсить строку: {line}");
                _userDialogService.ShowError($"Невозможно распарсить строку: {line}", GetType().Name);
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
        // Пробуем сначала с четырьмя цифрами миллисекунд
        if (DateTime.TryParseExact(input, "yyyy-MM-dd HH:mm:ss.ffff", null, System.Globalization.DateTimeStyles.None, out result))
        {
            // Округляем миллисекунды до трех цифр
            result = new DateTime(
                result.Year,
                result.Month,
                result.Day,
                result.Hour,
                result.Minute,
                result.Second,
                result.Millisecond);
            return true;
        }
        return DateTime.TryParseExact(input, "yyyy-MM-dd HH:mm:ss.fff", null, System.Globalization.DateTimeStyles.None, out result);
    }

    public override string ToString() => $"Объект чтения лога из файла {_path}";
}