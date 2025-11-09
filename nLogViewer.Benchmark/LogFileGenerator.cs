using System.Text;
using nLogViewer.Model;

namespace nLogViewer.Benchmark;

/// <summary>
/// Генератор тестовых лог-файлов для бенчмаркинга
/// </summary>
public class LogFileGenerator
{
    private static readonly Random Random = new();

    private static readonly string[] LogMessages =
    {
        "Application started successfully",
        "Processing user request",
        "Database connection established",
        "Cache miss for key: {0}",
        "API call completed in {0}ms",
        "File uploaded: {0} bytes",
        "User authentication successful",
        "Configuration loaded from {0}",
        "Background task scheduled",
        "Memory usage: {0}MB"
    };

    private static readonly string[] ErrorMessages =
    {
        "Failed to connect to database: timeout",
        "Null reference exception in module {0}",
        "Invalid operation: {0}",
        "File not found: {0}",
        "Access denied to resource {0}"
    };

    private static readonly string[] Sources =
    {
        "Application.Core",
        "Database.Manager",
        "Cache.Service",
        "API.Controller",
        "File.Handler",
        "Auth.Service",
        "Config.Loader",
        "Background.Worker"
    };

    /// <summary>
    /// Генерирует лог-файл с заданным количеством записей
    /// </summary>
    public static string GenerateLogFile(string fileName, int entryCount, bool includeMultiline = false)
    {
        var filePath = Path.Combine(Path.GetTempPath(), fileName);

        using var writer = new StreamWriter(filePath, false, Encoding.UTF8);
        var baseDateTime = DateTime.Now.AddDays(-1);

        for (int i = 0; i < entryCount; i++)
        {
            var dateTime = baseDateTime.AddSeconds(i * 0.1);
            var logType = GetRandomLogType();
            var source = Sources[Random.Next(Sources.Length)];
            var processId = Random.Next(1000, 9999);
            var threadId = Random.Next(1, 20);

            string message;
            if (logType == LogEntryType.Error || logType == LogEntryType.Fatal)
            {
                var template = ErrorMessages[Random.Next(ErrorMessages.Length)];
                message = string.Format(template, Random.Next(1000));
            }
            else
            {
                var template = LogMessages[Random.Next(LogMessages.Length)];
                message = string.Format(template, Random.Next(1000));
            }

            // Формат: 2025-01-15 10:30:45.123|INFO|Application.Core|Process:1234|Thread:5|Message text
            writer.WriteLine($"{dateTime:yyyy-MM-dd HH:mm:ss.fff}|{logType}|{source}|Process:{processId}|Thread:{threadId}|{message}");

            // Добавляем многострочные записи для части логов
            if (includeMultiline && Random.Next(10) == 0)
            {
                writer.WriteLine("  Stack trace:");
                writer.WriteLine($"    at System.Component.Method() in C:\\Source\\File.cs:line {Random.Next(100, 999)}");
                writer.WriteLine($"    at Application.Handler.Process() in C:\\Source\\Handler.cs:line {Random.Next(100, 999)}");
            }
        }

        return filePath;
    }

    /// <summary>
    /// Возвращает случайный тип лог-записи с реалистичным распределением
    /// </summary>
    private static LogEntryType GetRandomLogType()
    {
        var value = Random.Next(100);

        return value switch
        {
            < 1 => LogEntryType.Fatal,      // 1%
            < 5 => LogEntryType.Error,      // 4%
            < 15 => LogEntryType.Warn,      // 10%
            < 60 => LogEntryType.Info,      // 45%
            < 85 => LogEntryType.Debug,     // 25%
            _ => LogEntryType.Trace         // 15%
        };
    }

    /// <summary>
    /// Удаляет временные файлы после тестирования
    /// </summary>
    public static void CleanupTempFiles(params string[] filePaths)
    {
        foreach (var filePath in filePaths)
        {
            if (File.Exists(filePath))
            {
                try
                {
                    File.Delete(filePath);
                }
                catch
                {
                    // Игнорируем ошибки удаления
                }
            }
        }
    }
}
