using System;
using NLog;

namespace nLogViewer.Services.LogReader.Repository;

/// <summary>
/// Реализация фабрики репозиториев логов
/// </summary>
internal class LogRepositoryFactory : ILogRepositoryFactory
{
    private static readonly Logger _log = LogManager.GetCurrentClassLogger();

    public ILogRepository CreateFromUri(string sourceUri)
    {
        if (string.IsNullOrWhiteSpace(sourceUri))
            throw new ArgumentException("URI источника не может быть пустым", nameof(sourceUri));

        _log.Debug($"Создание репозитория из URI: {sourceUri}");

        // Проверяем, является ли это URI или просто путь к файлу
        if (!Uri.TryCreate(sourceUri, UriKind.Absolute, out var uri))
        {
            // Предполагаем, что это локальный файл
            _log.Debug($"URI не распознан, интерпретируем как путь к файлу: {sourceUri}");
            return new FileLogRepository(sourceUri);
        }

        // Определяем тип источника по схеме URI
        return uri.Scheme.ToLowerInvariant() switch
        {
            "file" => new FileLogRepository(uri.LocalPath),
            "http" or "https" => throw new NotImplementedException("Сетевые источники пока не поддерживаются"),
            "db" or "database" => throw new NotImplementedException("Источники из БД пока не поддерживаются"),
            _ => throw new NotSupportedException($"Неподдерживаемая схема URI: {uri.Scheme}")
        };
    }

    public ILogRepository Create(LogSourceType sourceType, object configuration)
    {
        _log.Debug($"Создание репозитория типа: {sourceType}");

        return sourceType switch
        {
            LogSourceType.File => CreateFileRepository(configuration),
            LogSourceType.Network => throw new NotImplementedException("Сетевые источники пока не поддерживаются"),
            LogSourceType.Database => throw new NotImplementedException("Источники из БД пока не поддерживаются"),
            LogSourceType.EventLog => throw new NotImplementedException("Windows Event Log пока не поддерживается"),
            LogSourceType.Custom => throw new NotImplementedException("Пользовательские источники пока не поддерживаются"),
            _ => throw new ArgumentOutOfRangeException(nameof(sourceType), sourceType, "Неизвестный тип источника")
        };
    }

    public bool IsSupported(LogSourceType sourceType)
    {
        return sourceType == LogSourceType.File;
    }

    private ILogRepository CreateFileRepository(object configuration)
    {
        if (configuration is string filePath)
        {
            return new FileLogRepository(filePath);
        }

        if (configuration is FileLogConfiguration fileConfig)
        {
            return new FileLogRepository(fileConfig.FilePath);
        }

        throw new ArgumentException(
            "Для файлового источника конфигурация должна быть строкой (путь) или FileLogConfiguration",
            nameof(configuration));
    }
}

/// <summary>
/// Конфигурация для файлового источника логов
/// </summary>
internal class FileLogConfiguration
{
    public string FilePath { get; set; }
    public string Encoding { get; set; } = "UTF-8";
    public bool WatchForChanges { get; set; } = true;
}