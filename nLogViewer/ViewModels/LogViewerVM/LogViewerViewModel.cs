using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Data;
using System.Windows.Input;
using Microsoft.Extensions.DependencyInjection;
using nLogViewer.Infrastructure.Collections;
using nLogViewer.Infrastructure.Commands;
using nLogViewer.Infrastructure.Configuration;
using nLogViewer.Services.Filter;
using nLogViewer.Services.LogReader.FileLogReader;
using nLogViewer.Services.LogViewer;
using FileLogReader = nLogViewer.Services.LogReader.FileLogReader.FileLogReader;
using nLogViewer.Services.Progress;
using nLogViewer.Services.UserDialogService;
using nLogViewer.ViewModels.Base;

namespace nLogViewer.ViewModels.LogViewerVM;

internal class LogViewerViewModel : BaseViewModel
{
    private ILogViewer _viewer;
    private readonly MemoryConfiguration _memoryConfig;
    private readonly IWindowProgressService _windowProgressService;
    private readonly ILogViewerFactory _logViewerFactory;
    private readonly IUserDialogService _userDialogService;
    private ILogEntryFilter _filter;
    private VirtualizingLogCollection _virtualLogEntries;
    private readonly CollectionViewSource _filtredLogEntries = new();
    
    public ICollectionView FiltredLogEntries => _filtredLogEntries.View;
    public LogEntryView SelectedEntry { get; set; }
    public int SelectedIndex { get; set; }

    public LogViewerViewModel(ILogViewerFactory logViewerFactory, MemoryConfiguration memoryConfig, IWindowProgressService windowProgressService, IUserDialogService userDialogService)
    {
        _log.Debug($"Вызов конструктора {GetType().Name}");
         
        _logViewerFactory = logViewerFactory;
        _memoryConfig = memoryConfig;
        _windowProgressService = windowProgressService;
        _userDialogService = userDialogService;
        
        _filter = App.Host.Services.GetService<ILogEntryFilter>();
        _filter.RefreshFilter += OnFilterRefresh;
        
        _filtredLogEntries.Source = new List<LogEntryView>();
        _filtredLogEntries.Filter += LogEntriesFilter;

        #region commands
        AutoscrollCommand = new LambdaCommand(OnEnableAutoscrollCommandExecuted, CanEnableAutoscrollCommandExecute);
        ClearCommand = new LambdaCommand(OnClearCommandExecuted, CanClearCommandExecute);
        PauseCommand = new LambdaCommand(OnPauseCommandExecuted, CanPauseCommandExecute);
        #endregion
    }
    
    public async Task InitializeWithFileAsync(string filePath)
    {
        _log.Debug($"InitializeWithFileAsync вызван с файлом: {filePath}");
        
        if (string.IsNullOrEmpty(filePath))
        {
            _log.Debug("Путь к файлу пустой, выход");
            return;
        }
        
        var fileInfo = new System.IO.FileInfo(filePath);
        const long largeFileThreshold = 10 * 1024 * 1024; // 10 MB
        
        _log.Debug($"Размер файла: {fileInfo.Length} байт, порог: {largeFileThreshold} байт");
        
        // Устанавливаем файл в конфигурацию
        new FileLogReaderConfiguration().FileName = filePath;
        _log.Debug($"Конфигурация файла установлена: {filePath}");
        
        if (fileInfo.Exists && fileInfo.Length > largeFileThreshold)
        {
            _log.Debug("Файл большой, показываем окно прогресса");
            await _windowProgressService.ShowProgressAsync(
                async (progressReporter, cancellationToken) =>
                {
                    _log.Debug("Внутри операции прогресса, создаем LogViewer с прогрессом");
                    _viewer = _logViewerFactory.Create(progressReporter);
                    await InitializeViewer();
                    
                    _log.Debug("Ждем полную загрузку файла через LogViewer");
                    await WaitForLogViewerComplete(progressReporter, cancellationToken);
                },
                $"Загрузка файла {System.IO.Path.GetFileName(filePath)}",
                true);
        }
        else
        {
            _log.Debug("Файл маленький или не существует, создаем LogViewer без прогресса");
            _viewer = _logViewerFactory.Create();
            await InitializeViewer();
        }
    }
    
    
    private async Task InitializeViewer()
    {
        if (_viewer == null) return;
        
        _viewer.EntriesChanged += ViewerEntriesRefresh;
        
        // Инициализируем виртуализированную коллекцию
        if (_memoryConfig?.EnableDataVirtualization == true)
        {
            _virtualLogEntries = new VirtualizingLogCollection(
                (startIndex, count) => 
                {
                    var entries = _viewer.GetEntriesRange(startIndex, count);
                    return entries;
                },
                _viewer.Count,
                _memoryConfig);
            
            await Application.Current.Dispatcher.InvokeAsync(() =>
            {
                _filtredLogEntries.Source = _virtualLogEntries;
            });
        }
        
        _viewer.Start();
    }
    
