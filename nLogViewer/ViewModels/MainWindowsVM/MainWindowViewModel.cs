using System;
using System.Collections.Generic;
using System.IO;
using System.Diagnostics;
using System.Linq;
using System.Windows;
using System.Windows.Input;
using System.Collections.ObjectModel;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Win32;
using nLogViewer.Infrastructure.Commands;
using nLogViewer.Infrastructure.Configuration;
using nLogViewer.Infrastructure.Helpers.FileProvider;
using nLogViewer.Model;
using nLogViewer.Model.AppSettings.AppConfig;
using nLogViewer.Model.AppSettings.RecentLogs;
using nLogViewer.Services.Filter;
using nLogViewer.Services.LogReader.FileLogReader;
using nLogViewer.Services.LogViewer;
using nLogViewer.Services.Progress;
using nLogViewer.Services.UserDialogService;
using nLogViewer.ViewModels.Base;
using nLogViewer.Views;
using Ookii.Dialogs.Wpf;

namespace nLogViewer.ViewModels;

public class LogTabItem
{
    public string Header { get; set; }
    public string ToolTip { get; set; }
    public string FilePath { get; set; }
    public LogViewerView Content { get; set; }
}

internal class MainWindowViewModel : BaseViewModel
{
    private IRecentLogsRepository _recentLogs;
    private IAppConfig _appConfig;
    private readonly ILogEntryFilter _filter;
    private readonly IUserDialogService _userDialogService;
    private readonly IWindowProgressService _windowProgressService;
    
    private ObservableCollection<LogTabItem> _logTabs;
    private LogTabItem _selectedLogTab;
    
    public MainWindowViewModel(IUserDialogService userDialogService, IWindowProgressService windowProgressService)
    {
        _log.Debug($"Вызов конструктора {this.GetType().Name} по умолчанию");
        _title = $"{AppConst.Get().AppName} {ProjectVersion.Get()}";
        _appConfig = AppConfig.GetConfigFromDefaultPath();
        _filter = App.Host.Services.GetService<ILogEntryFilter>();
        _userDialogService = userDialogService;
        _windowProgressService = windowProgressService;
        
        _logTabs = new ObservableCollection<LogTabItem>();

        #region commands
        AddFile = new LambdaCommand(OnAddFileExecuted, CanAddFileExecute);
        AddFolder = new LambdaCommand(OnAddFolderExecuted, CanAddFolderExecute);
        DeleteLog = new LambdaCommand(OnDeleteLogExecuted, CanDeleteLogExecute);
        About = new LambdaCommand(OnAboutExecuted, CanAboutExecute);
        Exit = new LambdaCommand(OnExitExecuted, CanExitExecute);
        Settings = new LambdaCommand(OnSettingsExecuted, CanSettingsExecute);
        
        // Context menu commands
        OpenInNotepadCommand = new LambdaCommand(OnOpenInNotepadExecuted, CanOpenInNotepadExecute);
        OpenInNotepadPPCommand = new LambdaCommand(OnOpenInNotepadPPExecuted, CanOpenInNotepadPPExecute);
        OpenInExplorerCommand = new LambdaCommand(OnOpenInExplorerExecuted, CanOpenInExplorerExecute);
        
        // Application menu commands
        LoadSession = new LambdaCommand(OnLoadSessionExecuted, CanLoadSessionExecute);
        #endregion
    }

    #region Commands
    
    #region Add file
    public ICommand AddFile { get; }
    private void OnAddFileExecuted(object p)
    {
        _log.Debug("Команда добавить файл лога в просмоторщик");
        var ofd = new OpenFileDialog
        {
            DefaultExt = "log",
            Filter = "log files (*.log)|*.log|All files (*.*)|*.*",
            Title = "Выберите лог-файл"
        };
        if(ofd.ShowDialog() == false)
        {
            _log.Debug("Диалог выбора файла завершился отменой");
            return;
        }
        AddNewLogViewer(ofd.FileName);
        _recentLogs.Add(ofd.FileName);
        OnPropertyChanged(nameof(RecentFiles));
    }
    
    private bool CanAddFileExecute(object p) => true;
    #endregion
    
    #region Add folder
    public ICommand AddFolder { get; }
    private void OnAddFolderExecuted(object p)
    {
        _log.Debug("Команда добавить директорию лога в просмоторщик");
        var ofd = new VistaFolderBrowserDialog
        {
            Description = "Выберите директорию логов",
            UseDescriptionForTitle = true,
            Multiselect = false
        };
        var dialogResult = ofd.ShowDialog();
        if (!dialogResult ?? false)
        {
            _log.Debug("Диалог выбора директории завершился отменой");
            return;
        }
        var selectedFolder = ofd.SelectedPath;
        if (!Directory.Exists(selectedFolder))
        {
            _log.Error($"Директория {selectedFolder} несуществует");
            return;
        }

        var filePath = FindLastFileInDirectory(selectedFolder);
        if (string.IsNullOrEmpty(filePath))
            return;
        
        AddNewLogViewer(filePath);
        _recentLogs.Add(selectedFolder, true);
        OnPropertyChanged(nameof(RecentFiles));
    }
    private bool CanAddFolderExecute(object p) => true;
    #endregion
    
