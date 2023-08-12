using nLogViewer.Services.LogReader.Factory;
using nLogViewer.Services.UserDialogService;

namespace nLogViewer.Services.LogReader.FileLogReader;

internal class FileLogReaderFactory : ILogReaderFactory
{
    private readonly IUserDialogService _userDialogService;
    public FileLogReaderFactory(IUserDialogService userDialogService)
    {
        _userDialogService = userDialogService;
    }
    public ILogReader Create()
    {
        var configuration = new FileLogReaderConfiguration();
        return new FileLogReader(configuration.FileName, _userDialogService);
    }
}