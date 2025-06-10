using System;
using System.Linq;
using System.Windows.Media;
using nLogViewer.Model;

namespace nLogViewer.ViewModels.LogViewerVM;

/// <summary>
/// Обертка для виртуализированного отображения LogEntry
/// </summary>
internal class VirtualLogEntryView : ILogEntry
{
    private readonly int _index;
    private readonly Func<int, ILogEntry> _entryProvider;
    private LogEntryView _cachedView;

    public VirtualLogEntryView(int index, Func<int, ILogEntry> entryProvider)
    {
        _index = index;
        _entryProvider = entryProvider ?? throw new ArgumentNullException(nameof(entryProvider));
    }

    private LogEntryView GetOrCreateView()
    {
        if (_cachedView == null)
        {
            var entry = _entryProvider(_index);
            if (entry != null)
            {
                _cachedView = new LogEntryView(entry);
            }
        }
        return _cachedView;
    }

    public DateTime EventDateTime => GetOrCreateView()?.EventDateTime ?? DateTime.MinValue;
    public LogEntryType EntryType => GetOrCreateView()?.EntryType ?? LogEntryType.Info;
    public string Message => GetOrCreateView()?.Message ?? string.Empty;
    public string Source => GetOrCreateView()?.Source ?? string.Empty;
    public int ProcessId => GetOrCreateView()?.ProcessId ?? 0;
    public int ThreadId => GetOrCreateView()?.ThreadId ?? 0;
    public Brush Background => GetOrCreateView()?.Background ?? new SolidColorBrush(Colors.White);
}