using BenchmarkDotNet.Running;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Jobs;
using BenchmarkDotNet.Reports;
using BenchmarkDotNet.Columns;
using BenchmarkDotNet.Exporters;

namespace nLogViewer.Benchmark;

class Program
{
    static void Main(string[] args)
    {
        PrintHeader();

        // Проверяем аргументы командной строки
        if (args.Length > 0 && args[0] == "--quick")
        {
            RunQuickBenchmark();
        }
        else if (args.Length > 0 && args[0] == "--full")
        {
            RunFullBenchmark();
        }
        else
        {
            ShowMenu();
        }
    }

    static void PrintHeader()
    {
        Console.ForegroundColor = ConsoleColor.Cyan;
        Console.WriteLine("═══════════════════════════════════════════════════════════════");
        Console.WriteLine("        nLogViewer Performance Benchmark Suite");
        Console.WriteLine("        Тестирование производительности чтения логов");
        Console.WriteLine("═══════════════════════════════════════════════════════════════");
        Console.ResetColor();
        Console.WriteLine();
    }

    static void ShowMenu()
    {
        Console.WriteLine("Выберите режим тестирования:");
        Console.WriteLine();
        Console.WriteLine("  1. Быстрый тест (рекомендуется для разработки)");
        Console.WriteLine("     - Тестирование с небольшими объемами данных");
        Console.WriteLine("     - Время выполнения: ~5 минут");
        Console.WriteLine();
        Console.WriteLine("  2. Полный тест (для финальной проверки)");
        Console.WriteLine("     - Полное тестирование со всеми объемами данных");
        Console.WriteLine("     - Время выполнения: ~30 минут");
        Console.WriteLine();
        Console.WriteLine("  3. Парсинг (тест производительности парсеров)");
        Console.WriteLine("     - Сравнение производительности разных методов парсинга");
        Console.WriteLine("     - Время выполнения: ~2 минуты");
        Console.WriteLine();
        Console.Write("Ваш выбор (1-3): ");

        var choice = Console.ReadLine();
        Console.WriteLine();

        switch (choice)
        {
            case "1":
                RunQuickBenchmark();
                break;
            case "2":
                RunFullBenchmark();
                break;
            case "3":
                RunParsingBenchmark();
                break;
            default:
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine("Неверный выбор. Запуск быстрого теста по умолчанию.");
                Console.ResetColor();
                Console.WriteLine();
                RunQuickBenchmark();
                break;
        }
    }

    static void RunQuickBenchmark()
    {
        Console.ForegroundColor = ConsoleColor.Yellow;
        Console.WriteLine("→ Запуск быстрого теста...");
        Console.ResetColor();
        Console.WriteLine();

        var config = ManualConfig.Create(DefaultConfig.Instance)
            .AddJob(Job.Default.WithIterationCount(5))
            .AddExporter(new ConsoleResultExporter())
            .HideColumns(Column.Error, Column.StdDev, Column.RatioSD);

        // Запускаем только базовые бенчмарки с 10k и 100k записей
        var summary = BenchmarkRunner.Run<LogReaderBenchmarks>(config);

        PrintSummaryTable(summary);
        WaitForExit();
    }

    static void RunFullBenchmark()
    {
        Console.ForegroundColor = ConsoleColor.Yellow;
        Console.WriteLine("→ Запуск полного теста...");
        Console.ResetColor();
        Console.WriteLine();

        var config = ManualConfig.Create(DefaultConfig.Instance)
            .AddExporter(new ConsoleResultExporter())
            .HideColumns(Column.Error, Column.StdDev);

        // Запускаем все бенчмарки
        var summaries = new List<Summary>
        {
            BenchmarkRunner.Run<LogReaderBenchmarks>(config),
            BenchmarkRunner.Run<MultilineLogReaderBenchmarks>(config)
        };

        Console.WriteLine();
        Console.ForegroundColor = ConsoleColor.Cyan;
        Console.WriteLine("═══════════════════════════════════════════════════════════════");
        Console.WriteLine("                  ИТОГОВЫЕ РЕЗУЛЬТАТЫ");
        Console.WriteLine("═══════════════════════════════════════════════════════════════");
        Console.ResetColor();
        Console.WriteLine();

        foreach (var summary in summaries)
        {
            PrintSummaryTable(summary);
            Console.WriteLine();
        }

        WaitForExit();
    }