    #region Delete log
    public ICommand DeleteLog { get; }
    private void OnDeleteLogExecuted(object p)
    {
        _log.Debug("Команда удалить текущий лог из просмоторщика");
        if (_selectedLogTab?.Content is LogViewerView logViewerView)
        {
            logViewerView.Dispose();
        }
        
        var selectedIndex = LogTabs.IndexOf(_selectedLogTab);
        if (selectedIndex >= 0 && selectedIndex < _recentLogs.Count())
        {
            _recentLogs.Remove(_recentLogs.ElementAt(selectedIndex));
        }
        
        LogTabs.Remove(_selectedLogTab);
    }
    private bool CanDeleteLogExecute(object p) => LogTabs.Count > 0 && _selectedLogTab != null;
    #endregion

    #region About
    public ICommand About { get; }
    private void OnAboutExecuted(object p)
    {
        _log.Debug("Открытие окна О приложении");
        var version = ProjectVersion.Get();
        var aboutMessage = $"{AppConst.Get().AppName} {version}\n\n" +
                          $"{AppConst.Get().AppDesciption}\n\n";
        MessageBox.Show(aboutMessage, "О приложении", MessageBoxButton.OK, MessageBoxImage.Information);
    }
    private bool CanAboutExecute(object p) => true;
    #endregion
    
    #region Exit
    public ICommand Exit { get; }
    private void OnExitExecuted(object p) => Application.Current.Shutdown();
    private bool CanExitExecute(object p) => true;
    #endregion
    
    #region Settings
    public ICommand Settings { get; }
    private void OnSettingsExecuted(object p)
    {
        _log.Debug("Открытие окна настроек");
        
        var settingsViewModel = new ViewModels.SettingsVM.SettingsViewModel(_appConfig);
        var settingsWindow = new Views.SettingsWindow(settingsViewModel)
        {
            Owner = Application.Current.MainWindow
        };
        
        var result = settingsWindow.ShowDialog();
        if (result == true)
        {
            _log.Info("Настройки сохранены, требуется перезапуск для применения изменений");
            MessageBox.Show(
                "Настройки сохранены!\nДля применения изменений виртуализации перезапустите приложение или перезагрузите лог-файл.",
                "Настройки",
                MessageBoxButton.OK,
                MessageBoxImage.Information
            );
        }
    }
    private bool CanSettingsExecute(object p) => true;
    #endregion
    
    #region Context Menu Commands
    public ICommand OpenInNotepadCommand { get; }
    private void OnOpenInNotepadExecuted(object parameter)
    {
        var filePath = parameter as string ?? _selectedLogTab?.FilePath;
        if (string.IsNullOrEmpty(filePath)) return;
        
        try
        {
            Process.Start(new ProcessStartInfo("notepad.exe", $"\"{filePath}\"") { UseShellExecute = true });
        }
        catch (Exception ex)
        {
            _log.Error($"Не удалось открыть файл в Блокноте: {ex.Message}");
        }
    }
    private bool CanOpenInNotepadExecute(object parameter) => true;
    
    public ICommand OpenInNotepadPPCommand { get; }
    private void OnOpenInNotepadPPExecuted(object parameter)
    {
        var filePath = parameter as string ?? _selectedLogTab?.FilePath;
        if (string.IsNullOrEmpty(filePath)) return;
        
        var notepadPlusPlusPath = GetNotepadPlusPlusPath();
        if (string.IsNullOrEmpty(notepadPlusPlusPath)) return;
        
        try
        {
            Process.Start(new ProcessStartInfo(notepadPlusPlusPath, $"\"{filePath}\"") { UseShellExecute = true });
        }
        catch (Exception ex)
        {
            _log.Error($"Не удалось открыть файл в Notepad++: {ex.Message}");
        }
    }
    private bool CanOpenInNotepadPPExecute(object parameter) => !string.IsNullOrEmpty(GetNotepadPlusPlusPath());
    
    public ICommand OpenInExplorerCommand { get; }
    private void OnOpenInExplorerExecuted(object parameter)
    {
        var filePath = parameter as string ?? _selectedLogTab?.FilePath;
        if (string.IsNullOrEmpty(filePath)) return;
        
        try
        {
            var args = $"/select,\"{filePath}\"";
            Process.Start(new ProcessStartInfo("explorer.exe", args) { UseShellExecute = true });
        }
        catch (Exception ex)
        {
            _log.Error($"Не удалось открыть проводник: {ex.Message}");
        }
    }
    private bool CanOpenInExplorerExecute(object parameter) => true;
    
