using System;
using System.Threading;
using System.Threading.Tasks;

namespace nLogViewer.Services.LogReader;

/// <summary>
/// Интерфейс источника логов с возможностью модификации
/// </summary>
internal interface ILogSource : ILogReader
{
    /// <summary>
    /// Очистка текущего лога
    /// </summary>
    /// <returns></returns>
    [Obsolete("Use ClearAsync instead to prevent deadlocks")]
    bool Clear();
    
    /// <summary>
    /// Асинхронная очистка текущего лога
    /// </summary>
    /// <param name="cancellationToken">Токен отмены</param>
    /// <returns></returns>
    Task<bool> ClearAsync(CancellationToken cancellationToken = default);
}