    static void RunParsingBenchmark()
    {
        Console.ForegroundColor = ConsoleColor.Yellow;
        Console.WriteLine("→ Запуск теста производительности парсинга...");
        Console.ResetColor();
        Console.WriteLine();

        var config = ManualConfig.Create(DefaultConfig.Instance)
            .AddJob(Job.Default.WithIterationCount(10))
            .AddExporter(new ConsoleResultExporter())
            .HideColumns(Column.Error, Column.StdDev);

        var summary = BenchmarkRunner.Run<ParsingBenchmarks>(config);

        PrintSummaryTable(summary);
        WaitForExit();
    }

    static void PrintSummaryTable(Summary summary)
    {
        if (!summary.Reports.Any())
        {
            Console.WriteLine("Нет результатов для отображения.");
            return;
        }

        Console.WriteLine();
        Console.ForegroundColor = ConsoleColor.Cyan;
        Console.WriteLine($"Результаты: {summary.Title}");
        Console.ResetColor();
        Console.WriteLine();

        // Заголовок таблицы
        Console.ForegroundColor = ConsoleColor.White;
        Console.WriteLine("┌──────────────────────────────────────┬──────────────┬──────────────┬──────────────┬──────────────┐");
        Console.WriteLine("│ Метод                                │ Записей      │ Время        │ Память       │ Скорость     │");
        Console.WriteLine("├──────────────────────────────────────┼──────────────┼──────────────┼──────────────┼──────────────┤");
        Console.ResetColor();

        foreach (var report in summary.Reports.OrderBy(r => r.BenchmarkCase.Parameters["EntryCount"] ?? 0))
        {
            var method = report.BenchmarkCase.Descriptor.WorkloadMethodDisplayInfo;
            var entryCount = report.BenchmarkCase.Parameters.Items.FirstOrDefault(p => p.Name == "EntryCount")?.Value ?? "N/A";
            var mean = report.ResultStatistics?.Mean ?? 0;
            var memory = report.GcStats.GetTotalAllocatedBytes(false) ?? 0;

            // Расчет скорости (записей в секунду)
            var entriesPerSecond = 0.0;
            if (mean > 0 && int.TryParse(entryCount.ToString(), out var entries))
            {
                entriesPerSecond = entries / (mean / 1_000_000_000); // mean в наносекундах
            }

            // Форматирование
            var methodStr = TruncateOrPad(method, 36);
            var entriesStr = FormatNumber(entryCount.ToString()!).PadLeft(12);
            var timeStr = FormatTime(mean).PadLeft(12);
            var memoryStr = FormatBytes(memory).PadLeft(12);
            var speedStr = FormatSpeed(entriesPerSecond).PadLeft(12);

            // Цветовое кодирование результатов
            Console.Write("│ ");
            Console.Write(methodStr);
            Console.Write(" │ ");
            Console.Write(entriesStr);
            Console.Write(" │ ");

            // Время: зеленый если быстро, желтый если средне, красный если медленно
            if (mean < 100_000_000) // < 100ms
                Console.ForegroundColor = ConsoleColor.Green;
            else if (mean < 1_000_000_000) // < 1s
                Console.ForegroundColor = ConsoleColor.Yellow;
            else
                Console.ForegroundColor = ConsoleColor.Red;
            Console.Write(timeStr);
            Console.ResetColor();

            Console.Write(" │ ");

            // Память: зеленый если мало, желтый если средне, красный если много
            if (memory < 20_000_000) // < 20 MB
                Console.ForegroundColor = ConsoleColor.Green;
            else if (memory < 100_000_000) // < 100 MB
                Console.ForegroundColor = ConsoleColor.Yellow;
            else
                Console.ForegroundColor = ConsoleColor.Red;
            Console.Write(memoryStr);
            Console.ResetColor();

            Console.Write(" │ ");

            // Скорость: зеленый если быстро, желтый если средне, красный если медленно
            if (entriesPerSecond > 100_000) // > 100k entries/sec
                Console.ForegroundColor = ConsoleColor.Green;
            else if (entriesPerSecond > 40_000) // > 40k entries/sec
                Console.ForegroundColor = ConsoleColor.Yellow;
            else
                Console.ForegroundColor = ConsoleColor.Red;
            Console.Write(speedStr);
            Console.ResetColor();

            Console.WriteLine(" │");
        }

        Console.ForegroundColor = ConsoleColor.White;
        Console.WriteLine("└──────────────────────────────────────┴──────────────┴──────────────┴──────────────┴──────────────┘");
        Console.ResetColor();
        Console.WriteLine();

        // Выводим цели производительности
        Console.ForegroundColor = ConsoleColor.Cyan;
        Console.WriteLine("Цели производительности (Phase 1):");
        Console.ResetColor();
        Console.WriteLine("  • Память: < 20 MB для всех тестов");
        Console.WriteLine("  • Скорость: > 40,000 записей/сек");
        Console.WriteLine();
    }