    public ICommand LoadSession { get; }
    private void OnLoadSessionExecuted(object parameter)
    {
        var filePath = parameter as string;
        if (string.IsNullOrEmpty(filePath)) return;
        
        var recentEntry = _recentLogs.FirstOrDefault(x => x.Path == filePath);
        if (recentEntry != null)
        {
            var path = recentEntry.IsFolder ? FindLastFileInDirectory(recentEntry.Path) : recentEntry.Path;
            if (!string.IsNullOrEmpty(path) && File.Exists(path))
            {
                AddNewLogViewer(path);
            }
        }
    }
    private bool CanLoadSessionExecute(object parameter) => true;
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
    
    #region Log Tabs
    /// <summary>
    /// Коллекция вкладок с логами
    /// </summary>
    public ObservableCollection<LogTabItem> LogTabs
    {
        get => _logTabs;
        set => Set(ref _logTabs, value);
    }
    
    /// <summary>
    /// Выбранная вкладка
    /// </summary>
    public LogTabItem SelectedLogTab
    {
        get => _selectedLogTab;
        set => Set(ref _selectedLogTab, value);
    }
    
    /// <summary>
    /// Список недавних файлов для меню приложения
    /// </summary>
    public IEnumerable<string> RecentFiles => _recentLogs?.Select(x => x.Path) ?? Enumerable.Empty<string>();
    
    #endregion
    
    #region Фильтр событий
    /// <summary>
    /// Trace
    /// </summary>
    public bool EnableTraceEvent
    {
        get => _filter.EnableTraceEvent;
        set => SetProxied(() => _filter.EnableTraceEvent, v => _filter.EnableTraceEvent = v, value);
    } 
    /// <summary>
    /// Debug
    /// </summary>
    public bool EnableDebugEvent
    {
        get => _filter.EnableDebugEvent;
        set => SetProxied(() => _filter.EnableDebugEvent, v => _filter.EnableDebugEvent = v, value);
    }
    /// <summary>
    /// Info
    /// </summary>
    public bool EnableInfoEvent
    {
        get => _filter.EnableInfoEvent;
        set => SetProxied(() => _filter.EnableInfoEvent, v => _filter.EnableInfoEvent = v, value);
    }
    /// <summary>
    /// Warning
    /// </summary>
    public bool EnableWarnEvent
    {
        get => _filter.EnableWarnEvent;
        set => SetProxied(() => _filter.EnableWarnEvent, v => _filter.EnableWarnEvent = v, value);
    }
    /// <summary>
    /// Error
    /// </summary>
    public bool EnableErrorEvent
    {
        get => _filter.EnableErrorEvent;
        set => SetProxied(() => _filter.EnableErrorEvent, v => _filter.EnableErrorEvent = v, value);
    }
    /// <summary>
    /// Fatal
    /// </summary>
    public bool EnableFatalEvent
    {
        get => _filter.EnableFatalEvent;
        set => SetProxied(() => _filter.EnableFatalEvent, v => _filter.EnableFatalEvent = v, value);
    }
    #endregion

    #region Поиск по тексту

    /// <summary>
    /// Включение поиска
    /// </summary>
    public bool EnableSearchEvent
    {
        get => _filter.EnableTextSearch;
        set => SetProxied(() => _filter.EnableTextSearch, v => _filter.EnableTextSearch = v, value);
    } 
    
    /// <summary>
    /// Текст поиска
    /// </summary>
    public string SearchText
    {
        get => _filter.TextSearch;
        set => SetProxied(() => _filter.TextSearch, v => _filter.TextSearch = v, value);
    }
    
    #endregion

    /// <summary>
    /// Инициализация просмотощика лога
    /// </summary>
    public void InitLogViewerControl()
    {
        _log.Trace("Промотрщик лога инициализирован");
        
        _recentLogs = InitRecentLogsRepository();
        if (_recentLogs.Any())
            ViewRecentLogs();
        OnPropertyChanged(nameof(RecentFiles));
    }
    
