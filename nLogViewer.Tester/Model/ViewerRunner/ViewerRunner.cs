using System.Diagnostics;

namespace nLogViewer.Tester.Model.ViewerRunner;

internal class ViewerRunner
{
    private readonly string _fileName = @"dotnet";
    private readonly string _arguments = @"run --no-build --configuration Debug";
    private readonly string _workingDirectory = @"..\..\..\..\nLogViewer";
    
    public void Start()
    {
        var processInfo = new ProcessStartInfo()
        {
            FileName = _fileName,
            Arguments = _arguments,
            WorkingDirectory = _workingDirectory
        };
        Process.Start(processInfo);
    }
}