    private async Task WaitForLogViewerComplete(IProgressReporter progressReporter, CancellationToken cancellationToken)
    {
        _log.Debug("Ждем завершения загрузки файла через LogViewer");
        
        var maxWaitTime = TimeSpan.FromMinutes(10);
        var startTime = DateTime.Now;
        var lastCount = -1;
        var progressPercent = 0;
        
        // Отправляем начальный прогресс
        progressReporter?.Report(0, 100, "Начинаем загрузку файла...");
        
        while (DateTime.Now - startTime < maxWaitTime && !cancellationToken.IsCancellationRequested)
        {
            var currentCount = _viewer?.Count ?? 0;
            var currentState = _viewer?.State ?? LogViewerState.Stop;
            
            _log.Trace($"LogViewer состояние: {currentState}, записей: {currentCount}");
            
            // Проверяем состояние LogViewer для определения завершения
            // ReadNewMsg означает что начальная загрузка завершена и теперь мониторим новые записи
            if (currentState == LogViewerState.ReadNewMsg)
            {
                _log.Debug($"LogViewer завершил чтение всех сообщений, записей: {currentCount}, текущий прогресс: {progressPercent}%");
                
                // Плавно доводим прогресс до 100%
                _log.Debug("Начинаем плавное завершение прогресса");
                while (progressPercent < 100)
                {
                    progressPercent = Math.Min(100, progressPercent + 10);
                    _log.Debug($"Устанавливаем прогресс: {progressPercent}%");
                    progressReporter?.Report(progressPercent, 100, $"Завершение загрузки... {currentCount} записей");
                    await Task.Delay(200, cancellationToken);
                }
                
                _log.Debug("Финальный прогресс 100%");
                progressReporter?.Report(100, 100, $"Загружено {currentCount} записей");
                
                // Дополнительная задержка чтобы пользователь увидел 100%
                await Task.Delay(500, cancellationToken);
                _log.Debug("Завершаем загрузку");
                break;
            }
            
            // Обновляем прогресс основываясь на количестве записей
            if (currentCount != lastCount)
            {
                lastCount = currentCount;
                progressPercent = Math.Min(90, currentCount / 1000); // Растем до 90%, оставляем 10% на финализацию
                progressReporter?.Report(progressPercent, 100, $"Загружено {currentCount} записей...");
            }
            
            await Task.Delay(500, cancellationToken);
        }
        
        if (DateTime.Now - startTime >= maxWaitTime)
        {
            _log.Warn("Превышено максимальное время ожидания загрузки файла");
            progressReporter?.Report(100, 100, "Загрузка завершена (timeout)");
        }
    }
    
    private void LogEntriesFilter(object sender, FilterEventArgs e)
    {
        if(!(e.Item is LogEntryView entry)) return;
        if(_filter.CheckFilter(entry)) return;
        
        e.Accepted = false;
    }

    private void OnFilterRefresh()
    {
        _log.Debug("Обновление фильтра событий");
        FiltredLogEntriesRefresh();
    }
    
