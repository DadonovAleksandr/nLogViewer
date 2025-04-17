using System;
using System.Collections.Generic;
using nLogViewer.Model;

namespace nLogViewer.Services.LogViewer;

public delegate void EntriesChanged();
public interface ILogViewer : IDisposable
{
    event EntriesChanged EntriesChanged;

    int Count { get; }
    LogViewerState State { get; }
    List<ILogEntry> LogEntries { get; }
    
    void Start();
    void Stop();
    void Pause();
    void Clear();

    IEnumerable<ILogEntry> GetEntries(int count = 0);
}