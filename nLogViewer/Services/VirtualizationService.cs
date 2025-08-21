using System.Windows.Controls;
using nLogViewer.Model.AppSettings.AppConfig;

namespace nLogViewer.Services;

/// <summary>
/// Сервис для управления настройками виртуализации DataGrid
/// </summary>
public interface IVirtualizationService
{
    /// <summary>
    /// Применить настройки виртуализации к DataGrid
    /// </summary>
    void ApplyVirtualizationSettings(DataGrid dataGrid, int itemCount = 0);
    
    /// <summary>
    /// Получить текущие настройки виртуализации
    /// </summary>
    IUIConfig GetVirtualizationSettings();
}

public class VirtualizationService : IVirtualizationService
{
    private readonly IAppConfig _appConfig;
    
    public VirtualizationService(IAppConfig appConfig)
    {
        _appConfig = appConfig;
    }
    
    public void ApplyVirtualizationSettings(DataGrid dataGrid, int itemCount = 0)
    {
        var settings = GetVirtualizationSettings();
        
        if (settings == null)
        {
            // Значения по умолчанию - без виртуализации для многострочных логов
            SetVirtualizationSettings(dataGrid, false, false, false, "Recycling");
            return;
        }
        
        bool enableVirtualization = settings.EnableRowVirtualization;
        
        // Автоматическое переключение виртуализации
        if (settings.AutoVirtualization && itemCount > 0)
        {
            enableVirtualization = itemCount > settings.MaxItemsWithoutVirtualization;
        }
        
        SetVirtualizationSettings(
            dataGrid,
            enableVirtualization,
            settings.EnablePanelVirtualization,
            settings.EnableCanContentScroll,
            settings.VirtualizationMode
        );
    }
    
    public IUIConfig GetVirtualizationSettings()
    {
        var settings = _appConfig?.UIConfig;
        
        // Если настройки еще не существуют, создаем с умолчаниями
        if (settings == null && _appConfig != null)
        {
            // Config.Net автоматически создаст настройки при первом обращении
            settings = _appConfig.UIConfig;
            if (settings != null)
            {
                settings.InitializeDefaults();
            }
        }
        
        return settings;
    }
    
    private void SetVirtualizationSettings(
        DataGrid dataGrid,
        bool enableRowVirtualization,
        bool enablePanelVirtualization,
        bool enableCanContentScroll,
        string virtualizationMode)
    {
        // Установка настроек виртуализации
        dataGrid.EnableRowVirtualization = enableRowVirtualization;
        System.Windows.Controls.VirtualizingStackPanel.SetIsVirtualizing(dataGrid, enablePanelVirtualization && enableRowVirtualization);
        System.Windows.Controls.ScrollViewer.SetCanContentScroll(dataGrid, enableCanContentScroll && enableRowVirtualization);
        
        // Режим виртуализации
        if (enableRowVirtualization)
        {
            var mode = virtualizationMode == "Standard" 
                ? System.Windows.Controls.VirtualizationMode.Standard 
                : System.Windows.Controls.VirtualizationMode.Recycling;
            System.Windows.Controls.VirtualizingStackPanel.SetVirtualizationMode(dataGrid, mode);
        }
    }
}