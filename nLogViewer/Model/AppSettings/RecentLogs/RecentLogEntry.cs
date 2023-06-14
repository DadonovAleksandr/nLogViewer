namespace nLogViewer.Model.AppSettings.RecentLogs;

public class RecentLogEntry
{
    public string Path { get; }
    public bool IsFolder { get; }

    public RecentLogEntry(string path, bool isFolder = false)
    {
        Path = path;
        IsFolder = isFolder;
    }
}