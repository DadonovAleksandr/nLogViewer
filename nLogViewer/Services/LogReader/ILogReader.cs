using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using nLogViewer.Model;

namespace nLogViewer.Services.LogReader;

internal interface ILogReader : IDisposable
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
}