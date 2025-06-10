using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using nLogViewer.Services.Progress;

namespace nLogViewer.Services.LogReader.Repository;

/// <summary>
/// Репозиторий для доступа к источникам логов
/// </summary>
internal interface ILogRepository : IDisposable
{
    /// <summary>
    /// Уникальный идентификатор источника
    /// </summary>
    string SourceId { get; }
    
    /// <summary>
    /// Описание источника (путь к файлу, URL, строка подключения и т.д.)
    /// </summary>
    string SourceDescription { get; }
    
    /// <summary>
    /// Проверяет доступность источника
    /// </summary>
    /// <returns>true если источник доступен</returns>
    Task<bool> IsAvailableAsync(CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Читает все строки из источника
    /// </summary>
    /// <param name="cancellationToken">Токен отмены</param>
    /// <returns>Поток строк из источника</returns>
    IAsyncEnumerable<string> ReadAllLinesAsync(CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Читает все строки из источника с отчетом о прогрессе
    /// </summary>
    /// <param name="progressReporter">Отчет о прогрессе</param>
    /// <param name="cancellationToken">Токен отмены</param>
    /// <returns>Поток строк из источника</returns>
    IAsyncEnumerable<string> ReadAllLinesAsync(IProgressReporter progressReporter, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Читает новые строки из источника (с последней позиции чтения)
    /// </summary>
    /// <param name="cancellationToken">Токен отмены</param>
    /// <returns>Поток новых строк</returns>
    IAsyncEnumerable<string> ReadNewLinesAsync(CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Читает новые строки из источника с отчетом о прогрессе
    /// </summary>
    /// <param name="progressReporter">Отчет о прогрессе</param>
    /// <param name="cancellationToken">Токен отмены</param>
    /// <returns>Поток новых строк</returns>
    IAsyncEnumerable<string> ReadNewLinesAsync(IProgressReporter progressReporter, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Очищает источник логов (если поддерживается)
    /// </summary>
    /// <param name="cancellationToken">Токен отмены</param>
    /// <returns>true если очистка выполнена успешно</returns>
    Task<bool> ClearAsync(CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Сбрасывает позицию чтения на начало
    /// </summary>
    void ResetPosition();
    
    /// <summary>
    /// Поддерживает ли источник очистку
    /// </summary>
    bool SupportsClear { get; }
    
    /// <summary>
    /// Поддерживает ли источник инкрементальное чтение
    /// </summary>
    bool SupportsIncrementalRead { get; }
}