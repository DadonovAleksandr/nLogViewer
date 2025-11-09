using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using nLogViewer.Model;
using nLogViewer.Services.Progress;

namespace nLogViewer.Services.LogReader;

internal interface ILogReader : IDisposable
{
    /// <summary>
    /// Описание источника логов (путь к файлу, URL и т.д.)
    /// </summary>
    string SourceDescription { get; }

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
    
    /// <summary>
    /// Асинхронно получить все записи с отчётом о прогрессе
    /// </summary>
    /// <param name="progressReporter">Отчёт о прогрессе</param>
    /// <param name="cancellationToken">Токен отмены</param>
    /// <returns></returns>
    IAsyncEnumerable<ILogEntry> GetAllAsync(IProgressReporter progressReporter, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Асинхронно получить новые записи с отчётом о прогрессе
    /// </summary>
    /// <param name="progressReporter">Отчёт о прогрессе</param>
    /// <param name="cancellationToken">Токен отмены</param>
    /// <returns></returns>
    IAsyncEnumerable<ILogEntry> GetNewAsync(IProgressReporter progressReporter, CancellationToken cancellationToken = default);
}