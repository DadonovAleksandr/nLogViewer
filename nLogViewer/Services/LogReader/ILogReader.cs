using System.Collections.Generic;
using nLogViewer.Model;

namespace nLogViewer.Services.LogReader;

internal interface ILogReader
{
    /// <summary>
    /// Получить все записи
    /// </summary>
    /// <returns></returns>
    IEnumerable<ILogEntry> GetAll();
    
    /// <summary>
    /// Получить новые записи
    /// </summary>
    /// <returns></returns>
    IEnumerable<ILogEntry> GetNew();

    /// <summary>
    /// Очистка текущего лога
    /// </summary>
    /// <returns></returns>
    bool Clear();
}