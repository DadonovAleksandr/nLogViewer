using Moq;
using nLogViewer.Model;
using nLogViewer.Services.LogReader.FileLogReader;
using nLogViewer.Services.UserDialogService;

[TestFixture]
public class FileLogReaderTests
{
    private Mock<IUserDialogService> _dialogServiceMock;
    private string _testFilePath;
    private FileLogReader _reader;

    [SetUp]
    public void SetUp()
    {
        _dialogServiceMock = new Mock<IUserDialogService>();
        _testFilePath = Path.GetTempFileName();
        _reader = new FileLogReader(_testFilePath, _dialogServiceMock.Object);
    }

    [TearDown]
    public void TearDown()
    {
        if (File.Exists(_testFilePath))
            File.Delete(_testFilePath);
    }

    [Test]
    public void GetAll_SingleLineLog_ReturnsCorrectEntry()
    {
        // Arrange
        string logContent = "2025-03-20 19:40:26.8972 | TRACE | Test message | Source | 123 | 456";
        File.WriteAllText(_testFilePath, logContent);

        // Act
        var entries = new List<ILogEntry>();
        foreach (var logEntry in _reader.GetAll())
        {
            entries.Add(logEntry);
        }

        // Assert
        Assert.That(entries.Count, Is.EqualTo(1));
        var entry = entries[0];
        Assert.That(entry.EventDateTime, Is.EqualTo(new DateTime(2025, 3, 20, 19, 40, 26, 897)));
        Assert.That(entry.EntryType, Is.EqualTo(LogEntryType.Trace));
        Assert.That(entry.Message, Is.EqualTo("Test message"));
        Assert.That(entry.Source, Is.EqualTo("Source"));
        Assert.That(entry.ProcessId, Is.EqualTo(123));
        Assert.That(entry.ThreadId, Is.EqualTo(456));
    }

    [Test]
    public void GetAll_MultiLineMessage_ReturnsSingleEntry()
    {
        // Arrange
        string logContent = 
            "2025-03-20 19:40:26.8972 | DEBUG | Multi-line message:\n" +
            "Line 2\n" +
            "Line 3 | Source | 123 | 456\n" +
            "2025-03-20 19:40:27.0000 | TRACE | Next entry | Source2 | 789 | 101";
        File.WriteAllText(_testFilePath, logContent);

        // Act
        var entries = new List<ILogEntry>();
        foreach (var logEntry in _reader.GetAll())
        {
            entries.Add(logEntry);
        }

        // Assert
        Assert.That(entries.Count, Is.EqualTo(2));
        var firstEntry = entries[0];
        Assert.That(firstEntry.Message, Is.EqualTo("Multi-line message:\nLine 2\nLine 3"));
        Assert.That(firstEntry.EntryType, Is.EqualTo(LogEntryType.Debug));
        Assert.That(firstEntry.ProcessId, Is.EqualTo(123));
        Assert.That(firstEntry.ThreadId, Is.EqualTo(456));

        var secondEntry = entries[1];
        Assert.That(secondEntry.Message, Is.EqualTo("Next entry"));
        Assert.That(secondEntry.EntryType, Is.EqualTo(LogEntryType.Trace));
    }

    [Test]
    public void GetAll_InvalidLine_LogsError()
    {
        // Arrange
        string logContent = "Invalid line\n2025-03-20 19:40:26.8972 | TRACE | Valid | Source | 123 | 456";
        File.WriteAllText(_testFilePath, logContent);

        // Act
        var entries = new List<ILogEntry>();
        foreach (var logEntry in _reader.GetAll())
        {
            entries.Add(logEntry);
        }

        // Assert
        Assert.That(entries.Count, Is.EqualTo(1));
        _dialogServiceMock.Verify(x => x.ShowError(It.IsAny<string>(), It.IsAny<string>()), Times.Once());
    }

    [Test]
    public void GetNew_TracksPosition()
    {
        // Arrange
        string initialContent = "2025-03-20 19:40:26.8972 | TRACE | First | Source | 123 | 456\n";
        File.WriteAllText(_testFilePath, initialContent);

        // Act 1: Первое чтение
        var firstEntries = new List<ILogEntry>();
        foreach (var logEntry in _reader.GetNew())
        {
            firstEntries.Add(logEntry);
        }

        // Добавляем новую строку
        File.AppendAllText(_testFilePath, "2025-03-20 19:40:27.0000 | DEBUG | Second | Source2 | 789 | 101\n");

        // Act 2: Чтение новых записей
        var newEntries = new List<ILogEntry>();
        foreach (var logEntry in _reader.GetNew())
        {
            newEntries.Add(logEntry);
        }

        // Assert
        Assert.That(firstEntries.Count, Is.EqualTo(1));
        Assert.That(firstEntries[0].Message, Is.EqualTo("First"));
        Assert.That(newEntries.Count, Is.EqualTo(1));
        Assert.That(newEntries[0].Message, Is.EqualTo("Second"));
    }
} 