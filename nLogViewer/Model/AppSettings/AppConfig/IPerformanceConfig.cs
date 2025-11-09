using System.ComponentModel;

namespace nLogViewer.Model.AppSettings.AppConfig;

/// <summary>
/// Настройки производительности приложения
/// </summary>
public interface IPerformanceConfig
{
    /// <summary>
    /// Интервал проверки новых записей в файле (миллисекунды)
    /// По умолчанию: 10000 (10 секунд)
    /// Рекомендуется: 5000-10000 для снижения нагрузки на CPU
    /// </summary>
    [DefaultValue(10000)]
    int PollingIntervalMs { get; set; }
}
