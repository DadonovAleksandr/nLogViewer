namespace nLogViewer.Services.LogViewer;

public delegate void EntriesChanged();
internal interface ILogViewer
{
    event EntriesChanged EntriesChanged;

    void Start();
    void Stop();
    void Pause();
    void Clear();
}