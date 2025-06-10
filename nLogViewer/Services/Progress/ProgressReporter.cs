using System;
using System.Diagnostics;
using NLog;

namespace nLogViewer.Services.Progress;

/// <summary>
/// Реализация отчёта о прогрессе с расчётом времени
/// </summary>
internal class ProgressReporter : IProgressReporter
{
    private static readonly Logger _log = LogManager.GetCurrentClassLogger();
    
    private readonly Stopwatch _stopwatch = new();
    private long _lastReportedCurrent = 0;
    private DateTime _lastReportTime = DateTime.UtcNow;
    private readonly TimeSpan _minReportInterval = TimeSpan.FromMilliseconds(100);
    
    public event EventHandler<ProgressEventArgs>? ProgressChanged;
    
    public ProgressReporter()
    {
        _stopwatch.Start();
    }
    
    public void Report(long current, long total, string? message = null)
    {
        // Ограничиваем частоту отчётов для производительности
        var now = DateTime.UtcNow;
        if (current < total && (now - _lastReportTime) < _minReportInterval)
            return;
            
        _lastReportTime = now;
        
        // Расчёт оставшегося времени
        TimeSpan? estimatedTimeRemaining = null;
        if (current > 0 && current < total && _stopwatch.IsRunning)
        {
            var elapsed = _stopwatch.Elapsed;
            var itemsProcessed = current - _lastReportedCurrent;
            
            if (itemsProcessed > 0)
            {
                var averageTimePerItem = elapsed.TotalMilliseconds / current;
                var remainingItems = total - current;
                var remainingMilliseconds = averageTimePerItem * remainingItems;
                estimatedTimeRemaining = TimeSpan.FromMilliseconds(remainingMilliseconds);
            }
        }
        
        _lastReportedCurrent = current;
        
        var args = new ProgressEventArgs(current, total, message, estimatedTimeRemaining);
        ProgressChanged?.Invoke(this, args);
        
        _log.Trace($"Прогресс: {args.Percentage}% ({current}/{total}) - {message}");
    }
    
    public void ReportPercentage(int percentage, string? message = null)
    {
        var args = new ProgressEventArgs(percentage, 100, message);
        ProgressChanged?.Invoke(this, args);
        
        _log.Trace($"Прогресс: {percentage}% - {message}");
    }
}