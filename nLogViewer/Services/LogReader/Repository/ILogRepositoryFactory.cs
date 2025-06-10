using System;

namespace nLogViewer.Services.LogReader.Repository;

/// <summary>
/// Фабрика для создания репозиториев логов
/// </summary>
internal interface ILogRepositoryFactory
{
    /// <summary>
    /// Создает репозиторий на основе URI источника
    /// </summary>
    /// <param name="sourceUri">URI источника (file://, http://, db:// и т.д.)</param>
    /// <returns>Экземпляр репозитория</returns>
    ILogRepository CreateFromUri(string sourceUri);
    
    /// <summary>
    /// Создает репозиторий на основе типа источника
    /// </summary>
    /// <param name="sourceType">Тип источника</param>
    /// <param name="configuration">Конфигурация источника</param>
    /// <returns>Экземпляр репозитория</returns>
    ILogRepository Create(LogSourceType sourceType, object configuration);
    
    /// <summary>
    /// Проверяет, поддерживается ли указанный тип источника
    /// </summary>
    /// <param name="sourceType">Тип источника</param>
    /// <returns>true если тип поддерживается</returns>
    bool IsSupported(LogSourceType sourceType);
}

/// <summary>
/// Типы источников логов
/// </summary>
internal enum LogSourceType
{
    /// <summary>
    /// Файловый источник
    /// </summary>
    File,
    
    /// <summary>
    /// Сетевой источник (HTTP/HTTPS)
    /// </summary>
    Network,
    
    /// <summary>
    /// База данных
    /// </summary>
    Database,
    
    /// <summary>
    /// Windows Event Log
    /// </summary>
    EventLog,
    
    /// <summary>
    /// Пользовательский источник
    /// </summary>
    Custom
}