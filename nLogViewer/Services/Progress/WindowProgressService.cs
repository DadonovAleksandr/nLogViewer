using System;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using nLogViewer.ViewModels.ProgressVM;
using nLogViewer.Views;

namespace nLogViewer.Services.Progress;

/// <summary>
/// Реализация сервиса для отображения окна прогресса
/// </summary>
internal class WindowProgressService : IWindowProgressService
{
    public async Task<T> ShowProgressAsync<T>(
        Func<IProgressReporter, CancellationToken, Task<T>> operation,
        string title = "Загрузка...",
        bool canCancel = true)
    {
        // Проверяем, что мы в UI потоке
        if (Application.Current.Dispatcher.Thread != Thread.CurrentThread)
        {
            // Если нет, вызываем через Dispatcher
            var task = Application.Current.Dispatcher.InvokeAsync(() => 
                ShowProgressAsync(operation, title, canCancel));
            return await await task;
        }

        var cancellationTokenSource = canCancel ? new CancellationTokenSource() : null;
        var progressReporter = new ProgressReporter();
        var progressViewModel = new ProgressViewModel(cancellationTokenSource);
        
        var progressWindow = new ProgressWindow(progressViewModel)
        {
            Title = title,
            Owner = Application.Current.MainWindow
        };
        
        // Подписываемся на обновления прогресса
        progressReporter.ProgressChanged += (sender, args) =>
        {
            Application.Current.Dispatcher.BeginInvoke(() =>
            {
                progressViewModel.UpdateProgress(args);
            });
        };
        
        T result = default;
        Exception exception = null;
        var operationCompleted = false;
        var windowClosed = false;
        
        // Запускаем операцию в фоновом потоке
        var operationTask = Task.Run(async () =>
        {
            try
            {
                result = await operation(progressReporter, cancellationTokenSource?.Token ?? CancellationToken.None);
            }
            catch (OperationCanceledException)
            {
                // Операция отменена пользователем
            }
            catch (Exception ex)
            {
                exception = ex;
            }
            finally
            {
                operationCompleted = true;
                // Даем время для завершения анимации прогресса перед закрытием окна
                _ = Task.Run(async () =>
                {
                    await Task.Delay(1000); // Даем секунду на завершение анимации
                    Application.Current.Dispatcher.BeginInvoke(() =>
                    {
                        if (!windowClosed && progressWindow.IsLoaded)
                        {
                            windowClosed = true;
                            progressWindow.DialogResult = true;
                        }
                    });
                });
            }
        });
        
        // Показываем окно прогресса модально (блокирует поток)
        var dialogResult = progressWindow.ShowDialog();
        
        // Ждём завершения операции
        await operationTask;
        
        // Освобождаем ресурсы
        cancellationTokenSource?.Dispose();
        
        // Если была ошибка, пробрасываем её
        if (exception != null)
            throw exception;
            
        return result;
    }
    
    public async Task ShowProgressAsync(
        Func<IProgressReporter, CancellationToken, Task> operation,
        string title = "Загрузка...",
        bool canCancel = true)
    {
        await ShowProgressAsync<object>(async (reporter, token) =>
        {
            await operation(reporter, token);
            return null;
        }, title, canCancel);
    }
}