using System;
using System.Collections.Generic;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Microsoft.Win32;
using nLogViewer.Infrastructure.Commands;
using nLogViewer.ViewModels.Base;
using nLogViewer.Views;

namespace nLogViewer.ViewModels;

internal class MainWindowViewModel : BaseViewModel
{
    public LogViewerViewModel LogViewer { get; }
    
    
    public MainWindowViewModel()
    {
        logger.Debug($"Вызов конструктора {this.GetType().Name} по умолчанию");
        _title = "Просмоторщик логов";
        
        DeleteTempFileLog();
        
        
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
        MessageBox.Show("Hello!");
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
        var ofd = new OpenFileDialog
        {
            DefaultExt = "log",
            Filter = "log files (*.log)|*.log|All files (*.*)|*.*",
            Title = "Выберите лог-файл"
        };
        if(ofd.ShowDialog() == false)
            return;
        if(!(p is MainWindow window))
            return;
        var newTabLogViewer = new TabItem
        {
            Header = Path.GetFileName(ofd.FileName),
            ToolTip = ofd.FileName,
            Content = new LogViewerView
            {
                DataContext = new LogViewerViewModel(ofd.FileName)
            } 
        };
        window.LogViewersControl.Items.Add(newTabLogViewer);
        window.LogViewersControl.SelectedIndex = window.LogViewersControl.Items.Count - 1;
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
    
    private bool _enableTraceEvent;
    /// <summary>
    /// Trace
    /// </summary>
    public bool EnableTraceEvent
    {
        get => _enableTraceEvent;
        set => Set(ref _enableTraceEvent, value);
    }
    
    private bool _enableDebugEvent;
    /// <summary>
    /// Debug
    /// </summary>
    public bool EnableDebugEvent
    {
        get => _enableDebugEvent;
        set => Set(ref _enableDebugEvent, value);
    }
    
    private bool _enableInfoEvent;
    /// <summary>
    /// Info
    /// </summary>
    public bool EnableInfoEvent
    {
        get => _enableInfoEvent;
        set => Set(ref _enableInfoEvent, value);
    }
    
    private bool _enableWarnEvent;
    /// <summary>
    /// Warning
    /// </summary>
    public bool EnableWarnEvent
    {
        get => _enableWarnEvent;
        set => Set(ref _enableWarnEvent, value);
    }
    
    private bool _enableErrorEvent;
    /// <summary>
    /// Error
    /// </summary>
    public bool EnableErrorEvent
    {
        get => _enableErrorEvent;
        set => Set(ref _enableErrorEvent, value);
    }
    
    private bool _enableFatalEvent;
    /// <summary>
    /// Fatal
    /// </summary>
    public bool EnableFatalEvent
    {
        get => _enableFatalEvent;
        set => Set(ref _enableFatalEvent, value);
    }
    #endregion

    //TODO: Нужно делать сохранение настроек
    /// <summary>
    /// Удаление воеменных файл-логов
    /// </summary>
    private void DeleteTempFileLog()
    {
        var appDir = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
        var tempDir = Path.Combine(appDir, "LogViewer", "temp");
        
        if(!Directory.Exists(tempDir))
            return;
        
        var dirInfo = new DirectoryInfo(tempDir);
        foreach (FileInfo file in dirInfo.GetFiles())
        {
            file.Delete();
        }
    }
    
    
}