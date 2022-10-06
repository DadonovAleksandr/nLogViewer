using System.Collections.Generic;

namespace nLogViewer.Model.AppSettings.RecentLogs;

public interface IRecentLogsRepository : IEnumerable<RecentLogEntry>
{
    /// <summary>
    /// Добавление записи
    /// </summary>
    /// <param name="path">Путь к файлу или директории</param>
    /// <param name="isFolder">Открытие директории</param>
    void Add(string path, bool isFolder = false);
    /// <summary>
    /// Удоление записи
    /// </summary>
    /// <param name="entry"></param>
    /// <returns></returns>
    bool Remove(RecentLogEntry entry);
    /// <summary>
    /// Очистка репозитория
    /// </summary>
    void Clear();
    /// <summary>
    /// Сохранение репозитория
    /// </summary>
    void Save();

    /// <summary>
    /// Получение записи по индексу
    /// </summary>
    /// <param name="index"></param>
    RecentLogEntry this[int index] { get; }
}