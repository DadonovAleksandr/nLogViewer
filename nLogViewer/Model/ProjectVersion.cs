using System;
using System.Reflection;
using System.Threading;

namespace nLogViewer.Model;

internal class ProjectVersion
{
    private static readonly Lazy<ProjectVersion> _instance = 
        new(() => new ProjectVersion(), LazyThreadSafetyMode.ExecutionAndPublication);

    private readonly Version _version;
    private readonly string _versionString;
    
    private ProjectVersion()
    {
        var assemblyVersion = Assembly.GetExecutingAssembly().GetName().Version;
        _version = assemblyVersion ?? new Version(0, 0, 0);
        _versionString = $"{_version.Major}.{_version.Minor}.{_version.Build}";
    }

    public static string Get() => _instance.Value._versionString;
}