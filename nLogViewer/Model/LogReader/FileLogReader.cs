using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using NLog;

namespace nLogViewer.Model;

internal class FileLogReader : ILogReader
{
    private static Logger _logger = LogManager.GetCurrentClassLogger();
    private readonly string _sourcePath;
    private readonly string _tempPath;
    private int _count;

    public FileLogReader(string path)
    {
        _logger.Debug($"Вызов конструктора {GetType().Name} с параметрами: path - {path}");
        _sourcePath = path;
        var destDir = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
        _tempPath = Path.Combine(destDir, "temp", Guid.NewGuid().ToString());
        if (!Directory.Exists(Path.Combine(destDir, "temp")))
            Directory.CreateDirectory(Path.Combine(destDir, "temp"));
    }


    public IEnumerable<ILogEntry> GetAll()
    {
        _logger.Debug($"Получение всех записей из файла {_sourcePath}");

        string[] data = ReadLogFile().ToArray();
        _count = data.Length;
        var entries = ParseStringsToLogEntries(data);
        return entries;
    }
    
    public IEnumerable<ILogEntry> GetNew()
    {
        _logger.Debug($"Получение новых записей из файла {_sourcePath}");
        
        string[] data = ReadLogFile().ToArray();
        if (data.Length > _count)
        {
            var newData = new string[data.Length - _count];
            Array.Copy(data, _count, newData, 0, data.Length - _count);
            var entries = ParseStringsToLogEntries(newData);
                
            _count = data.Length;
            return entries;
        }
        return Enumerable.Empty<ILogEntry>();
    }

    private IEnumerable<string> ReadLogFile()
    {
        if (!File.Exists(_sourcePath))
        {
            _logger.Error($"Файл {_sourcePath} не существует");
            return Enumerable.Empty<string>();
        }

        File.Copy(_sourcePath, _tempPath, true);
        return File.ReadAllLines(_tempPath);
    }
    
    private IEnumerable<ILogEntry> ParseStringsToLogEntries(IEnumerable<string> data)
    {
        var logEntries = new List<ILogEntry>();
        foreach (var item in data)
        {
            var parts = item.Split("|").Select(x=>x.Trim()).ToArray();
            logEntries.Add(new LogEntry(DateTime.Parse(parts[0]), ParseStringToLogEntryType(parts[1]), parts[2], parts[3]));
        }
        return logEntries;
    }

    private LogEntryType ParseStringToLogEntryType(string type)
    {
        return type.ToUpper() switch
        {
            "TRACE" => LogEntryType.Trace,
            "DEBUG" => LogEntryType.Debug,
            "INFO" => LogEntryType.Info,
            "WARN" => LogEntryType.Warn,
            "ERROR" => LogEntryType.Error,
            "FATAL" => LogEntryType.Fatal,
            _ => throw new ArgumentOutOfRangeException()
        };
    }

    public override string ToString()
    {
        return $"Объект чтения лога из файла {_sourcePath}";
    }
}