using System.Diagnostics;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Order;
using nLogViewer.Services.LogReader.FileLogReader;
using nLogViewer.Services.UserDialogService;
using nLogViewer.Model;

namespace nLogViewer.Benchmark;

/// <summary>
/// Бенчмарки для тестирования производительности чтения и парсинга логов
/// </summary>
[MemoryDiagnoser]
[Orderer(SummaryOrderPolicy.FastestToSlowest)]
[RankColumn]
public class LogReaderBenchmarks
{
    private string? _testFilePath;
    private IUserDialogService? _userDialogService;

    [Params(10_000, 100_000, 1_000_000)]
    public int EntryCount { get; set; }

    [GlobalSetup]
    public void Setup()
    {
        // Создаем фиктивный сервис диалогов
        _userDialogService = new DummyUserDialogService();

        // Генерируем тестовый файл
        _testFilePath = LogFileGenerator.GenerateLogFile($"benchmark_{EntryCount}.log", EntryCount);
        Console.WriteLine($"Создан тестовый файл: {_testFilePath}");
    }

    [GlobalCleanup]
    public void Cleanup()
    {
        if (_testFilePath != null)
        {
            LogFileGenerator.CleanupTempFiles(_testFilePath);
        }
    }

    [Benchmark(Description = "Синхронное чтение логов")]
    public int ReadLogsSync()
    {
        using var reader = new FileLogReader(_testFilePath!, _userDialogService!);
        var count = 0;
        foreach (var entry in reader.GetAll())
        {
            count++;
        }
        return count;
    }

    [Benchmark(Description = "Асинхронное чтение логов")]
    public async Task<int> ReadLogsAsync()
    {
        using var reader = new FileLogReader(_testFilePath!, _userDialogService!);
        var count = 0;
        await foreach (var entry in reader.GetAllAsync())
        {
            count++;
        }
        return count;
    }

    /// <summary>
    /// Фиктивный сервис диалогов для бенчмарков
    /// </summary>
    private class DummyUserDialogService : IUserDialogService
    {
        public void ShowInformation(string message, string title) { }
        public void ShowWarning(string message, string title) { }
        public void ShowError(string message, string title) { }
        public bool Confirm(string message, string title, bool exclamation = false) => true;
    }
}

/// <summary>
/// Бенчмарки для многострочных логов
/// </summary>
[MemoryDiagnoser]
[Orderer(SummaryOrderPolicy.FastestToSlowest)]
[RankColumn]
public class MultilineLogReaderBenchmarks
{
    private string? _testFilePath;
    private IUserDialogService? _userDialogService;

    [Params(10_000, 100_000)]
    public int EntryCount { get; set; }

    [GlobalSetup]
    public void Setup()
    {
        _userDialogService = new DummyUserDialogService();
        _testFilePath = LogFileGenerator.GenerateLogFile($"benchmark_multiline_{EntryCount}.log", EntryCount, includeMultiline: true);
        Console.WriteLine($"Создан тестовый файл с многострочными записями: {_testFilePath}");
    }

    [GlobalCleanup]
    public void Cleanup()
    {
        if (_testFilePath != null)
        {
            LogFileGenerator.CleanupTempFiles(_testFilePath);
        }
    }

    [Benchmark(Description = "Чтение многострочных логов")]
    public int ReadMultilineLogsSync()
    {
        using var reader = new FileLogReader(_testFilePath!, _userDialogService!);
        var count = 0;
        foreach (var entry in reader.GetAll())
        {
            count++;
        }
        return count;
    }

    private class DummyUserDialogService : IUserDialogService
    {
        public void ShowInformation(string message, string title) { }
        public void ShowWarning(string message, string title) { }
        public void ShowError(string message, string title) { }
        public bool Confirm(string message, string title, bool exclamation = false) => true;
    }
}

/// <summary>
/// Бенчмарки для сравнения производительности парсинга
/// </summary>
[MemoryDiagnoser]
[Orderer(SummaryOrderPolicy.FastestToSlowest)]
[RankColumn]
public class ParsingBenchmarks
{
    private FileLogReader? _reader;
    private const string TestLine = "2025-01-15 10:30:45.1234 | Info | Test message for benchmarking | Application.Core | 1234 | 5";

    [GlobalSetup]
    public void Setup()
    {
        var tempFile = Path.GetTempFileName();
        _reader = new FileLogReader(tempFile, new DummyUserDialogService());
    }

    [GlobalCleanup]
    public void Cleanup()
    {
        _reader?.Dispose();
    }

    [Benchmark(Description = "Парсинг Fast")]
    public bool ParseFast()
    {
        return _reader!.TryParseLogEntryFast(TestLine.AsSpan(), out _);
    }

    [Benchmark(Description = "Парсинг Fast Multiline")]
    public bool ParseFastMultiline()
    {
        return _reader!.TryParseLogEntryFastMultiline(TestLine.AsSpan(), out _);
    }

    private class DummyUserDialogService : IUserDialogService
    {
        public void ShowInformation(string message, string title) { }
        public void ShowWarning(string message, string title) { }
        public void ShowError(string message, string title) { }
        public bool Confirm(string message, string title, bool exclamation = false) => true;
    }
}