    static string TruncateOrPad(string str, int length)
    {
        if (str.Length > length)
            return str.Substring(0, length - 3) + "...";
        return str.PadRight(length);
    }

    static string FormatNumber(string number)
    {
        if (long.TryParse(number, out var n))
        {
            if (n >= 1_000_000)
                return $"{n / 1_000_000.0:F1}M";
            if (n >= 1_000)
                return $"{n / 1_000.0:F0}K";
            return n.ToString();
        }
        return number;
    }

    static string FormatTime(double nanoseconds)
    {
        if (nanoseconds >= 1_000_000_000)
            return $"{nanoseconds / 1_000_000_000:F2} s";
        if (nanoseconds >= 1_000_000)
            return $"{nanoseconds / 1_000_000:F2} ms";
        if (nanoseconds >= 1_000)
            return $"{nanoseconds / 1_000:F2} μs";
        return $"{nanoseconds:F2} ns";
    }

    static string FormatBytes(long bytes)
    {
        if (bytes >= 1_073_741_824)
            return $"{bytes / 1_073_741_824.0:F2} GB";
        if (bytes >= 1_048_576)
            return $"{bytes / 1_048_576.0:F2} MB";
        if (bytes >= 1_024)
            return $"{bytes / 1_024.0:F2} KB";
        return $"{bytes} B";
    }

    static string FormatSpeed(double entriesPerSecond)
    {
        if (entriesPerSecond >= 1_000_000)
            return $"{entriesPerSecond / 1_000_000:F2}M/s";
        if (entriesPerSecond >= 1_000)
            return $"{entriesPerSecond / 1_000:F0}K/s";
        return $"{entriesPerSecond:F0}/s";
    }

    static void WaitForExit()
    {
        Console.WriteLine();
        Console.ForegroundColor = ConsoleColor.Gray;
        Console.WriteLine("Нажмите любую клавишу для выхода...");
        Console.ResetColor();
        Console.ReadKey();
    }
}

/// <summary>
/// Кастомный экспортер для красивого вывода в консоль
/// </summary>
class ConsoleResultExporter : IExporter
{
    public string Name => "ConsoleResult";

    public void ExportToLog(Summary summary, BenchmarkDotNet.Loggers.ILogger logger)
    {
        // Логирование не требуется
    }

    public IEnumerable<string> ExportToFiles(Summary summary, BenchmarkDotNet.Loggers.ILogger consoleLogger)
    {
        return Array.Empty<string>();
    }
}
