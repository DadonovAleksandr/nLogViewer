using System;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using Microsoft.Win32;
using nLogViewer.Infrastructure.Commands;
using nLogViewer.Model.Filter;
using nLogViewer.ViewModels.Base;
using nLogViewer.Views;

namespace nLogViewer.ViewModels;

public class MainWindowViewModel : BaseViewModel
{
    private Selector _logViewer;
    public LogEntryFilter Filter;
    public Action RefreshFilter;
    
    public MainWindowViewModel()
    {
        _logger.Debug($"Вызов конструктора {this.GetType().Name} по умолчанию");
        _title = "Просмоторщик логов";
        Filter = new LogEntryFilter();
        
        #region commands
        NewSession = new LambdaCommand(OnNewSessionExecuted, CanNewSessionExecute);
        LoadSession = new LambdaCommand(OnLoadSessionExecuted, CanLoadSessionExecute);
        SaveSession = new LambdaCommand(OnSaveSessionExecuted, CanSaveSessionExecute);
        AddFolder = new LambdaCommand(OnAddFolderExecuted, CanAddFolderExecute);
        DeleteLog = new LambdaCommand(OnSDeleteLogExecuted, CanDeleteLogExecute);
        About = new LambdaCommand(OnAboutExecuted, CanAboutExecute);
        Exit = new LambdaCommand(OnExitExecuted, CanExitExecute);
        RecentFiles = new LambdaCommand(OnRecentFilesExecuted, CanRecentFilesExecute);
        AddFile = new LambdaCommand(OnAddFileExecuted, CanAddFileExecute);
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

    #region Add file
    public ICommand AddFile { get; }
    private void OnAddFileExecuted(object p)
    {
        _logger.Debug("Команда добавить файл лога в просмоторщик");
        var ofd = new OpenFileDialog
        {
            DefaultExt = "log",
            Filter = "log files (*.log)|*.log|All files (*.*)|*.*",
            Title = "Выберите лог-файл"
        };
        if(ofd.ShowDialog() == false)
        {
            _logger.Debug("Диалог выбора файла завершился отменой");
            return;
        }

        if (_logViewer is null)
        {
            _logger.Error("Просмоторщик логов не инициализирован");
            return;
        }
        var newTabLogViewer = new TabItem
        {
            Header = Path.GetFileName(ofd.FileName),
            ToolTip = ofd.FileName,
            Content = new LogViewerView
            {
                DataContext = new LogViewerViewModel(ofd.FileName, this)
            } 
        };
        _logViewer.Items.Add(newTabLogViewer);
        _logViewer.SelectedIndex = _logViewer.Items.Count - 1;
    }
    
    private bool CanAddFileExecute(object p) => true;
    #endregion
    
    #region Add folder
    public ICommand AddFolder { get; }
    private void OnAddFolderExecuted(object p)
    {
        Application.Current.Shutdown();
    }
    private bool CanAddFolderExecute(object p) => true;
    #endregion
    
    #region Delete log
    public ICommand DeleteLog { get; }
    private void OnSDeleteLogExecuted(object p)
    {
        Application.Current.Shutdown();
    }
    private bool CanDeleteLogExecute(object p) => true;
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


    public void InitLogViwerControl(object control)
    {
        if (!(control is TabControl tabControl))
        {
            _logger.Error($"Не возможно ининцилизирвать компонент просмотра лога. Компонент имеет неподходящий тип: {control.GetType().Name}");
            return;
        }
        _logViewer = tabControl;
        _logger.Trace("Промотрщик лога инициализирован");
    }
}