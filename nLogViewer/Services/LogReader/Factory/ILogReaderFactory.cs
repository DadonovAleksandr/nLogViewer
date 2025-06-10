namespace nLogViewer.Services.LogReader.Factory;

internal interface ILogReaderFactory
{
    ILogSource Create();
}