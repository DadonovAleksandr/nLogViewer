using System;

namespace nLogViewer.Services.Progress;

/// <summary>
/// Интерфейс для отчёта о прогрессе операции
/// </summary>
public interface IProgressReporter
{
    /// <summary>
    /// Событие изменения прогресса
    /// </summary>
    event EventHandler<ProgressEventArgs> ProgressChanged;
    
    /// <summary>
    /// Сообщить о прогрессе
    /// </summary>
    /// <param name="current">Текущее значение</param>
    /// <param name="total">Общее значение</param>
    /// <param name="message">Сообщение</param>
    void Report(long current, long total, string? message = null);
    
    /// <summary>
    /// Сообщить о прогрессе в процентах
    /// </summary>
    /// <param name="percentage">Процент выполнения (0-100)</param>
    /// <param name="message">Сообщение</param>
    void ReportPercentage(int percentage, string? message = null);
}

/// <summary>
/// Данные о прогрессе операции
/// </summary>
public class ProgressEventArgs : EventArgs
{
    public long Current { get; }
    public long Total { get; }
    public int Percentage { get; }
    public string? Message { get; }
    public TimeSpan? EstimatedTimeRemaining { get; }
    
    public ProgressEventArgs(long current, long total, string? message = null, TimeSpan? estimatedTimeRemaining = null)
    {
        Current = current;
        Total = total;
        Percentage = total > 0 ? (int)((current * 100) / total) : 0;
        Message = message;
        EstimatedTimeRemaining = estimatedTimeRemaining;
    }
}