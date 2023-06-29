using System;

namespace nLogViewer.Model;

public interface ILogEntry
{
    /// <summary>
    /// Дата и время события
    /// </summary>
    public DateTime EventDateTime { get; }

    /// <summary>
    /// Важность события
    /// </summary>
    public LogEntryType EntryType { get; }
    
    /// <summary>
    /// Сообщение (описание события)
    /// </summary>
    public string Message { get; }
    
    /// <summary>
    /// Источник события
    /// </summary>
    public string Source { get; }
    
    /// <summary>
    /// ИД процесса
    /// </summary>
    public int ProcessId { get; }
    /// <summary>
    /// ИД потока
    /// </summary>
    public int ThreadId { get; }
}