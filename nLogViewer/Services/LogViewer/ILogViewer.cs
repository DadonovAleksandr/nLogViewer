using System.Collections.Generic;
using nLogViewer.Model;

namespace nLogViewer.Services.LogViewer;

public delegate void EntriesChanged();
internal interface ILogViewer
{
    event EntriesChanged EntriesChanged;

    int Count { get; }
    LogViewerState State { get; }
    
    void Start();
    void Stop();
    void Pause();
    void Clear();

    IEnumerable<ILogEntry> GetEntries(int count = 0);
}