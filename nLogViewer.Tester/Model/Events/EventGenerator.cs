using System;
using System.Threading;
using NLog;
using nLogViewer.Model;

namespace nLogViewer.Tester.Model.Events;

internal class EventGenerator
{
    protected static Logger _log = LogManager.GetCurrentClassLogger();
    private int _logCount;
    private IProgress<int> _progress;

    public int LogCount => _logCount;
    
    public void Generate(LogEntryType type, IProgress<int> progress)
    {
        var msg = $"Сообщение {++_logCount}";
        
        switch(type)
        {
            case LogEntryType.Trace: _log.Trace(msg); break;
            case LogEntryType.Debug: _log.Debug(msg); break;
            case LogEntryType.Info: _log.Info(msg); break;
            case LogEntryType.Warn: _log.Warn(msg); break;
            case LogEntryType.Error: _log.Error(msg); break;
            case LogEntryType.Fatal: _log.Fatal(msg); break;
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
                case 0: _log.Trace(message); break;
                case 1: _log.Debug(message); break;
                case 2: _log.Info(message); break;
                case 3: _log.Warn(message); break;
                case 4: _log.Error(message); break;
                case 5: _log.Fatal(message); break;
            }
            progress.Report(_logCount);
            Thread.Sleep(10);
        }
    }
}