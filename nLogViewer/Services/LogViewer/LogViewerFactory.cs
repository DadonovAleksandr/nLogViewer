using System;
using Microsoft.Extensions.DependencyInjection;
using nLogViewer.Infrastructure.Configuration;
using nLogViewer.Services.LogReader.Factory;
using nLogViewer.Services.Progress;

namespace nLogViewer.Services.LogViewer;

/// <summary>
/// Реализация фабрики для создания экземпляров LogViewer
/// </summary>
internal class LogViewerFactory : ILogViewerFactory
{
    private readonly IServiceProvider _serviceProvider;
    
    public LogViewerFactory(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }
    
    public ILogViewer Create()
    {
        var readerFactory = _serviceProvider.GetRequiredService<ILogReaderFactory>();
        var memoryConfig = _serviceProvider.GetRequiredService<MemoryConfiguration>();
        
        return new LogViewer(readerFactory, memoryConfig);
    }
    
    public ILogViewer Create(IProgressReporter progressReporter)
    {
        var readerFactory = _serviceProvider.GetRequiredService<ILogReaderFactory>();
        var memoryConfig = _serviceProvider.GetRequiredService<MemoryConfiguration>();
        
        return new LogViewer(readerFactory, memoryConfig, progressReporter);
    }
}