using System.Windows;
using System.Windows.Input;
using nLogViewer.Infrastructure.Commands;
using nLogViewer.ViewModels.Base;

namespace nLogViewer.ViewModels;

internal class MainWindowViewModel : BaseViewModel
{
    
    
    
    public MainWindowViewModel()
    {
        logger.Debug($"Вызов конструктора {this.GetType().Name} по умолчанию");
        
        _title = "Просмоторщик логов";
        
        
        
        #region commands
        NewSession = new LambdaCommand(OnNewSessionExecuted, CanNewSessionExecute);
        LoadSession = new LambdaCommand(OnLoadSessionExecuted, CanLoadSessionExecute);
        SaveSession = new LambdaCommand(OnSaveSessionExecuted, CanSaveSessionExecute);
        ExportLogs = new LambdaCommand(OnExportLogsExecuted, CanExportLogsExecute);
        ShowPreferences = new LambdaCommand(OnShowPreferencesExecuted, CanShowPreferencesExecute);
        About = new LambdaCommand(OnAboutExecuted, CanAboutExecute);
        Exit = new LambdaCommand(OnExitExecuted, CanExitExecute);
        RecentFiles = new LambdaCommand(OnRecentFilesExecuted, CanRecentFilesExecute);
        Add = new LambdaCommand(OnAddExecuted, CanAddExecute);
        #endregion
    }
    
    
    #region Pages
    
    #endregion
    
    #region Commands

    #region NewSession
    public ICommand NewSession { get; }
    private void OnNewSessionExecuted(object p)
    {
        Application.Current.Shutdown();
    }
    private bool CanNewSessionExecute(object p) => true;
    #endregion
    
    #region LoadSession
    public ICommand LoadSession { get; }
    private void OnLoadSessionExecuted(object p)
    {
        Application.Current.Shutdown();
    }
    private bool CanLoadSessionExecute(object p) => true;
    #endregion

    #region SaveSession
    public ICommand SaveSession { get; }
    private void OnSaveSessionExecuted(object p)
    {
        Application.Current.Shutdown();
    }
    private bool CanSaveSessionExecute(object p) => true;
    #endregion
    
    #region ExportLogs
    public ICommand ExportLogs { get; }
    private void OnExportLogsExecuted(object p)
    {
        Application.Current.Shutdown();
    }
    private bool CanExportLogsExecute(object p) => true;
    #endregion
    
    #region ShowPreferences
    public ICommand ShowPreferences { get; }
    private void OnShowPreferencesExecuted(object p)
    {
        Application.Current.Shutdown();
    }
    private bool CanShowPreferencesExecute(object p) => true;
    #endregion
    
    #region About
    public ICommand About { get; }
    private void OnAboutExecuted(object p)
    {
        Application.Current.Shutdown();
    }
    private bool CanAboutExecute(object p) => true;
    #endregion
    
    #region Exit
    public ICommand Exit { get; }
    private void OnExitExecuted(object p)
    {
        Application.Current.Shutdown();
    }
    private bool CanExitExecute(object p) => true;
    #endregion
    
    #region RecentFiles
    public ICommand RecentFiles { get; }
    private void OnRecentFilesExecuted(object p)
    {
        Application.Current.Shutdown();
    }
    private bool CanRecentFilesExecute(object p) => true;
    #endregion

    #region Add
    public ICommand Add { get; }
    private void OnAddExecuted(object p)
    {
        Application.Current.Shutdown();
    }
    private bool CanAddExecute(object p) => true;
    #endregion
    
    #endregion
    
    #region Window title
    
    private string _title;
    /// <summary>
    /// Заголовок окна
    /// </summary>
    public string Title
    {
        get => _title;
        set => Set(ref _title, value);
    }
    #endregion
    
    #region Фильтр событий
    
    private bool _enableTrace;
    /// <summary>
    /// Trace
    /// </summary>
    public bool EnableTrace
    {
        get => _enableTrace;
        set => Set(ref _enableTrace, value);
    }
    
    private bool _enableDebug;
    /// <summary>
    /// Debug
    /// </summary>
    public bool EnableDebug
    {
        get => _enableDebug;
        set => Set(ref _enableDebug, value);
    }
    
    private bool _enableInfo;
    /// <summary>
    /// Info
    /// </summary>
    public bool EnableInfo
    {
        get => _enableInfo;
        set => Set(ref _enableInfo, value);
    }
    
    private bool _enableWarn;
    /// <summary>
    /// Warning
    /// </summary>
    public bool EnableWarn
    {
        get => _enableWarn;
        set => Set(ref _enableWarn, value);
    }
    
    private bool _enableError;
    /// <summary>
    /// Error
    /// </summary>
    public bool EnableError
    {
        get => _enableError;
        set => Set(ref _enableError, value);
    }
    
    private bool _enableFatal;
    /// <summary>
    /// Fatal
    /// </summary>
    public bool EnableFatal
    {
        get => _enableFatal;
        set => Set(ref _enableFatal, value);
    }
    #endregion

}