using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using NLog;
using nLogViewer.Model;
using nLogViewer.Services.UserDialogService;
using nLogViewer.Views;

namespace nLogViewer.Services.LogReader;

internal class FileLogReader : ILogReader
{
    private static readonly Logger _log = LogManager.GetCurrentClassLogger();
    private readonly IUserDialogService _userDialogService;
    private readonly string _path;
    private int _count;

    public FileLogReader(string path, IUserDialogService userDialogService)
    {
        _log.Debug($"Вызов конструктора {GetType().Name} с параметрами: path - {path}");
        _path = path;
        _userDialogService = userDialogService;
    }
    
    public IEnumerable<ILogEntry> GetAll()
    {
        _userDialogService.ShowError("Tect", GetType().Name);
        _log.Trace($"Получение всех записей из файла {_path}");
        string[] data = ReadLogFile().ToArray();
        _count = data.Length;
        return ConvertStringsLogToLogEntries(data);
    }
    
    public IEnumerable<ILogEntry> GetNew()
    {
        _log.Trace($"Получение новых записей из файла {_path}");
        
        string[] data = ReadLogFile().ToArray();
        if (data.Length > _count)
        {
            var newData = new string[data.Length - _count];
            Array.Copy(data, _count, newData, 0, data.Length - _count);
            var entries = ConvertStringsLogToLogEntries(newData);
                
            _count = data.Length;
            return entries;
        }
        return Enumerable.Empty<ILogEntry>();
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
        while (sr.ReadLine() is { } line)
        {
            data.Add(line);
        }
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
        string message = String.Empty; 
        string source = String.Empty;
        int process = 0;
        int thread = 0;
        
        var parts = str.Split("|").Select(x => x.Trim()).ToArray();
        if (parts.Length < 4)
        {
            _log.Error($"Ошибка при парсинге события: {str}");
            _userDialogService.ShowError($"Ошибка при парсинге события: {str}", GetType().Name);
            //new ErrorWindow($"Ошибка при парсинге события: {str}").ShowDialog();    
            return new LogEntry(dateTime, type, message, source, process, thread);
        }
        if (!DateTime.TryParse(parts[0], out dateTime))
        {
            _log.Error($"Ошибка при парсинге события: не возможно преобразовать {parts[0]} в DateTime");
            _userDialogService.ShowError($"Ошибка при парсинге события: не возможно преобразовать {parts[0]} в DateTime", GetType().Name);
            //new ErrorWindow($"Ошибка при парсинге события: не возможно преобразовать {parts[0]} в DateTime").ShowDialog();
        }
        if (!Enum.TryParse<LogEntryType>(parts[1], true, out type))
        {
            _log.Error($"Ошибка при парсинге события: не возможно преобразовать {parts[1]} в LogEntryType");
            _userDialogService.ShowError($"Ошибка при парсинге события: не возможно преобразовать {parts[1]} в LogEntryType", GetType().Name);
            //new ErrorWindow($"Ошибка при парсинге события: не возможно преобразовать {parts[1]} в LogEntryType").ShowDialog();
        }
        message = parts[2];
        source = parts[3];
        if (parts.Length > 4)
        {
            if (!int.TryParse(parts[4], out process))
            {
                _log.Error($"Ошибка при парсинге события: не возможно преобразовать {parts[4]} в Int");
                _userDialogService.ShowError($"Ошибка при парсинге события: не возможно преобразовать {parts[4]} в Int", GetType().Name);
                //new ErrorWindow($"Ошибка при парсинге события: не возможно преобразовать {parts[4]} в Int").ShowDialog();
            }
        }
        if (parts.Length > 5)
        {
            if (!int.TryParse(parts[5], out thread))
            {
                _log.Error($"Ошибка при парсинге события: не возможно преобразовать {parts[5]} в Int");
                _userDialogService.ShowError($"Ошибка при парсинге события: не возможно преобразовать {parts[5]} в Int", GetType().Name);
                //new ErrorWindow($"Ошибка при парсинге события: не возможно преобразовать {parts[5]} в Int").ShowDialog();
            }
        }
        
        return new LogEntry(dateTime, type, message, source, process, thread);
    }
    
    public override string ToString() => $"Объект чтения лога из файла {_path}";
}