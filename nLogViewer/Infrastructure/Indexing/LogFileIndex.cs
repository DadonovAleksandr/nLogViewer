using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using NLog;

namespace nLogViewer.Infrastructure.Indexing;

/// <summary>
/// Индекс для быстрого доступа к записям в лог-файле
/// Позволяет получить позицию в файле для произвольной строки за O(1) время
/// </summary>
internal class LogFileIndex
{
    private static readonly Logger _logger = LogManager.GetCurrentClassLogger();

    // Индекс: каждая N-я запись → позиция в файле
    private readonly Dictionary<int, long> _index = new();
    private const int IndexInterval = 1000; // Индексируем каждую 1000-ю строку

    private int _totalLines;
    private long _fileSize;
    private string _filePath;

    /// <summary>
    /// Общее количество проиндексированных строк
    /// </summary>
    public int TotalLines => _totalLines;

    /// <summary>
    /// Размер проиндексированного файла
    /// </summary>
    public long FileSize => _fileSize;

    /// <summary>
    /// Путь к файлу
    /// </summary>
    public string FilePath => _filePath;

    /// <summary>
    /// Интервал индексирования (каждая N-я строка)
    /// </summary>
    public int Interval => IndexInterval;

    /// <summary>
    /// Количество точек индексации
    /// </summary>
    public int IndexPointsCount => _index.Count;

    /// <summary>
    /// Создает индекс для указанного лог-файла
    /// </summary>
    /// <param name="filePath">Путь к лог-файлу</param>
    /// <param name="cancellationToken">Токен отмены</param>
    public async Task BuildIndexAsync(string filePath, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrEmpty(filePath))
            throw new ArgumentException("Путь к файлу не может быть пустым", nameof(filePath));

        if (!File.Exists(filePath))
            throw new FileNotFoundException($"Файл не найден: {filePath}");

        _logger.Info($"Начинается построение индекса для файла: {filePath}");
        _filePath = filePath;
        _index.Clear();

        var startTime = DateTime.UtcNow;

        using var fs = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
        using var sr = new StreamReader(fs, Encoding.UTF8);

        int lineNumber = 0;
        long position = 0;

        while (await sr.ReadLineAsync(cancellationToken).ConfigureAwait(false) is not null)
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (lineNumber % IndexInterval == 0)
            {
                // Сохраняем позицию каждой 1000-й строки
                _index[lineNumber] = position;
            }

            lineNumber++;
            position = fs.Position;