    private void FiltredLogEntriesRefresh() => 
        _filtredLogEntries.Dispatcher.BeginInvoke(new Action(() => _filtredLogEntries.View.Refresh()));

    private void ViewerEntriesRefresh() => 
        _filtredLogEntries.Dispatcher.BeginInvoke(new Action(ViewerEntriesChange));

    private void ViewerEntriesChange()
    {
        if (_memoryConfig?.EnableDataVirtualization == true)
        {
            // Для виртуализации просто обновляем общее количество
            if (_virtualLogEntries != null && _filtredLogEntries.Source != null)
            {
                _virtualLogEntries.UpdateTotalCount(_viewer?.Count ?? 0);
                _log.Debug($"Обновлено общее количество записей: {_viewer?.Count ?? 0}");
            }
        }
        else
        {
            // Для обычного режима работаем как раньше
            var logEntries = _filtredLogEntries.Source as List<LogEntryView>;
            if (logEntries != null && _viewer != null)
            {
                int newEntriesCount = _viewer.Count - logEntries.Count;
                _log.Debug($"Получено уведомление о новых событиях ({newEntriesCount})");
                var newEntries = _viewer.GetEntries(newEntriesCount);
                foreach (var entry in newEntries)
                {
                    logEntries.Insert(0, new LogEntryView(entry));
                }
            }
        }
        // Для виртуализированной коллекции не нужно обновлять View,
        // так как она сама уведомляет об изменениях
        if (_memoryConfig?.EnableDataVirtualization != true)
        {
            FiltredLogEntriesRefresh();
        }
    }
    
    #region Commands
    
    #region Включение автопрокрутки
    public ICommand AutoscrollCommand { get; }
    private void OnEnableAutoscrollCommandExecuted(object p) { }
    private bool CanEnableAutoscrollCommandExecute(object p) => true;
    #endregion
    
    #region Очистка лога
    public ICommand ClearCommand { get; }
    private void OnClearCommandExecuted(object p)
    {
        _log.Debug($"Очистка всех событий");
        if (_viewer != null)
        {
            _viewer.Clear();
        }
        
        if (_memoryConfig?.EnableDataVirtualization == true)
        {
            _virtualLogEntries?.InvalidateCache();
        }
        else
        {
            var logEntries = _filtredLogEntries.Source as List<LogEntryView>;
            logEntries?.Clear();
        }
        
        _filtredLogEntries.View.Refresh();
    }
    private bool CanClearCommandExecute(object p) => _viewer?.Count > 0;
    #endregion

    #region Пауза отображения лога
    public ICommand PauseCommand { get; }
    private void OnPauseCommandExecuted(object p)
    {
        if (_viewer != null)
        {
            if (EnabledPause)
            {
                _log.Debug($"Команда: Отключить паузу");
                _viewer.Start();
            }
            else
            {
                _log.Debug($"Команда: Включить паузу");
                _viewer.Pause();
            }
        }
    }
    private bool CanPauseCommandExecute(object p) => _viewer?.Count > 0;
    #endregion

    #endregion

    #region Toolbar
    
    private bool _autoscroll;
    /// <summary>
    /// Включение автопрокрутки
    /// </summary>
    public bool EnabledAutoscroll
    {
        get => _autoscroll;
        set => Set(ref _autoscroll, value);
    }

    /// <summary>
    /// Включение паузы
    /// </summary>
    public bool EnabledPause => _viewer?.State == LogViewerState.Pause;

    // {
    //     get => _pause;
    //     set => Set(ref _pause, value);
    // }

    #endregion

    public void Dispose()
    {
        _log.Debug($"Освобождение ресурсов LogViewerViewModel");
        if (_viewer != null)
        {
            _viewer.EntriesChanged -= ViewerEntriesRefresh;
            _viewer.Stop();
            _viewer.Dispose();
        }
        if (_filter != null)
        {
            _filter.RefreshFilter -= OnFilterRefresh;
        }
        _filtredLogEntries.Filter -= LogEntriesFilter;
    }
}