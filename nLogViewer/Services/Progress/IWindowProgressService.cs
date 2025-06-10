using System;
using System.Threading;
using System.Threading.Tasks;

namespace nLogViewer.Services.Progress;

/// <summary>
/// Сервис для отображения окна прогресса
/// </summary>
public interface IWindowProgressService
{
    /// <summary>
    /// Показать окно прогресса и выполнить операцию
    /// </summary>
    /// <typeparam name="T">Тип результата операции</typeparam>
    /// <param name="operation">Операция для выполнения</param>
    /// <param name="title">Заголовок окна</param>
    /// <param name="canCancel">Можно ли отменить операцию</param>
    /// <returns>Результат операции</returns>
    Task<T> ShowProgressAsync<T>(
        Func<IProgressReporter, CancellationToken, Task<T>> operation,
        string title = "Загрузка...",
        bool canCancel = true);
    
    /// <summary>
    /// Показать окно прогресса и выполнить операцию без результата
    /// </summary>
    /// <param name="operation">Операция для выполнения</param>
    /// <param name="title">Заголовок окна</param>
    /// <param name="canCancel">Можно ли отменить операцию</param>
    Task ShowProgressAsync(
        Func<IProgressReporter, CancellationToken, Task> operation,
        string title = "Загрузка...",
        bool canCancel = true);
}