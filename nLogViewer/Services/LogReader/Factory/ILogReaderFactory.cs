namespace nLogViewer.Services.LogReader.Factory;

internal interface ILogReaderFactory
{
    ILogReader Create();
}