namespace nLogViewer.Tester.Model.AppSettings.AppConfig;

internal class AppConst
{
    private static AppConst? _instance;

    public static AppConst Get() => _instance ??= new AppConst();

    public string AppName => "nLogViewer.Tester";
    public string AppDesciption => "Тестирование просмоторщика логов";
}