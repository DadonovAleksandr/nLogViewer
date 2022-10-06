using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using NLog;
using nLogViewer.Infrastructure.Helpers.FileProvider;

namespace nLogViewer.Model.AppSettings.RecentLogs;

public class RecentLogsFileRepository : IRecentLogsRepository
{
    private static Logger _logger = LogManager.GetCurrentClassLogger();
    
    private readonly string _path;
    private IFileProvider<RecentLogEntries> _fileProvider;
    private RecentLogEntries _entries;

    public RecentLogsFileRepository(string filePath, IFileProvider<RecentLogEntries> fileProvider)
    {
        _logger.Debug($"Вызов конструктора {GetType().Name} с параметрами (path: \"{filePath}\", fileProvider: \"{fileProvider}\")");
        _path = filePath;
        _fileProvider = fileProvider;

        Load();
    }
    
    public void Add(string path, bool isFolder = false)
    {
        _logger.Debug($"Добавление записи в список ранее открытых логов (path: \"{path}\", isFolder: \"{isFolder}\")");
        _entries.Add(new RecentLogEntry(path, isFolder));
    }
    public bool Remove(RecentLogEntry entry)
    {
        _logger.Debug($"Удаление записи в списке ранее открытых логов (path: \"{entry.Path}\", isFolder: \"{entry.IsFolder}\")");
        return _entries.Remove(entry);
    }
    public void Clear()
    {
        _logger.Debug($"Очистка всех записей списка ранее открытых логов");
        _entries.Clear();
    }

    public RecentLogEntry this[int index] { get => _entries[index]; }
    
    /// <summary>
    /// Сохранение списка логов
    /// </summary>
    public void Save()
    {
        _logger.Debug($"Сохранения списка ранее открытых логов в файл {_path}");
        try
        {
            var dirPath = Path.GetDirectoryName(_path);
            if (!Directory.Exists(dirPath))
            {
                _logger.Warn($"Директория {dirPath} не существует. Запуск процедуры автоматического создания.");
                Directory.CreateDirectory(dirPath);
            }
            _fileProvider.Save(_entries, _path);
        }
        catch (Exception e)
        {
            _logger.Error($"При сохранении файла {_path} было вызвано исключение: {e.Message}");
        }
    }

    /// <summary>
    /// Загрузка списка логов
    /// </summary>
    private bool Load()
    {
        _logger.Debug($"Загрузка списка ранее открытых логов из файла {_path}");
        try
        {
            if (!File.Exists(_path))
            {
                _logger.Warn($"Файл {_path} не существует. Загружен пустой список ранее открытых логов");
                _entries = new RecentLogEntries();
                return false;
            }
            _entries = _fileProvider.Load(_path);
        }
        catch (Exception e)
        {
            _logger.Error($"При чтении файла {_path} было вызвано исключение: {e.Message}");
            _entries = new RecentLogEntries();
            return false;
        }

        return true;
    }

    public IEnumerator<RecentLogEntry> GetEnumerator()
    {
        foreach (var entry in _entries)
        {
            yield return entry;
        }
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        return GetEnumerator();
    }
}