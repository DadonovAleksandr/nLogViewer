using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using nLogViewer.Infrastructure.Benchmarking;

namespace nLogViewer.Tester;

/// <summary>
/// Консольная утилита для запуска бенчмарков производительности
/// </summary>
public static class BenchmarkRunner
{
    public static async Task Main(string[] args)
    {
        Console.WriteLine("═══════════════════════════════════════════════════════════════");
        Console.WriteLine("        nLogViewer Performance Benchmark Tool");
        Console.WriteLine("═══════════════════════════════════════════════════════════════");
        Console.WriteLine();

        if (args.Length == 0)
        {
            ShowUsage();
            return;
        }

        var command = args[0].ToLowerInvariant();

        try
        {
            switch (command)
            {
                case "generate":
                    await GenerateTestFileAsync(args);
                    break;

                case "run":
                    await RunBenchmarkAsync(args);
                    break;

                case "compare":
                    await RunComparisonAsync(args);
                    break;

                default:
                    ShowUsage();
                    break;
            }
        }
        catch (Exception ex)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine($"\n❌ Error: {ex.Message}");
            Console.ResetColor();
            Environment.Exit(1);
        }
    }

    private static void ShowUsage()
    {
        Console.WriteLine("Usage:");
        Console.WriteLine();
        Console.WriteLine("  Generate test file:");
        Console.WriteLine("    benchmark generate <output-path> <num-entries> [--multiline]");
        Console.WriteLine("    Example: benchmark generate test.log 100000");
        Console.WriteLine("    Example: benchmark generate test.log 100000 --multiline");
        Console.WriteLine();
        Console.WriteLine("  Run single benchmark:");
        Console.WriteLine("    benchmark run <log-file-path> [--polling <ms>] [--output <path>]");
        Console.WriteLine("    Example: benchmark run test.log --polling 2000");
        Console.WriteLine();
        Console.WriteLine("  Run comparison tests:");
        Console.WriteLine("    benchmark compare <log-file-path> [--output <path>]");
        Console.WriteLine("    Example: benchmark compare test.log --output results.json");
        Console.WriteLine();
    }

    private static async Task GenerateTestFileAsync(string[] args)
    {
        if (args.Length < 3)
        {
            Console.WriteLine("Error: Missing arguments for generate command");
            ShowUsage();
            return;
        }

        var outputPath = args[1];
        if (!int.TryParse(args[2], out var numEntries))
        {
            Console.WriteLine("Error: Invalid number of entries");
            return;
        }

        var multiline = args.Any(a => a.Equals("--multiline", StringComparison.OrdinalIgnoreCase));

        Console.WriteLine($"Generating test file: {outputPath}");
        Console.WriteLine($"Number of entries: {numEntries:N0}");
        Console.WriteLine($"Multiline mode: {multiline}");
        Console.WriteLine();

        var random = new Random(42); // Фиксированный seed для воспроизводимости
        var startTime = DateTime.Now.AddHours(-1);

        using var writer = new StreamWriter(outputPath);

        for (int i = 0; i < numEntries; i++)
        {
            var timestamp = startTime.AddMilliseconds(i * 100);
            var level = random.Next(0, 100) switch
            {
                < 50 => "INFO",
                < 75 => "DEBUG",
                < 90 => "WARNING",
                < 98 => "ERROR",
                _ => "FATAL"
            };

            string message;
            if (multiline && random.Next(0, 100) < 20) // 20% многострочных
            {
                message = $"Message {i}\nLine 2: {Guid.NewGuid()}\nLine 3: Details";
            }
            else
            {
                message = $"Log message number {i} - {Guid.NewGuid()}";
            }

            var source = $"Source.{random.Next(1, 10)}";
            var processId = random.Next(1000, 9999);
            var threadId = random.Next(1, 20);

            await writer.WriteLineAsync(
                $"{timestamp:yyyy-MM-dd HH:mm:ss.ffff} | {level} | {message} | {source} | {processId} | {threadId}");

            if (i % 10000 == 0 && i > 0)
            {
                Console.Write($"\rProgress: {i:N0} / {numEntries:N0} ({i * 100.0 / numEntries:F1}%)");
            }
        }

        Console.WriteLine($"\r✅ Generated {numEntries:N0} entries");

        var fileInfo = new FileInfo(outputPath);
        Console.WriteLine($"   File size: {fileInfo.Length / 1024.0 / 1024.0:F2} MB");
        Console.WriteLine($"   Path: {fileInfo.FullName}");
    }

    private static async Task RunBenchmarkAsync(string[] args)
    {
        if (args.Length < 2)
        {
            Console.WriteLine("Error: Missing log file path");
            ShowUsage();
            return;
        }

        var logFilePath = args[1];
        var pollingInterval = 2000;
        string? outputPath = null;

        // Парсим аргументы
        for (int i = 2; i < args.Length; i++)
        {
            if (args[i] == "--polling" && i + 1 < args.Length)
            {
                pollingInterval = int.Parse(args[i + 1]);
                i++;
            }
            else if (args[i] == "--output" && i + 1 < args.Length)
            {
                outputPath = args[i + 1];
                i++;
            }
        }

        Console.WriteLine($"Running benchmark on: {logFilePath}");
        Console.WriteLine($"Polling interval: {pollingInterval} ms");
        Console.WriteLine();

        var benchmark = new LogViewerBenchmark(logFilePath);
        var result = await benchmark.RunLoadTestAsync(pollingInterval);

        Console.WriteLine(result.ToString());

        if (!string.IsNullOrEmpty(outputPath))
        {
            await LogViewerBenchmark.SaveResultsAsync(new List<BenchmarkResult> { result }, outputPath);
        }
    }

    private static async Task RunComparisonAsync(string[] args)
    {
        if (args.Length < 2)
        {
            Console.WriteLine("Error: Missing log file path");
            ShowUsage();
            return;
        }

        var logFilePath = args[1];
        string? outputPath = null;

        // Парсим аргументы
        for (int i = 2; i < args.Length; i++)
        {
            if (args[i] == "--output" && i + 1 < args.Length)
            {
                outputPath = args[i + 1];
                i++;
            }
        }

        Console.WriteLine($"Running comparison tests on: {logFilePath}");
        Console.WriteLine();
        Console.WriteLine("⚠️  This will run 3 tests and may take several minutes...");
        Console.WriteLine();

        var benchmark = new LogViewerBenchmark(logFilePath);
        var results = await benchmark.RunComparisonTestsAsync();

        LogViewerBenchmark.PrintComparisonTable(results);

        if (!string.IsNullOrEmpty(outputPath))
        {
            await LogViewerBenchmark.SaveResultsAsync(results, outputPath);
        }
    }
}
