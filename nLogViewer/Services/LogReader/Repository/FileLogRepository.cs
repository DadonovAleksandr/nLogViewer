using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using NLog;
using nLogViewer.Services.Progress;

namespace nLogViewer.Services.LogReader.Repository;

/// <summary>
/// Репозиторий для работы с файловыми источниками логов
/// </summary>
internal class FileLogRepository : ILogRepository
{
    private static readonly Logger _log = LogManager.GetCurrentClassLogger();
    private readonly string _filePath;
    private long _currentPosition;
    private bool _disposed;

    public string SourceId => _filePath;
    public string SourceDescription => $"File: {Path.GetFileName(_filePath)}";
    public bool SupportsClear => true;
    public bool SupportsIncrementalRead => true;

    public FileLogRepository(string filePath)
    {
        _log.Debug($"Создание FileLogRepository для файла: {filePath}");
        
        if (string.IsNullOrEmpty(filePath))
            throw new ArgumentException("Путь к файлу не может быть пустым", nameof(filePath));
            
        _filePath = filePath;
        _currentPosition = 0;
    }

    public async Task<bool> IsAvailableAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            return await Task.Run(() => File.Exists(_filePath), cancellationToken);
        }
        catch (Exception ex)
        {
            _log.Error(ex, $"Ошибка проверки доступности файла {_filePath}");
            return false;
        }
    }

    public async IAsyncEnumerable<string> ReadAllLinesAsync([EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        _log.Trace($"Чтение всех строк из файла {_filePath}");
        
        // Сбрасываем позицию для чтения с начала
        _currentPosition = 0;
        
        await foreach (var line in ReadLinesFromPositionAsync(_currentPosition, null, cancellationToken))
        {
            yield return line;
        }
    }

    public async IAsyncEnumerable<string> ReadAllLinesAsync(IProgressReporter progressReporter, [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        _log.Trace($"Чтение всех строк из файла {_filePath} с отчетом о прогрессе");
        
        // Сбрасываем позицию для чтения с начала
        _currentPosition = 0;
        
        await foreach (var line in ReadLinesFromPositionAsync(_currentPosition, progressReporter, cancellationToken))
        {
            yield return line;
        }
    }

    public async IAsyncEnumerable<string> ReadNewLinesAsync([EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        _log.Trace($"Чтение новых строк из файла {_filePath} с позиции {_currentPosition}");
        
        await foreach (var line in ReadLinesFromPositionAsync(_currentPosition, null, cancellationToken))
        {
            yield return line;
        }
    }

    public async IAsyncEnumerable<string> ReadNewLinesAsync(IProgressReporter progressReporter, [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        _log.Trace($"Чтение новых строк из файла {_filePath} с позиции {_currentPosition} с отчетом о прогрессе");
        
        await foreach (var line in ReadLinesFromPositionAsync(_currentPosition, progressReporter, cancellationToken))
        {
            yield return line;
        }
    }

    public async Task<bool> ClearAsync(CancellationToken cancellationToken = default)
    {
        _log.Debug($"Очистка файла {_filePath}");
        
        try
        {
            await using var fs = new FileStream(_filePath, FileMode.Create, FileAccess.Write, FileShare.Read, 4096, true);
            await fs.FlushAsync(cancellationToken);
            
            _currentPosition = 0;
            _log.Info($"Файл {_filePath} успешно очищен");
            return true;
        }
        catch (Exception ex)
        {
            _log.Error(ex, $"Ошибка при очистке файла {_filePath}");
            return false;
        }
    }

    public void ResetPosition()
    {
        _log.Debug($"Сброс позиции чтения для файла {_filePath}");
        _currentPosition = 0;
    }

    private async IAsyncEnumerable<string> ReadLinesFromPositionAsync(long startPosition, IProgressReporter progressReporter, [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        if (!File.Exists(_filePath))
        {
            _log.Warn($"Файл {_filePath} не существует");
            yield break;
        }

        using var fs = new FileStream(_filePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite, 4096, true);
        fs.Seek(startPosition, SeekOrigin.Begin);
        
        var fileSize = fs.Length;
        var linesRead = 0;
        
        using var reader = new StreamReader(fs);
        string line;
        
        while ((line = await reader.ReadLineAsync()) != null)
        {
            cancellationToken.ThrowIfCancellationRequested();
            
            linesRead++;
            
            // Отчет о прогрессе каждые 100 строк
            if (progressReporter != null && linesRead % 100 == 0)
            {
                var currentPosition = fs.Position;
                var progressPercentage = fileSize > 0 ? (double)currentPosition / fileSize * 100 : 0;
                progressReporter.ReportPercentage((int)progressPercentage, $"Обработано {linesRead} строк");
            }
            
            yield return line;
        }
        
        // Финальный отчет о прогрессе
        if (progressReporter != null)
        {
            progressReporter.ReportPercentage(100, $"Обработано {linesRead} строк");
        }
        
        // Обновляем позицию для следующего инкрементального чтения
        _currentPosition = fs.Position;
        _log.Trace($"Обновлена позиция чтения: {_currentPosition}");
    }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    protected virtual void Dispose(bool disposing)
    {
        if (_disposed)
            return;

        if (disposing)
        {
            _log.Debug($"Освобождение ресурсов FileLogRepository для файла {_filePath}");
        }

        _disposed = true;
    }
}