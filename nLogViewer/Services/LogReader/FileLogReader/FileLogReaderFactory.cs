using nLogViewer.Services.LogReader.Factory;
using nLogViewer.Services.LogReader.Repository;
using nLogViewer.Services.UserDialogService;

namespace nLogViewer.Services.LogReader.FileLogReader;

internal class FileLogReaderFactory : ILogReaderFactory
{
    private readonly IUserDialogService _userDialogService;
    private readonly ILogRepositoryFactory _repositoryFactory;
    
    public FileLogReaderFactory(IUserDialogService userDialogService, ILogRepositoryFactory repositoryFactory)
    {
        _userDialogService = userDialogService;
        _repositoryFactory = repositoryFactory;
    }
    
    public ILogSource Create()
    {
        var configuration = new FileLogReaderConfiguration();
        
        // Используем новую архитектуру с репозиториями
        var repository = _repositoryFactory.Create(LogSourceType.File, configuration.FileName);
        return new RepositoryLogSource(repository);
    }
}