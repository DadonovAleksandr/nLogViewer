﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Win32;
using nLogViewer.Infrastructure.Commands;
using nLogViewer.Infrastructure.Helpers.FileProvider;
using nLogViewer.Model;
using nLogViewer.Model.AppSettings.AppConfig;
using nLogViewer.Model.AppSettings.RecentLogs;
using nLogViewer.Model.Filter;
using nLogViewer.ViewModels.Base;
using nLogViewer.Views;
using Ookii.Dialogs.Wpf;

namespace nLogViewer.ViewModels;

internal class MainWindowViewModel : BaseViewModel
{
    private Selector _logViewer;
    private IRecentLogsRepository _recentLogs;
    private IAppConfig _appConfig;
    public LogEntryFilter Filter;
    public Action RefreshFilter;
    
    
    public MainWindowViewModel()
    {
        _logger.Debug($"Вызов конструктора {this.GetType().Name} по умолчанию");
        _title = "Просмоторщик логов";
        _appConfig = AppConfig.GetConfigFromDefaultPath();
        Filter = new LogEntryFilter(_appConfig.FilterConfig);
        

        #region commands
        AddFile = new LambdaCommand(OnAddFileExecuted, CanAddFileExecute);
        AddFolder = new LambdaCommand(OnAddFolderExecuted, CanAddFolderExecute);
        DeleteLog = new LambdaCommand(OnDeleteLogExecuted, CanDeleteLogExecute);
        SetFilter = new LambdaCommand(OnSetFilterExecuted, CanSetFilterExecute);
        SetSearch = new LambdaCommand(OnSetSearchExecuted, CanSetSearchExecute);
        About = new LambdaCommand(OnAboutExecuted, CanAboutExecute);
        Exit = new LambdaCommand(OnExitExecuted, CanExitExecute);
        #endregion
    }


    #region Pages
    
    #endregion
    
