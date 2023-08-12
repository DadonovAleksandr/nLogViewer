namespace nLogViewer.Services.LogReader.FileLogReader;

internal class FileLogReaderConfiguration : ILogReaderConfiguration
{
    private static string _fileName;
    public string FileName 
    {
        get => _fileName;
        set => _fileName = value;
    }
    
    public string[] Args => new string[] { _fileName };
}