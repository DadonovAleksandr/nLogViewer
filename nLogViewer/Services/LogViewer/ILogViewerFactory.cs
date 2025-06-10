using nLogViewer.Services.Progress;

namespace nLogViewer.Services.LogViewer;

/// <summary>
/// Фабрика для создания экземпляров ILogViewer
/// </summary>
internal interface ILogViewerFactory
{
    /// <summary>
    /// Создать экземпляр LogViewer
    /// </summary>
    ILogViewer Create();
    
    /// <summary>
    /// Создать экземпляр LogViewer с поддержкой прогресса
    /// </summary>
    ILogViewer Create(IProgressReporter progressReporter);
}