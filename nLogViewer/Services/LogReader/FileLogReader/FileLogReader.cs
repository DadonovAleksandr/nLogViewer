using System;
using System.Collections.Generic;
using System.IO;
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
    private static readonly Regex LogEntryPattern = new(@"^(\d{4}-\d{2}-\d{2} \d{2}:\d{2}:\d{2}\.\d{4})\s*\|\s*(\w+)\s*\|\s*(.*?)\s*\|\s*(\w+)(?:\s*\|\s*(\d+)\s*\|\s*(\d+))?$");

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

        string? line;
        while ((line = sr.ReadLine()) != null)
        {
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

    public override string ToString() => $"Объект чтения лога из файла {_path}";
}