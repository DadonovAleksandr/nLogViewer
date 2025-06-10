namespace nLogViewer.Infrastructure.Configuration;

/// <summary>
/// Конфигурация для управления использованием памяти
/// </summary>
internal class MemoryConfiguration
{
    /// <summary>
    /// Максимальное количество записей в памяти
    /// </summary>
    public int MaxEntriesInMemory { get; set; } = 100_000;
    
    /// <summary>
    /// Включить виртуализацию данных
    /// </summary>
    public bool EnableDataVirtualization { get; set; } = true;
    
    /// <summary>
    /// Размер страницы для виртуализации
    /// </summary>
    public int VirtualizationPageSize { get; set; } = 100;
    
    /// <summary>
    /// Максимальное количество кэшированных страниц
    /// </summary>
    public int MaxCachedPages { get; set; } = 10;
    
    /// <summary>
    /// Использовать кольцевой буфер для ограничения памяти
    /// </summary>
    public bool UseCircularBuffer { get; set; } = true;
}