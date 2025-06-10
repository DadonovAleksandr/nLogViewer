using System;
using System.Threading;
using System.Windows.Input;
using nLogViewer.Infrastructure.Commands;
using nLogViewer.Services.Progress;
using nLogViewer.ViewModels.Base;

namespace nLogViewer.ViewModels.ProgressVM;

/// <summary>
/// ViewModel для окна прогресса
/// </summary>
public class ProgressViewModel : BaseViewModel
{
    private readonly CancellationTokenSource _cancellationTokenSource;
    private int _progressValue;
    private string _statusMessage;
    private string _estimatedTimeRemaining;
    private bool _isIndeterminate;
    private bool _canCancel;
    
    public int ProgressValue
    {
        get => _progressValue;
        set => Set(ref _progressValue, value);
    }
    
    public string StatusMessage
    {
        get => _statusMessage;
        set => Set(ref _statusMessage, value);
    }
    
    public string EstimatedTimeRemaining
    {
        get => _estimatedTimeRemaining;
        set => Set(ref _estimatedTimeRemaining, value);
    }
    
    public bool IsIndeterminate
    {
        get => _isIndeterminate;
        set => Set(ref _isIndeterminate, value);
    }
    
    public bool CanCancel
    {
        get => _canCancel;
        set => Set(ref _canCancel, value);
    }
    
    public ICommand CancelCommand { get; }
    
    public ProgressViewModel(CancellationTokenSource cancellationTokenSource = null)
    {
        _cancellationTokenSource = cancellationTokenSource;
        CanCancel = _cancellationTokenSource != null;
        
        CancelCommand = new LambdaCommand(OnCancelExecuted, CanCancelExecute);
        
        // По умолчанию индикатор неопределённый
        IsIndeterminate = true;
        StatusMessage = "Загрузка...";
    }
    
    public void UpdateProgress(ProgressEventArgs args)
    {
        ProgressValue = args.Percentage;
        StatusMessage = args.Message ?? $"Обработано {args.Current} из {args.Total}";
        
        // Переключаем режим индикатора
        if (args.Total > 0)
        {
            IsIndeterminate = false;
        }
        
        // Обновляем оставшееся время
        if (args.EstimatedTimeRemaining.HasValue)
        {
            var time = args.EstimatedTimeRemaining.Value;
            if (time.TotalHours >= 1)
            {
                EstimatedTimeRemaining = $"Осталось: {time:hh\\:mm\\:ss}";
            }
            else if (time.TotalMinutes >= 1)
            {
                EstimatedTimeRemaining = $"Осталось: {time:mm\\:ss}";
            }
            else
            {
                EstimatedTimeRemaining = $"Осталось: {time.Seconds} сек";
            }
        }
        else
        {
            EstimatedTimeRemaining = null;
        }
    }
    
    private void OnCancelExecuted(object parameter)
    {
        _log.Debug("Отмена операции по запросу пользователя");
        _cancellationTokenSource?.Cancel();
        StatusMessage = "Отмена операции...";
        CanCancel = false;
    }
    
    private bool CanCancelExecute(object parameter) => 
        CanCancel && _cancellationTokenSource != null && !_cancellationTokenSource.IsCancellationRequested;
}