    /// <summary>
    /// Действия выполняемые при закрытии основной формы
    /// </summary>
    public void OnExit()
    {
        _recentLogs?.Save();

        #region Cохранение настроек фильтра
        if (_filter != null)
        {
            _appConfig.FilterConfig.EnableTraceEvent = _filter.EnableTraceEvent;
            _appConfig.FilterConfig.EnableDebugEvent = _filter.EnableDebugEvent;
            _appConfig.FilterConfig.EnableInfoEvent = _filter.EnableInfoEvent;
            _appConfig.FilterConfig.EnableWarnEvent = _filter.EnableWarnEvent;
            _appConfig.FilterConfig.EnableErrorEvent = _filter.EnableErrorEvent;
            _appConfig.FilterConfig.EnableFatalEvent = _filter.EnableFatalEvent;

            _appConfig.FilterConfig.EnableTextSearch = _filter.EnableTextSearch;
            _appConfig.FilterConfig.TextSearch = _filter.TextSearch;
        }
        #endregion
    }

    /// <summary>
    /// Загрузка ранее сохраненных логов
    /// </summary>
    /// <exception cref="NotImplementedException"></exception>
    private void ViewRecentLogs()
    {
        foreach (var entry in _recentLogs.ToList())
        {
            _log.Debug($"Открываем ранее {(entry.IsFolder ? "открытую директорию" : "открытый лог-файл")} \"{entry.Path}\"");
            var path = entry.IsFolder ? FindLastFileInDirectory(entry.Path) : entry.Path;

            if (string.IsNullOrEmpty(path) || !File.Exists(path))
            {
                _log.Warn($"Файл \"{path}\" не существует. Удаляем данную запись из списка ранее открытых логов");
                _recentLogs.Remove(entry);
            }
            AddNewLogViewer(path);
        }
    }

    private string FindLastFileInDirectory(string dirPath)
    {
        var directoryInfo = new DirectoryInfo(dirPath);
        if (!directoryInfo.Exists)
        {
            _log.Error($"Директория {dirPath} отсутствует");
            return string.Empty;
        }
        var filesInfo = directoryInfo.GetFiles();
        if (filesInfo.Length == 0 && filesInfo.All(x => x.Extension != ".log"))
        {
            _log.Error($"В директории {dirPath} отсутствуют лог-файлы");
            return string.Empty;
        }
        var lastFile = filesInfo.Where(x => x.Extension == ".log").OrderBy(x => x.LastWriteTime).Last();
        _log.Debug($"Последний измененный файл в заданной директории {lastFile.FullName}");
        return lastFile.FullName;
    }
    
    /// <summary>
    /// Добавление нового просмоторщика
    /// </summary>
    /// <param name="filePath"></param>
    private async void AddNewLogViewer(string filePath)
    {
        _log.Debug($"AddNewLogViewer вызван с файлом: {filePath}");
        
        // Создаем ViewModel вручную с нужными зависимостями
        _log.Debug($"Получаем сервисы из DI");
        var logViewerFactory = App.Host.Services.GetRequiredService<ILogViewerFactory>();
        var memoryConfig = App.Host.Services.GetRequiredService<MemoryConfiguration>();
        var windowProgressService = App.Host.Services.GetRequiredService<IWindowProgressService>();
        var userDialogService = App.Host.Services.GetRequiredService<IUserDialogService>();
        
        _log.Debug($"Создаем LogViewerViewModel");
        var viewModel = new LogViewerVM.LogViewerViewModel(logViewerFactory, memoryConfig, windowProgressService, userDialogService);
        
        _log.Debug($"Создаем LogViewerView и устанавливаем DataContext");
        var logViewerView = new LogViewerView()
        {
            DataContext = viewModel
        };
        
        _log.Debug($"Создаем LogTabItem и добавляем в коллекцию");
        var logTabItem = new LogTabItem
        {
            Header = Path.GetFileName(filePath),
            ToolTip = filePath,
            FilePath = filePath,
            Content = logViewerView
        };
        
        LogTabs.Add(logTabItem);
        SelectedLogTab = logTabItem;
        
        // Инициализируем LogViewerViewModel с файлом
        _log.Debug($"Вызываем InitializeWithFileAsync для файла: {filePath}");
        await viewModel.InitializeWithFileAsync(filePath);
        _log.Debug($"InitializeWithFileAsync завершен");
    }


    private static string GetNotepadPlusPlusPath()
    {
        try
        {
            var candidates = new[]
            {
                Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "Notepad++", "notepad++.exe"),
                Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86), "Notepad++", "notepad++.exe")
            };
            foreach (var candidate in candidates)
                if (File.Exists(candidate))
                    return candidate;

            var pathEnv = Environment.GetEnvironmentVariable("PATH") ?? string.Empty;
            foreach (var dir in pathEnv.Split(Path.PathSeparator))
            {
                if (string.IsNullOrWhiteSpace(dir)) continue;
                string possible = string.Empty;
                try { possible = Path.Combine(dir, "notepad++.exe"); } catch { }
                if (!string.IsNullOrEmpty(possible) && File.Exists(possible))
                    return possible;
            }
        }
        catch { }
        return string.Empty;
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