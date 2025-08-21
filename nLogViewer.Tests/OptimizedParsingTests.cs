using nLogViewer.Model;
using nLogViewer.Services.LogReader.FileLogReader;
using nLogViewer.Services.UserDialogService;

namespace nLogViewer.Tests;

[TestFixture]
internal class OptimizedParsingTests
{
    private FileLogReader _reader;
    private IUserDialogService _userDialogService;
    
    [SetUp]
    public void SetUp()
    {
        _userDialogService = new TestUserDialogService();
        _reader = new FileLogReader("dummy.log", _userDialogService); // File doesn't need to exist for these tests
    }

    [TearDown]
    public void TearDown()
    {
        _reader?.Dispose();
    }

    [Test]
    public void TryParseLogEntryFast_ValidSimpleEntry_ShouldParse()
    {
        // Arrange
        var logLine = "2024-08-21 10:30:45.1234 | INFO | Test message | MyApp.Service | 1234 | 5678".AsSpan();
        
        // Act
        var success = InvokeTryParseLogEntryFast(logLine, out var entry);
        
        // Assert
        Assert.That(success, Is.True);
        Assert.That(entry, Is.Not.Null);
        Assert.That(entry.EventDateTime, Is.EqualTo(new DateTime(2024, 8, 21, 10, 30, 45, 123).AddTicks(4000)));
        Assert.That(entry.EntryType, Is.EqualTo(LogEntryType.Info));
        Assert.That(entry.Message, Is.EqualTo("Test message"));
        Assert.That(entry.Source, Is.EqualTo("MyApp.Service"));
        Assert.That(entry.ProcessId, Is.EqualTo(1234));
        Assert.That(entry.ThreadId, Is.EqualTo(5678));
    }

    [Test]
    public void TryParseLogEntryFast_EntryWithoutThreadId_ShouldParse()
    {
        // Arrange
        var logLine = "2024-08-21 10:30:45.1234 | ERROR | Error occurred | MyApp.Controller | 9999".AsSpan();
        
        // Act
        var success = InvokeTryParseLogEntryFast(logLine, out var entry);
        
        // Assert
        Assert.That(success, Is.True);
        Assert.That(entry, Is.Not.Null);
        Assert.That(entry.EventDateTime, Is.EqualTo(new DateTime(2024, 8, 21, 10, 30, 45, 123).AddTicks(4000)));
        Assert.That(entry.EntryType, Is.EqualTo(LogEntryType.Error));
        Assert.That(entry.Message, Is.EqualTo("Error occurred"));
        Assert.That(entry.Source, Is.EqualTo("MyApp.Controller"));
        Assert.That(entry.ProcessId, Is.EqualTo(9999));
        Assert.That(entry.ThreadId, Is.EqualTo(0));
    }

    [Test]
    public void TryParseLogEntryFast_InvalidFormat_ShouldReturnFalse()
    {
        // Arrange
        var logLine = "Invalid log line format".AsSpan();
        
        // Act
        var success = InvokeTryParseLogEntryFast(logLine, out var entry);
        
        // Assert
        Assert.That(success, Is.False);
        Assert.That(entry, Is.Null);
    }

    [Test]
    public void TryParseLogEntryFast_InvalidDateTime_ShouldReturnFalse()
    {
        // Arrange
        var logLine = "2024-13-45 25:70:90.9999 | INFO | Test message | MyApp.Service | 1234 | 5678".AsSpan();
        
        // Act
        var success = InvokeTryParseLogEntryFast(logLine, out var entry);
        
        // Assert
        Assert.That(success, Is.False);
        Assert.That(entry, Is.Null);
    }

    [Test]
    public void TryParseLogEntryFast_WithSpacesAroundPipes_ShouldParse()
    {
        // Arrange
        var logLine = "2024-08-21 10:30:45.1234  |  DEBUG  |  Debug message with spaces  |  MyApp.Repository  |  4567  |  8901".AsSpan();
        
        // Act
        var success = InvokeTryParseLogEntryFast(logLine, out var entry);
        
        // Assert
        Assert.That(success, Is.True);
        Assert.That(entry, Is.Not.Null);
        Assert.That(entry.EntryType, Is.EqualTo(LogEntryType.Debug));
        Assert.That(entry.Message, Is.EqualTo("Debug message with spaces"));
        Assert.That(entry.Source, Is.EqualTo("MyApp.Repository"));
        Assert.That(entry.ProcessId, Is.EqualTo(4567));
        Assert.That(entry.ThreadId, Is.EqualTo(8901));
    }

    [Test]
    public void TryParseLogEntryFast_AllLogLevels_ShouldParseCorrectly()
    {
        var testCases = new[]
        {
            ("TRACE", LogEntryType.Trace),
            ("DEBUG", LogEntryType.Debug),
            ("INFO", LogEntryType.Info),
            ("WARN", LogEntryType.Warn),
            ("ERROR", LogEntryType.Error),
            ("FATAL", LogEntryType.Fatal)
        };

        foreach (var (levelString, expectedLevel) in testCases)
        {
            // Arrange
            var logLine = $"2024-08-21 10:30:45.1234 | {levelString} | Test message | MyApp.Service | 1234 | 5678".AsSpan();
            
            // Act
            var success = InvokeTryParseLogEntryFast(logLine, out var entry);
            
            // Assert
            Assert.That(success, Is.True, $"Failed to parse {levelString} level");
            Assert.That(entry.EntryType, Is.EqualTo(expectedLevel), $"Wrong log level for {levelString}");
        }
    }

    private bool InvokeTryParseLogEntryFast(ReadOnlySpan<char> logLine, out ILogEntry? entry)
    {
        // Call the internal method directly
        return _reader.TryParseLogEntryFast(logLine, out entry);
    }

    private class TestUserDialogService : IUserDialogService
    {
        public void ShowInformation(string message, string title) { }
        public void ShowWarning(string message, string title) { }
        public void ShowError(string message, string title) { }
        public bool Confirm(string message, string title, bool exclamation = false) => false;
    }
}