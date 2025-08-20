using System;
using System.Diagnostics.CodeAnalysis;
using System.Threading;
using NLog;
using nLogViewer.Model;

namespace nLogViewer.Tester.Model.Events;

internal class EventGenerator
{
    private static Logger _logger = LogManager.GetCurrentClassLogger();
    private int _logCount;
    
    public int LogCount => _logCount;
    
    [SuppressMessage("Interoperability", "CA1416:Validate platform compatibility", 
        Justification = "False positive - LogEntryType.Debug is custom enum, not Windows API")]
    public void Generate(LogEntryType type, IProgress<int> progress)
    {
        var msg = $"Сообщение {++_logCount}";
        
        switch(type)
        {
            case LogEntryType.Trace: _logger.Trace(msg); break;
            case LogEntryType.Debug: _logger.Debug(msg); break;
            case LogEntryType.Info: _logger.Info(msg); break;
            case LogEntryType.Warn: _logger.Warn(msg); break;
            case LogEntryType.Error: _logger.Error(msg); break;
            case LogEntryType.Fatal: _logger.Fatal(msg); break;
            default: throw new ArgumentOutOfRangeException("Тип события не определен");
        }
        progress.Report(_logCount);
    }
    
    public void RandomGenerate(int count, IProgress<int> progress)
    {
        Random random = new Random();
        for(int i = 0; i < count; i++)
        {
            var message = $"Сообщение {++_logCount}";
            switch(random.Next(0,6))
            {
                case 0: _logger.Trace(message); break;
                case 1: _logger.Debug(message); break;
                case 2: _logger.Info(message); break;
                case 3: _logger.Warn(message); break;
                case 4: _logger.Error(message); break;
                case 5: _logger.Fatal(message); break;
            }
            progress.Report(_logCount);
            Thread.Sleep(10);
        }
    }
}