            // Периодически логируем прогресс
            if (lineNumber % 100000 == 0)
            {
                _logger.Debug($"Проиндексировано {lineNumber:N0} строк, {_index.Count} точек индексации");
            }
        }

        _totalLines = lineNumber;
        _fileSize = fs.Length;

        var elapsed = DateTime.UtcNow - startTime;
        _logger.Info($"Индекс построен за {elapsed.TotalSeconds:F2} сек: {_totalLines:N0} строк, {_index.Count} точек индексации");
    }

    /// <summary>
    /// Получает приблизительную позицию в файле для указанной строки
    /// </summary>
    /// <param name="lineNumber">Номер строки (0-based)</param>
    /// <returns>Позиция в файле (в байтах)</returns>
    public long GetApproximatePosition(int lineNumber)
    {
        if (lineNumber < 0)
            return 0;

        if (lineNumber >= _totalLines)
            return _fileSize;

        // Находим ближайшую проиндексированную строку
        var indexedLine = (lineNumber / IndexInterval) * IndexInterval;

        if (_index.TryGetValue(indexedLine, out var position))
            return position;

        // Ищем предыдущую проиндексированную строку
        while (indexedLine > 0)
        {
            indexedLine -= IndexInterval;
            if (_index.TryGetValue(indexedLine, out position))
                return position;
        }

        return 0;
    }

    /// <summary>
    /// Получает диапазон строк начиная с указанной позиции
    /// </summary>
    /// <param name="startLineNumber">Начальная строка (0-based)</param>
    /// <param name="count">Количество строк</param>
    /// <param name="cancellationToken">Токен отмены</param>
    /// <returns>Коллекция строк</returns>
    public async Task<List<string>> GetLinesAsync(int startLineNumber, int count, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrEmpty(_filePath))
            throw new InvalidOperationException("Индекс не построен. Вызовите BuildIndexAsync сначала.");

        if (startLineNumber < 0 || startLineNumber >= _totalLines)
            return new List<string>();

        var result = new List<string>(count);
        var position = GetApproximatePosition(startLineNumber);

        using var fs = new FileStream(_filePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
        using var sr = new StreamReader(fs, Encoding.UTF8);
        fs.Seek(position, SeekOrigin.Begin);

        // Вычисляем сколько строк нужно пропустить от проиндексированной позиции
        var indexedLine = (startLineNumber / IndexInterval) * IndexInterval;
        var linesToSkip = startLineNumber - indexedLine;

        // Пропускаем строки до целевой
        for (int i = 0; i < linesToSkip; i++)
        {
            if (await sr.ReadLineAsync(cancellationToken).ConfigureAwait(false) is null)
                return result;
        }

        // Читаем нужное количество строк
        for (int i = 0; i < count; i++)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var line = await sr.ReadLineAsync(cancellationToken).ConfigureAwait(false);
            if (line is null)
                break;

            result.Add(line);
        }

        return result;
    }

    /// <summary>
    /// Сохраняет индекс на диск
    /// </summary>
    /// <param name="indexPath">Путь к файлу индекса</param>
    public async Task SaveIndexAsync(string indexPath)
    {
        if (string.IsNullOrEmpty(indexPath))
            throw new ArgumentException("Путь к индексу не может быть пустым", nameof(indexPath));

        _logger.Debug($"Сохранение индекса в файл: {indexPath}");

        var indexData = new IndexData
        {
            FilePath = _filePath,
            TotalLines = _totalLines,
            FileSize = _fileSize,
            IndexInterval = IndexInterval,
            Index = _index
        };

        var options = new JsonSerializerOptions { WriteIndented = true };
        var json = JsonSerializer.Serialize(indexData, options);

        await File.WriteAllTextAsync(indexPath, json);
        _logger.Info($"Индекс сохранен: {_index.Count} точек, {json.Length:N0} байт");
    }

    /// <summary>
    /// Загружает индекс с диска
    /// </summary>
    /// <param name="indexPath">Путь к файлу индекса</param>
    public async Task LoadIndexAsync(string indexPath)
    {
        if (string.IsNullOrEmpty(indexPath))
            throw new ArgumentException("Путь к индексу не может быть пустым", nameof(indexPath));

        if (!File.Exists(indexPath))
            throw new FileNotFoundException($"Файл индекса не найден: {indexPath}");

        _logger.Debug($"Загрузка индекса из файла: {indexPath}");

        var json = await File.ReadAllTextAsync(indexPath);
        var indexData = JsonSerializer.Deserialize<IndexData>(json);

        if (indexData is null)
            throw new InvalidOperationException("Не удалось десериализовать индекс");

        _filePath = indexData.FilePath;
        _totalLines = indexData.TotalLines;
        _fileSize = indexData.FileSize;
        _index.Clear();

        foreach (var kvp in indexData.Index)
        {
            _index[kvp.Key] = kvp.Value;
        }

        _logger.Info($"Индекс загружен: {_index.Count} точек для {_totalLines:N0} строк");
    }

    /// <summary>
    /// Проверяет актуальность индекса
    /// </summary>
    /// <returns>true если индекс актуален (размер файла не изменился)</returns>
    public bool IsIndexValid()
    {
        if (string.IsNullOrEmpty(_filePath) || !File.Exists(_filePath))
            return false;

        var currentFileSize = new FileInfo(_filePath).Length;
        return currentFileSize == _fileSize;
    }

    private class IndexData
    {
        public string FilePath { get; set; }
        public int TotalLines { get; set; }
        public long FileSize { get; set; }
        public int IndexInterval { get; set; }
        public Dictionary<int, long> Index { get; set; }
    }
}
