using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using nLogViewer.Infrastructure.Collections;
using nLogViewer.Infrastructure.Configuration;
using nLogViewer.Services.LogReader.Repository;
using nLogViewer.Services.LogViewer;

namespace nLogViewer.Infrastructure.Benchmarking;

/// <summary>
/// Бенчмаркинг LogViewer для измерения производительности оптимизаций
/// </summary>
internal class LogViewerBenchmark
{
    private readonly string _testFilePath;
    private readonly MemoryConfiguration _memoryConfig;

    public LogViewerBenchmark(string testFilePath, MemoryConfiguration? memoryConfig = null)
    {
        _testFilePath = testFilePath;
        _memoryConfig = memoryConfig ?? new MemoryConfiguration
        {
            UseCircularBuffer = true,
            MaxEntriesInMemory = 100_000,
            EnableDataVirtualization = false
        };
    }

    /// <summary>
    /// Запускает тест загрузки файла
    /// </summary>
    public async Task<BenchmarkResult> RunLoadTestAsync(int pollingIntervalMs = 2000)
    {
        var fileInfo = new FileInfo(_testFilePath);
        if (!fileInfo.Exists)
            throw new FileNotFoundException($"Test file not found: {_testFilePath}");

        var result = new BenchmarkResult
        {
            TestName = $"Load Test - {Path.GetFileName(_testFilePath)}",
            FileSizeBytes = fileInfo.Length,
            Metadata =
            {
                ["PollingIntervalMs"] = pollingIntervalMs,
                ["UseCircularBuffer"] = _memoryConfig.UseCircularBuffer,
                ["MaxEntriesInMemory"] = _memoryConfig.MaxEntriesInMemory,
                ["EnableDataVirtualization"] = _memoryConfig.EnableDataVirtualization,
                ["FileName"] = fileInfo.Name
            }
        };

        using var metricsCollector = new MetricsCollector();

        // Создаем LogViewer с настройками
        var repository = new FileLogRepository(_testFilePath);
        var viewer = new Services.LogViewer.LogViewer(
            repository,
            _memoryConfig,
            null, // appConfig - не нужен для бенчмарка
            null  // progressReporter
        );

        // Запускаем чтение
        await viewer.StartAsync();

        // Ждем завершения загрузки
        var timeout = TimeSpan.FromMinutes(5);
        var startTime = DateTime.UtcNow;

        while (viewer.State == LogViewerState.ReadAllMsg || viewer.State == LogViewerState.ReadNewMsg)
        {
            await Task.Delay(100);

            if (DateTime.UtcNow - startTime > timeout)
            {
                throw new TimeoutException("Benchmark timeout exceeded (5 minutes)");
            }
        }

        // Собираем метрики
        var snapshot = metricsCollector.Stop();

        result.LoadTimeMs = snapshot.ElapsedMs;
        result.MemoryUsedBytes = snapshot.MemoryUsedBytes;
        result.PeakMemoryBytes = snapshot.PeakMemoryBytes;
        result.AverageCpuPercent = snapshot.AverageCpuPercent;
        result.TotalEntries = viewer.LogEntries.Count;

        // Останавливаем viewer
        await viewer.StopAsync();

        return result;
    }

