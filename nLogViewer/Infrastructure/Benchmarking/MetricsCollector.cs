using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;

namespace nLogViewer.Infrastructure.Benchmarking;

/// <summary>
/// Сборщик метрик производительности
/// </summary>
public class MetricsCollector : IDisposable
{
    private readonly Stopwatch _stopwatch;
    private readonly Process _currentProcess;
    private readonly long _initialMemory;
    private readonly PerformanceCounter? _cpuCounter;
    private readonly List<double> _cpuSamples = new();
    private readonly Timer _samplingTimer;
    private long _peakMemory;

    public MetricsCollector()
    {
        _stopwatch = Stopwatch.StartNew();
        _currentProcess = Process.GetCurrentProcess();

        // Форсируем GC перед началом замера
        GC.Collect();
        GC.WaitForPendingFinalizers();
        GC.Collect();

        _initialMemory = GC.GetTotalMemory(false);
        _peakMemory = _initialMemory;

        // Пытаемся создать счетчик CPU (может не работать на Linux)
        try
        {
            _cpuCounter = new PerformanceCounter("Processor", "% Processor Time", "_Total");
            _cpuCounter.NextValue(); // Первый вызов для инициализации
        }
        catch
        {
            // CPU counter не поддерживается на этой платформе
            _cpuCounter = null;
        }

        // Запускаем таймер для периодического сбора метрик
        _samplingTimer = new Timer(_ => CollectSample(), null, 100, 100);
    }

    private void CollectSample()
    {
        // Собираем CPU usage
        if (_cpuCounter != null)
        {
            try
            {
                _cpuSamples.Add(_cpuCounter.NextValue());
            }
            catch
            {
                // Игнорируем ошибки сбора CPU
            }
        }

        // Отслеживаем пиковое потребление памяти
        var currentMemory = GC.GetTotalMemory(false);
        if (currentMemory > _peakMemory)
            _peakMemory = currentMemory;
    }

    /// <summary>
    /// Останавливает сбор метрик и возвращает результаты
    /// </summary>
    public MetricsSnapshot Stop()
    {
        _stopwatch.Stop();
        _samplingTimer.Change(Timeout.Infinite, Timeout.Infinite);

        // Финальный сбор метрик
        var finalMemory = GC.GetTotalMemory(false);

        return new MetricsSnapshot
        {
            ElapsedMs = _stopwatch.ElapsedMilliseconds,
            MemoryUsedBytes = finalMemory - _initialMemory,
            PeakMemoryBytes = _peakMemory - _initialMemory,
            AverageCpuPercent = _cpuSamples.Any() ? _cpuSamples.Average() : 0
        };
    }

    public void Dispose()
    {
        _samplingTimer?.Dispose();
        _cpuCounter?.Dispose();
        _stopwatch.Stop();
    }
}

/// <summary>
/// Снимок метрик производительности
/// </summary>
public class MetricsSnapshot
{
    /// <summary>
    /// Время выполнения (мс)
    /// </summary>
    public long ElapsedMs { get; set; }

    /// <summary>
    /// Использованная память (байты)
    /// </summary>
    public long MemoryUsedBytes { get; set; }

    /// <summary>
    /// Пиковая память (байты)
    /// </summary>
    public long PeakMemoryBytes { get; set; }

    /// <summary>
    /// Среднее CPU usage (%)
    /// </summary>
    public double AverageCpuPercent { get; set; }
}
