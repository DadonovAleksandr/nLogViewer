using System;
using System.Reflection;

namespace nLogViewer.Model;

internal class ProjectVersion
{
    private static ProjectVersion _instance;

    public ProjectVersion()
    {
        _version = Assembly.GetExecutingAssembly().GetName().Version;
    }

    public static Version Get()
    {
        if (_instance is null)
            _instance = new ProjectVersion();
        return _instance.Version;
    }
    
    #region Версия сборки
    private Version _version;

    /// <summary>
    /// Версия сборки
    /// </summary>
    public Version Version => _version;
    #endregion
}