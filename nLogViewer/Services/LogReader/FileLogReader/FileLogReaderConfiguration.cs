namespace nLogViewer.Services.LogReader.FileLogReader;

internal class FileLogReaderConfiguration
{
    private string _fileName;
    
    public FileLogReaderConfiguration(string fileName = null)
    {
        _fileName = fileName;
    }
    
    public string FileName 
    {
        get => _fileName;
        set => _fileName = value;
    }
    
    public string[] Args => new string[] { _fileName };
}