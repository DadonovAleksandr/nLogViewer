using System.Collections;
using System.Collections.Generic;

namespace nLogViewer.Model.AppSettings.RecentLogs;

public class RecentLogEntries : IEnumerable<RecentLogEntry>
{
    private List<RecentLogEntry> _container;

    public RecentLogEntries()
    {
        _container = new List<RecentLogEntry>();
    }

    public void Add(RecentLogEntry entry) => _container.Add(entry);
    public bool Remove(RecentLogEntry entry) => _container.Remove(entry);
    public void Clear() => _container.Clear();

    public RecentLogEntry this[int index] => _container[index];
    public IEnumerator<RecentLogEntry> GetEnumerator()
    {
        foreach (var entry in _container)
            yield return entry;
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        return GetEnumerator();
    }
}