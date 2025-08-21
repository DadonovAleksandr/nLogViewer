namespace nLogViewer.Model.AppSettings.AppConfig;

/// <summary>
/// Настройки пользовательского интерфейса
/// </summary>
public interface IUIConfig
{
    /// <summary>
    /// Включить виртуализацию строк в DataGrid
    /// </summary>
    bool EnableRowVirtualization { get; set; }
    
    /// <summary>
    /// Включить виртуализацию панели
    /// </summary>
    bool EnablePanelVirtualization { get; set; }
    
    /// <summary>
    /// Включить скролл по содержимому (pixel-based scrolling)
    /// </summary>
    bool EnableCanContentScroll { get; set; }
    
    /// <summary>
    /// Режим виртуализации (Recycling/Standard)
    /// </summary>
    string VirtualizationMode { get; set; }
    
    /// <summary>
    /// Максимальное количество элементов для отображения без виртуализации
    /// </summary>
    int MaxItemsWithoutVirtualization { get; set; }
    
    /// <summary>
    /// Автоматически переключать виртуализацию в зависимости от количества записей
    /// </summary>
    bool AutoVirtualization { get; set; }
}

public static class UIConfigExtensions
{
    public static void InitializeDefaults(this IUIConfig config)
    {
        config.EnableRowVirtualization = false; // По умолчанию отключено для многострочных логов
        config.EnablePanelVirtualization = true;
        config.EnableCanContentScroll = false; // Лучше для многострочных логов
        config.VirtualizationMode = "Recycling";
        config.MaxItemsWithoutVirtualization = 1000;
        config.AutoVirtualization = true;
    }
}