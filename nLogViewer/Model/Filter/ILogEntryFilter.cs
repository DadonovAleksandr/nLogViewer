namespace nLogViewer.Model.Filter;

public interface ILogEntryFilter
{
    bool CheckFilter(ILogEntry entry);
}