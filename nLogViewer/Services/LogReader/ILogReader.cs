using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using nLogViewer.Model;

namespace nLogViewer.Services.LogReader;

internal interface ILogReader
{
    /// <summary>
    /// Получить все записи
    /// </summary>
    /// <returns></returns>
    IEnumerable<ILogEntry> GetAll();
    
    /// <summary>
    /// Получить новые записи
    /// </summary>
    /// <returns></returns>
    IEnumerable<ILogEntry> GetNew();

    /// <summary>
    /// Очистка текущего лога
    /// </summary>
    /// <returns></returns>
    bool Clear();
    
    /// <summary>
    /// Асинхронно получить все записи
    /// </summary>
    /// <param name="cancellationToken">Токен отмены</param>
    /// <returns></returns>
    IAsyncEnumerable<ILogEntry> GetAllAsync(CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Асинхронно получить новые записи
    /// </summary>
    /// <param name="cancellationToken">Токен отмены</param>
    /// <returns></returns>
    IAsyncEnumerable<ILogEntry> GetNewAsync(CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Асинхронная очистка текущего лога
    /// </summary>
    /// <param name="cancellationToken">Токен отмены</param>
    /// <returns></returns>
    Task<bool> ClearAsync(CancellationToken cancellationToken = default);
}