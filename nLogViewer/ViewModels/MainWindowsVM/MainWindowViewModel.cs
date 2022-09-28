using System;
using System.Collections.Generic;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Microsoft.Win32;
using nLogViewer.Infrastructure.Commands;
using nLogViewer.Model.Filter;
using nLogViewer.ViewModels.Base;
using nLogViewer.Views;

namespace nLogViewer.ViewModels;

public class MainWindowViewModel : BaseViewModel
{
    public LogViewerViewModel LogViewer { get; }
    public LogEntryFilter Filter;
    public Action RefreshFilter;
    
    public MainWindowViewModel()
    {
        logger.Debug($"Вызов конструктора {this.GetType().Name} по умолчанию");
        _title = "Просмоторщик логов";
        Filter = new LogEntryFilter();
        
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
        SetFilter = new LambdaCommand(OnSetFilterExecuted, CanSetFilterExecute);
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
                DataContext = new LogViewerViewModel(ofd.FileName, this)
            } 
        };
        window.LogViewersControl.Items.Add(newTabLogViewer);
        window.LogViewersControl.SelectedIndex = window.LogViewersControl.Items.Count - 1;
    }
    private bool CanAddExecute(object p) => true;
    #endregion
    
    #region SetFilter
    public ICommand SetFilter { get; }
    private void OnSetFilterExecuted(object p) => RefreshFilter?.Invoke();
    private bool CanSetFilterExecute(object p) => true;
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
    /// <summary>
    /// Trace
    /// </summary>
    public bool EnableTraceEvent
    {
        get => Filter.EnableTraceEvent;
        set => Set(ref Filter.EnableTraceEvent, value);
    } 
    /// <summary>
    /// Debug
    /// </summary>
    public bool EnableDebugEvent
    {
        get => Filter.EnableDebugEvent;
        set => Set(ref Filter.EnableDebugEvent, value);
    }
    /// <summary>
    /// Info
    /// </summary>
    public bool EnableInfoEvent
    {
        get => Filter.EnableInfoEvent;
        set => Set(ref Filter.EnableInfoEvent, value);
    }
    /// <summary>
    /// Warning
    /// </summary>
    public bool EnableWarnEvent
    {
        get => Filter.EnableWarnEvent;
        set => Set(ref Filter.EnableWarnEvent, value);
    }
    /// <summary>
    /// Error
    /// </summary>
    public bool EnableErrorEvent
    {
        get => Filter.EnableErrorEvent;
        set => Set(ref Filter.EnableErrorEvent, value);
    }
    /// <summary>
    /// Fatal
    /// </summary>
    public bool EnableFatalEvent
    {
        get => Filter.EnableFatalEvent;
        set => Set(ref Filter.EnableFatalEvent, value);
    }
    #endregion
    
}