    #region Commands
    
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
        AddNewLogViewer(ofd.FileName);
        _recentLogs.Add(ofd.FileName);
    }
    
    private bool CanAddFileExecute(object p) => true;
    #endregion
    
    #region Add folder
    public ICommand AddFolder { get; }
    private void OnAddFolderExecuted(object p)
    {
        _logger.Debug("Команда добавить директорию лога в просмоторщик");
        var ofd = new VistaFolderBrowserDialog()
        {
            Description = "Выберите директорию логов",
            UseDescriptionForTitle = true,
            Multiselect = false
        };
        var dialogResult = ofd.ShowDialog();
        if (!dialogResult ?? false)
        {
            _logger.Debug("Диалог выбора директории завершился отменой");
            return;
        }
        var selectedFolder = ofd.SelectedPath;
        if (!Directory.Exists(selectedFolder))
        {
            _logger.Error($"Директория {selectedFolder} несуществует");
            return;
        }

        var filePath = FindLastFileInDirectory(selectedFolder);
        if (string.IsNullOrEmpty(filePath))
            return;
        
        AddNewLogViewer(filePath);
        _recentLogs.Add(selectedFolder, true);
    }
    private bool CanAddFolderExecute(object p) => true;
    #endregion
    
    #region Delete log
    public ICommand DeleteLog { get; }
    private void OnDeleteLogExecuted(object p)
    {
        _logger.Debug("Команда удалить текущий лог из просмоторщика");
        _recentLogs.Remove(_recentLogs[_logViewer.SelectedIndex]);
        _logViewer.Items.Remove(_logViewer.SelectedItem);
    }
    private bool CanDeleteLogExecute(object p) => !(_logViewer is null) && _logViewer.Items.Count > 0;
    #endregion
    
    #region SetFilter
    public ICommand SetFilter { get; }
    private void OnSetFilterExecuted(object p) => RefreshFilter?.Invoke();
    private bool CanSetFilterExecute(object p) => true;
    #endregion
    
    #region SetSearch
    public ICommand SetSearch { get; }
    private void OnSetSearchExecuted(object p) => RefreshFilter?.Invoke();
    private bool CanSetSearchExecute(object p) => true;
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
    private void OnExitExecuted(object p) => Application.Current.Shutdown();
    private bool CanExitExecute(object p) => true;
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

    #region Поиск по тексту

    /// <summary>
    /// Включение поиска
    /// </summary>
    public bool EnableSearchEvent
    {
        get => Filter.EnableTextSearch;
        set => Set(ref Filter.EnableTextSearch, value);
    } 
    
    /// <summary>
    /// Текст поиска
    /// </summary>
    public string SearchText
    {
        get => Filter.TextSearch;
        set => Set(ref Filter.TextSearch, value);
    }
    
    #endregion

    /// <summary>
    /// Инициализация просмотощика лога
    /// </summary>
    /// <param name="control"></param>
    public void InitLogViewerControl(object control)
    {
        if (!(control is TabControl tabControl))
        {
            _logger.Error($"Не возможно ининцилизирвать компонент просмотра лога. Компонент имеет неподходящий тип: {control.GetType().Name}");
            return;
        }
        _logViewer = tabControl;
        _logger.Trace("Промотрщик лога инициализирован");
        
        _recentLogs = InitRecentLogsRepository();
        if (_recentLogs.Any())
            ViewRecentLogs();
    }
    
    /// <summary>
    /// Действия выполняемые при закрытии основной формы
    /// </summary>
    public void OnExit()
    {
        _recentLogs?.Save();

        #region Cохранение настроек фильтра
        _appConfig.FilterConfig.EnableTraceEvent = Filter.EnableTraceEvent;
        _appConfig.FilterConfig.EnableDebugEvent = Filter.EnableDebugEvent;
        _appConfig.FilterConfig.EnableInfoEvent = Filter.EnableInfoEvent;
        _appConfig.FilterConfig.EnableWarnEvent = Filter.EnableWarnEvent;
        _appConfig.FilterConfig.EnableErrorEvent = Filter.EnableErrorEvent;
        _appConfig.FilterConfig.EnableFatalEvent = Filter.EnableFatalEvent;

        _appConfig.FilterConfig.EnableTextSearch = Filter.EnableTextSearch;
        _appConfig.FilterConfig.TextSearch = Filter.TextSearch;
        #endregion
    }

    /// <summary>
    /// Загрузка ранее сохраненных логов
    /// </summary>
    /// <exception cref="NotImplementedException"></exception>
    private void ViewRecentLogs()
    {
        foreach (var entry in _recentLogs)
        {
            _logger.Debug($"Открываем ранее {(entry.IsFolder ? "открытую директорию" : "открытый лог-файл")} \"{entry.Path}\"");
            var path = entry.IsFolder ? FindLastFileInDirectory(entry.Path) : entry.Path;

            if (string.IsNullOrEmpty(path) || !File.Exists(path))
            {
                _logger.Warn($"Файл \"{path}\" не существует. Удаляем данную запись из списка ранее открытых логов");
                _recentLogs.Remove(entry);
            }
            AddNewLogViewer(path);
        }
    }

    private string FindLastFileInDirectory(string dirPath)
    {
        var directoryInfo = new DirectoryInfo(dirPath);
        var filesInfo = directoryInfo.GetFiles();
        if (filesInfo.Length == 0 && filesInfo.All(x => x.Extension != ".log"))
        {
            _logger.Error($"В директории {dirPath} отсутствуют лог-файлы");
            return string.Empty;
        }
        var lastFile = filesInfo.Where(x => x.Extension == ".log").OrderBy(x => x.LastWriteTime).Last();
        _logger.Debug($"Последний измененный файл в заданной директории {lastFile.FullName}");
        return lastFile.FullName;
    }
    
    /// <summary>
    /// Добавление нового просмоторщика
    /// </summary>
    /// <param name="filePath"></param>
    private void AddNewLogViewer(string filePath)
    {
        var logViewerVm = App.Host.Services.GetRequiredService<LogViewerViewModel>();
        logViewerVm.Reader = new FileLogReader(filePath);
        logViewerVm.MainVm = this;
        
        _logViewer.Items.Add(new TabItem
        {
            Header = Path.GetFileName(filePath),
            ToolTip = filePath,
            Content = new LogViewerView
            {
                DataContext = logViewerVm
            } 
        });
        _logViewer.SelectedIndex = _logViewer.Items.Count - 1;
    }

    /// <summary>
    /// Инициализация репозитория списка ранее открытых логов
    /// </summary>
    /// <returns></returns>
    /// <exception cref="NotImplementedException"></exception>
    private IRecentLogsRepository InitRecentLogsRepository()
    {
        var appDataPath = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
        var configPath = Path.Combine(appDataPath, "nLogViewer", "recent-logs.json");
        return new RecentLogsFileRepository(configPath, new JsonFileProvider<List<RecentLogEntry>>());
    }
    
    
}