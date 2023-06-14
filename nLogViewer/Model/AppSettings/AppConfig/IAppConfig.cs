using System;
using System.IO;
using Config.Net;

namespace nLogViewer.Model.AppSettings.AppConfig;

public interface IAppConfig
{
    IFilterConfig FilterConfig { get; set; }
}

public static class AppConfig
{
    internal static IAppConfig GetConfigFromDefaultPath()
    {
        var appDataPath = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
        var configPath = Path.Combine(appDataPath, "nLogViewer", "settings.json");
        return new ConfigurationBuilder<IAppConfig>()
            .UseJsonFile(configPath)
            .Build();
    }
}