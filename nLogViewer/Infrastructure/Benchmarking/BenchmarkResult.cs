using System;
using System.Collections.Generic;
using System.Linq;

namespace nLogViewer.Infrastructure.Benchmarking;

/// <summary>
/// Результаты замера производительности
/// </summary>
public class BenchmarkResult
{
    /// <summary>
    /// Название теста
    /// </summary>
    public string TestName { get; set; } = string.Empty;

    /// <summary>
    /// Размер тестового файла (байты)
    /// </summary>
    public long FileSizeBytes { get; set; }

    /// <summary>
    /// Количество записей в файле
    /// </summary>
    public int TotalEntries { get; set; }

    /// <summary>
    /// Время загрузки (миллисекунды)
    /// </summary>
    public long LoadTimeMs { get; set; }

    /// <summary>
    /// Потребление памяти после загрузки (байты)
    /// </summary>
    public long MemoryUsedBytes { get; set; }

    /// <summary>
    /// Пиковое потребление памяти (байты)
    /// </summary>
    public long PeakMemoryBytes { get; set; }

    /// <summary>
    /// Среднее CPU usage во время загрузки (%)
    /// </summary>
    public double AverageCpuPercent { get; set; }

    /// <summary>
    /// Записей в секунду
    /// </summary>
    public double EntriesPerSecond => LoadTimeMs > 0 ? (TotalEntries / (LoadTimeMs / 1000.0)) : 0;

    /// <summary>
    /// Мегабайт в секунду
    /// </summary>
    public double MbPerSecond => LoadTimeMs > 0 ? (FileSizeBytes / 1024.0 / 1024.0) / (LoadTimeMs / 1000.0) : 0;

    /// <summary>
    /// Байт на запись (среднее)
    /// </summary>
    public double BytesPerEntry => TotalEntries > 0 ? (double)MemoryUsedBytes / TotalEntries : 0;

    /// <summary>
    /// Метаданные конфигурации
    /// </summary>
    public Dictionary<string, object> Metadata { get; set; } = new();

    /// <summary>
    /// Timestamp проведения теста
    /// </summary>
    public DateTime TestTimestamp { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Форматированный вывод результатов
    /// </summary>
    public override string ToString()
    {
        return $"""
            ═══════════════════════════════════════════════════════════════
            BENCHMARK RESULT: {TestName}
            ═══════════════════════════════════════════════════════════════
            File:             {FileSizeBytes / 1024.0 / 1024.0:F2} MB ({TotalEntries:N0} entries)
            Load Time:        {LoadTimeMs:N0} ms ({LoadTimeMs / 1000.0:F2} sec)
            Throughput:       {EntriesPerSecond:N0} entries/sec | {MbPerSecond:F2} MB/sec
            Memory Used:      {MemoryUsedBytes / 1024.0 / 1024.0:F2} MB
            Peak Memory:      {PeakMemoryBytes / 1024.0 / 1024.0:F2} MB
            Avg CPU:          {AverageCpuPercent:F1}%
            Memory/Entry:     {BytesPerEntry:F0} bytes
            Timestamp:        {TestTimestamp:yyyy-MM-dd HH:mm:ss}
            {GetMetadataString()}
            ═══════════════════════════════════════════════════════════════
            """;
    }

    private string GetMetadataString()
    {
        if (!Metadata.Any())
            return string.Empty;

        var lines = Metadata.Select(kv => $"{kv.Key,-20}: {kv.Value}");
        return "Metadata:\n  " + string.Join("\n  ", lines);
    }
}
