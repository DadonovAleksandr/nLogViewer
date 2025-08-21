using System.Diagnostics;
using System.Text;
using nLogViewer.Services.LogReader.FileLogReader;
using nLogViewer.Services.UserDialogService;

namespace nLogViewer.Tests;

[TestFixture]
internal class LogParsingBenchmark
{
    private const string TestLogPath = "test_performance.log";
    private const int LogEntryCount = 10000;
    private IUserDialogService _userDialogService;

    [OneTimeSetUp]
    public void OneTimeSetUp()
    {
        _userDialogService = new TestUserDialogService();
        GenerateTestLogFile();
    }

    [OneTimeTearDown]
    public void OneTimeTearDown()
    {
        if (File.Exists(TestLogPath))
        {
            File.Delete(TestLogPath);
        }
    }

    private void GenerateTestLogFile()
    {
        var sb = new StringBuilder();
        var random = new Random(42); // Fixed seed for reproducible results
        var logLevels = new[] { "TRACE", "DEBUG", "INFO", "WARN", "ERROR", "FATAL" };
        var sources = new[] { "MyApp.Service", "MyApp.Controller", "MyApp.Repository", "MyApp.Validator" };

        for (int i = 0; i < LogEntryCount; i++)
        {
            var timestamp = DateTime.Now.AddMilliseconds(-LogEntryCount + i).ToString("yyyy-MM-dd HH:mm:ss.ffff");
            var level = logLevels[random.Next(logLevels.Length)];
            var message = $"This is test log message number {i} with some additional text to make it realistic";
            var source = sources[random.Next(sources.Length)];
            var processId = random.Next(1000, 9999);
            var threadId = random.Next(1, 100);

            sb.AppendLine($"{timestamp} | {level} | {message} | {source} | {processId} | {threadId}");
        }

        File.WriteAllText(TestLogPath, sb.ToString());
    }

    [Test]
    public void BenchmarkLogParsing()
    {
        const int iterations = 5;
        var times = new List<long>();

        Console.WriteLine($"Benchmarking log parsing with {LogEntryCount} entries over {iterations} iterations");
        Console.WriteLine("Format: Iteration | Time (ms) | Entries/sec | Memory (MB)");

        for (int i = 0; i < iterations; i++)
        {
            GC.Collect();
            GC.WaitForPendingFinalizers();
            GC.Collect();

            var initialMemory = GC.GetTotalMemory(false);
            var stopwatch = Stopwatch.StartNew();

            var reader = new FileLogReader(TestLogPath, _userDialogService);
            var entries = reader.GetAll().ToList();

            stopwatch.Stop();
            var finalMemory = GC.GetTotalMemory(false);
            var memoryUsed = (finalMemory - initialMemory) / (1024.0 * 1024.0);

            times.Add(stopwatch.ElapsedMilliseconds);
            var entriesPerSecond = LogEntryCount / (stopwatch.ElapsedMilliseconds / 1000.0);

            Console.WriteLine($"    {i + 1,3}   | {stopwatch.ElapsedMilliseconds,8} | {entriesPerSecond,10:F0} | {memoryUsed,8:F2}");

            Assert.That(entries.Count, Is.EqualTo(LogEntryCount), $"Expected {LogEntryCount} entries, but got {entries.Count}");
            reader.Dispose();
        }

        var avgTime = times.Average();
        var avgEntriesPerSecond = LogEntryCount / (avgTime / 1000.0);

        Console.WriteLine();
        Console.WriteLine($"Average time: {avgTime:F2} ms");
        Console.WriteLine($"Average throughput: {avgEntriesPerSecond:F0} entries/second");
    }

    [Test]
    public void ValidateParsingCorrectness()
    {
        var reader = new FileLogReader(TestLogPath, _userDialogService);
        var entries = reader.GetAll().ToList();

        Assert.That(entries.Count, Is.EqualTo(LogEntryCount));

        // Validate first entry
        var firstEntry = entries.First();
        Assert.That(firstEntry.EventDateTime, Is.Not.EqualTo(default(DateTime)));
        Assert.That(firstEntry.Message, Is.Not.Null.And.Not.Empty);
        Assert.That(firstEntry.Source, Is.Not.Null.And.Not.Empty);
        Assert.That(firstEntry.ProcessId, Is.GreaterThan(0));
        Assert.That(firstEntry.ThreadId, Is.GreaterThan(0));

        // Validate all entries have valid data
        foreach (var entry in entries)
        {
            Assert.That(entry.EventDateTime, Is.Not.EqualTo(default(DateTime)));
            Assert.That(entry.Message, Is.Not.Null);
            Assert.That(entry.Source, Is.Not.Null);
        }

        reader.Dispose();
    }

    private class TestUserDialogService : IUserDialogService
    {
        public void ShowInformation(string message, string title) { }
        public void ShowWarning(string message, string title) { }
        public void ShowError(string message, string title) { }
        public bool Confirm(string message, string title, bool exclamation = false) => false;
    }
}