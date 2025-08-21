using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using nLogViewer.Model;

namespace nLogViewer.Services.LogViewer;

public delegate void EntriesChanged();
public interface ILogViewer : IDisposable
{
    event EntriesChanged EntriesChanged;

    int Count { get; }
    LogViewerState State { get; }
    IReadOnlyList<ILogEntry> LogEntries { get; }
    
    void Start();
    void Stop();
    void Pause();
    [Obsolete("Use ClearAsync instead to prevent UI blocking")]
    void Clear();
    
    /// <summary>
    /// Асинхронно очищает все записи в логе
    /// </summary>
    /// <returns>Task для ожидания завершения операции</returns>
    Task ClearAsync();

    IEnumerable<ILogEntry> GetEntries(int count = 0);
    
    /// <summary>
    /// Получает записи в заданном диапазоне для виртуализации
    /// </summary>
    /// <param name="startIndex">Начальный индекс</param>
    /// <param name="count">Количество записей</param>
    /// <returns>Записи в заданном диапазоне</returns>
    IEnumerable<ILogEntry> GetEntriesRange(int startIndex, int count);
}