    /// <summary>
    /// Запускает полный набор тестов с разными конфигурациями
    /// </summary>
    public async Task<List<BenchmarkResult>> RunComparisonTestsAsync()
    {
        var results = new List<BenchmarkResult>();

        // Тест 1: Без оптимизаций (CircularBuffer OFF)
        Console.WriteLine("Running Test 1: Without CircularBuffer...");
        var config1 = new MemoryConfiguration
        {
            UseCircularBuffer = false,
            MaxEntriesInMemory = int.MaxValue,
            EnableDataVirtualization = false
        };
        var benchmark1 = new LogViewerBenchmark(_testFilePath, config1);
        var result1 = await benchmark1.RunLoadTestAsync(2000);
        result1.TestName = "Baseline (No CircularBuffer)";
        results.Add(result1);

        // Даем GC время очистить память
        GC.Collect();
        GC.WaitForPendingFinalizers();
        await Task.Delay(2000);

        // Тест 2: С CircularBuffer (100k entries)
        Console.WriteLine("Running Test 2: With CircularBuffer (100k)...");
        var config2 = new MemoryConfiguration
        {
            UseCircularBuffer = true,
            MaxEntriesInMemory = 100_000,
            EnableDataVirtualization = false
        };
        var benchmark2 = new LogViewerBenchmark(_testFilePath, config2);
        var result2 = await benchmark2.RunLoadTestAsync(2000);
        result2.TestName = "Optimized (CircularBuffer 100k)";
        results.Add(result2);

        // Даем GC время очистить память
        GC.Collect();
        GC.WaitForPendingFinalizers();
        await Task.Delay(2000);

        // Тест 3: С увеличенным polling interval
        Console.WriteLine("Running Test 3: With 5-second polling...");
        var config3 = new MemoryConfiguration
        {
            UseCircularBuffer = true,
            MaxEntriesInMemory = 100_000,
            EnableDataVirtualization = false
        };
        var benchmark3 = new LogViewerBenchmark(_testFilePath, config3);
        var result3 = await benchmark3.RunLoadTestAsync(5000);
        result3.TestName = "Optimized (CircularBuffer + 5sec polling)";
        results.Add(result3);

        return results;
    }

    /// <summary>
    /// Выводит сравнительную таблицу результатов
    /// </summary>
    public static void PrintComparisonTable(List<BenchmarkResult> results)
    {
        if (!results.Any())
        {
            Console.WriteLine("No results to display.");
            return;
        }

        Console.WriteLine();
        Console.WriteLine("═══════════════════════════════════════════════════════════════════════════════════════");
        Console.WriteLine("                            PERFORMANCE COMPARISON TABLE");
        Console.WriteLine("═══════════════════════════════════════════════════════════════════════════════════════");
        Console.WriteLine();

        var baseline = results.First();

        Console.WriteLine($"{"Test Name",-40} {"Load Time",-15} {"Memory Used",-15} {"Peak Mem",-15} {"Entries/sec",-15}");
        Console.WriteLine(new string('─', 100));

        foreach (var result in results)
        {
            var loadTimeImprovement = baseline != result
                ? $"({(1 - result.LoadTimeMs / (double)baseline.LoadTimeMs) * 100:+0.0;-0.0}%)"
                : "";

            var memoryImprovement = baseline != result
                ? $"({(1 - result.MemoryUsedBytes / (double)baseline.MemoryUsedBytes) * 100:+0.0;-0.0}%)"
                : "";

            Console.WriteLine(
                $"{result.TestName,-40} " +
                $"{result.LoadTimeMs,8:N0} ms {loadTimeImprovement,-6} " +
                $"{result.MemoryUsedBytes / 1024.0 / 1024.0,8:F1} MB {memoryImprovement,-6} " +
                $"{result.PeakMemoryBytes / 1024.0 / 1024.0,8:F1} MB " +
                $"{result.EntriesPerSecond,12:N0}"
            );
        }

        Console.WriteLine(new string('─', 100));
        Console.WriteLine();

        // Детальная статистика
        Console.WriteLine("DETAILED STATISTICS:");
        Console.WriteLine();

        foreach (var result in results)
        {
            Console.WriteLine(result.ToString());
        }
    }

    /// <summary>
    /// Сохраняет результаты в JSON файл
    /// </summary>
    public static async Task SaveResultsAsync(List<BenchmarkResult> results, string outputPath)
    {
        var json = System.Text.Json.JsonSerializer.Serialize(results, new System.Text.Json.JsonSerializerOptions
        {
            WriteIndented = true
        });

        await File.WriteAllTextAsync(outputPath, json);
        Console.WriteLine($"\nResults saved to: {outputPath}");
    }
}
