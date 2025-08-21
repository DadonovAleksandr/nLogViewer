namespace nLogViewer.Model.AppSettings.AppConfig;

internal class AppConst
{
    private static AppConst _instance;
    public static AppConst Get() => _instance ??= new AppConst();

    public string AppName => "Просмоторщик логов";
    public string AppDesciption => "Программа для просмотра и анализа NLog файлов";
}