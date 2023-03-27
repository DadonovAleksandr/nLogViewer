using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using NLog;
using nLogViewer.Views;

namespace nLogViewer.Model;

internal class FileLogReader : ILogReader
{
    private static Logger log = LogManager.GetCurrentClassLogger();
    private readonly string _path;
    private int _count;

    public FileLogReader(string path)
    {
        log.Debug($"Вызов конструктора {GetType().Name} с параметрами: path - {path}");
        _path = path;
    }


    public IEnumerable<ILogEntry> GetAll()
    {
        log.Trace($"Получение всех записей из файла {_path}");

        string[] data = ReadLogFile().ToArray();
        _count = data.Length;
        var entries = ParseStringsToLogEntries(data);
        return entries;
    }
    
    public IEnumerable<ILogEntry> GetNew()
    {
        log.Trace($"Получение новых записей из файла {_path}");
        
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
        if (!File.Exists(_path))
        {
            log.Error($"Файл {_path} не существует");
            return Enumerable.Empty<string>();
        }
        FileInfo file = new FileInfo(_path);
        using var sr = new StreamReader(file.Open(FileMode.Open, FileAccess.Read, FileShare.ReadWrite));
        List<string> data = new List<string>();
        while (sr.ReadLine() is { } line)
        {
            data.Add(line);
        }
        return data;
    }
    
    private IEnumerable<ILogEntry> ParseStringsToLogEntries(IEnumerable<string> data)
    {
        var logEntries = new List<ILogEntry>();
        foreach (var item in data)
        {
            try
            {
                var parts = item.Split("|").Select(x=>x.Trim()).ToArray();
                var type = Enum.Parse<LogEntryType>(parts[1], true);
                logEntries.Add(new LogEntry(DateTime.Parse(parts[0]), type, parts[2], parts[3]));
            }
            catch (Exception e)
            {
                log.Error($"Ошибка при парсинге события: {e.Message}");
                new ErrorWindow($"Ошибка при парсинге события: {e.Message}").ShowDialog();
            }
            
        }
        return logEntries;
    }
    
    public override string ToString() => $"Объект чтения лога из файла {_path}";
    
}