using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
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
        string[] data = ReadLogFile().ToArray();
        _lineCount += data.Length;
        return ConvertStringsLogToLogEntries(data);
    }

    public IEnumerable<ILogEntry> GetNew()
    {
        _log.Trace($"Получение новых записей из файла {_path}");

        string[] data = ReadLogFile().ToArray();
        _lineCount += data.Length;
        return ConvertStringsLogToLogEntries(data);
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
        List<string> data = new List<string>();
        sr.BaseStream.Seek(_pos, SeekOrigin.Begin);
        while (sr.ReadLine() is { } line)
        {
            data.Add(line);
        }
        _pos = sr.BaseStream.Position;
        return data;
    }

    private IEnumerable<ILogEntry> ConvertStringsLogToLogEntries(IEnumerable<string> data)
    {
        var logEntries = new List<ILogEntry>();
        foreach (var item in data)
        {
            logEntries.Add(ParseStringToLogEntry(item));
        }
        return logEntries;
    }

    private ILogEntry ParseStringToLogEntry(string str)
    {
        DateTime dateTime = new DateTime();
        LogEntryType type = LogEntryType.Fatal;
        string message = string.Empty;
        string source = string.Empty;
        int process = 0;
        int thread = 0;

        var parts = str.Split("|").Select(x => x.Trim()).ToArray();
        if (parts.Length < 4)
        {
            _log.Error($"Ошибка при парсинге события: {str}");
            _userDialogService.ShowError($"Ошибка при парсинге события: {str}", GetType().Name);
            return new LogEntry(dateTime, type, message, source, process, thread);
        }
        if (!DateTime.TryParse(parts[0], out dateTime))
        {
            _log.Error($"Ошибка при парсинге события: не возможно преобразовать {parts[0]} в DateTime");
            _userDialogService.ShowError($"Ошибка при парсинге события: не возможно преобразовать {parts[0]} в DateTime", GetType().Name);
        }
        if (!Enum.TryParse(parts[1], true, out type))
        {
            _log.Error($"Ошибка при парсинге события: не возможно преобразовать {parts[1]} в LogEntryType");
            _userDialogService.ShowError($"Ошибка при парсинге события: не возможно преобразовать {parts[1]} в LogEntryType", GetType().Name);
        }
        message = parts[2];
        source = parts[3];
        if(parts.Length > 5)
        {
            if (!int.TryParse(parts[parts.Length - 2], out process))
            {
                _log.Error($"Ошибка при парсинге события: не возможно преобразовать {parts[4]} в Int");
                _userDialogService.ShowError($"Ошибка при парсинге события: не возможно преобразовать {parts[4]} в Int", GetType().Name);
            }
            if (!int.TryParse(parts[parts.Length - 1], out thread))
            {
                _log.Error($"Ошибка при парсинге события: не возможно преобразовать {parts[5]} в Int");
                _userDialogService.ShowError($"Ошибка при парсинге события: не возможно преобразовать {parts[5]} в Int", GetType().Name);
            }
        }
        
        return new LogEntry(dateTime, type, message, source, process, thread);
    }

    public override string ToString() => $"Объект чтения лога из файла {_path}";
}