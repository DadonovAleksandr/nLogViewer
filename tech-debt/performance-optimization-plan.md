# План оптимизации производительности nLogViewer

**Дата:** 2025-11-08
**Автор:** Техническая документация
**Статус:** 🟡 В планировании

---

## 📋 Содержание

- [Обзор проблемы](#обзор-проблемы)
- [Обнаруженные узкие места](#обнаруженные-узкие-места)
- [Фаза 1: Быстрые победы](#фаза-1-быстрые-победы)
- [Фаза 2: Средние улучшения](#фаза-2-средние-улучшения)
- [Фаза 3: Продвинутые оптимизации](#фаза-3-продвинутые-оптимизации)
- [Сводная таблица](#сводная-таблица-оптимизаций)
- [Рекомендации по приоритетам](#рекомендации-по-приоритетам)

---

## 🔍 Обзор проблемы

### Симптомы
- Приложение зависает при открытии больших файлов логов
- Высокое потребление памяти 
- Медленная реакция UI при скроллинге
- Постоянная нагрузка на CPU даже в состоянии покоя

### Архитектура чтения логов

```
┌─────────────────────────────────────────────────────────────────┐
│                         UI Layer (WPF)                          │
│ LogViewerViewModel ← UI Commands (Clear, Pause, etc)           │
└────────────────────────┬────────────────────────────────────────┘
                         │
                         ▼
┌─────────────────────────────────────────────────────────────────┐
│               Collection & Virtualization Layer                 │
│ VirtualizingLogCollection (lazy loading, pagination)           │
│ List<ILogEntry> or CircularBuffer<ILogEntry>                  │
└────────────────────────┬────────────────────────────────────────┘
                         │
                         ▼
┌─────────────────────────────────────────────────────────────────┐
│                    LogViewer Service                            │
│ ├─ State Machine: Stop → ReadAllMsg → ReadNewMsg → Pause       │
│ ├─ Timer-based polling (every 2000ms)                          │
│ ├─ SemaphoreSlim lock for thread-safe updates                  │
│ └─ Error aggregation & throttling                              │
└────────────────────────┬────────────────────────────────────────┘
                         │
                         ▼
┌─────────────────────────────────────────────────────────────────┐
│              Log Source / Reader Layer                          │
│ RepositoryLogSource (adapter pattern)                          │
│        │                              │                        │
│        ▼                              ▼                        │
│   FileLogReader              ILogRepository                     │
│  (legacy interface)          (new repository pattern)           │
│                                       │                        │
│                    ┌──────────────────┼──────────────────┐     │
│                    │                  │                  │     │
│                    ▼                  ▼                  ▼     │
│            FileLogRepository   CompositeLogRepository   ...    │
│            (single source)      (multiple sources)             │
└────────────────────────┬────────────────────────────────────────┘
                         │
                         ▼
┌─────────────────────────────────────────────────────────────────┐
│                    Parsing Layer                                │
│ ├─ TryParseLogEntryFast() - быстрый парсинг (без regex)       │
│ ├─ TryParseLogEntry() - regex fallback для многострочных      │
│ ├─ Multi-line entry support (StringBuilder accumulation)       │
│ └─ DateTime parsing, Enum conversion, int parsing              │
└────────────────────────┬────────────────────────────────────────┘
                         │
                         ▼
┌─────────────────────────────────────────────────────────────────┐
│                   File I/O Layer                                │
│ ├─ StreamReader with FileShare.ReadWrite                       │
│ ├─ Position tracking (_pos field) for incremental reads        │
│ ├─ Async/await based I/O (ConfigureAwait(false))              │
│ └─ IProgressReporter for large file loading (10MB+ threshold) │
└─────────────────────────────────────────────────────────────────┘
```

---

## 🚨 Обнаруженные узкие места

### КРИТИЧЕСКИЕ проблемы

#### 1. Отключены оптимизации памяти
**Файл:** `ServiceRegistration.cs:22,25`

```csharp
EnableDataVirtualization = false, // Временно отключаем для отладки
UseCircularBuffer = false         // Временно отключаем для отладки
```

**Проблема:**
- Весь файл логов загружается в `List<ILogEntry>`
- Для файла на 1,000,000 записей → 100-200 MB памяти
- При фильтрации создается еще одна копия данных
- UI пытается отрендерить все записи → зависание

**Приоритет:** 🔥 КРИТИЧЕСКИЙ

---

#### 2. Таймер опрашивает файл каждые 2 секунды
**Файл:** `LogViewer.cs:76-77`

```csharp
var tm = new TimerCallback(async obj => await ProcessAsync(obj));
_timer = new Timer(tm, null, 0, 2000);  // Каждые 2000ms
```

**Проблема:**
- Происходит disk I/O даже когда файл не изменяется
- Постоянная нагрузка на CPU каждые 2 секунды
- Задержка до 2 секунд перед обнаружением новых логов

**Приоритет:** 🔥 ВЫСОКИЙ

---

#### 3. CompositeLogRepository загружает все в память для сортировки
**Файл:** `CompositeLogRepository.cs:81-95`

```csharp
var allLines = new List<(DateTime, string, string)>();

foreach (var task in tasks)
{
    await foreach (var item in task)
    {
        allLines.Add(item);  // ⚠️ Миллионы строк в List!
    }
}

// Сортируем в памяти
var sortedLines = allLines.OrderBy(x => x.timestamp).ToList();
```

**Проблема:**
- При работе с несколькими файлами ВСЕ строки загружаются в память
- 2 файла по 500k записей = 1 млн строк в памяти (150-300 MB)
- `OrderBy` создает еще одну копию → 300-600 MB
- GC pressure

**Приоритет:** 🔥 ВЫСОКИЙ

---

#### 4. Парсинг многострочных записей использует Regex
**Файл:** `FileLogReader.cs:171-181`

```csharp
// Try fast parsing first (avoids regex overhead)
if (TryParseLogEntryFast(currentTextSpan, out var fastEntry))
{
    yield return fastEntry;
}
else
{
    // Fallback to regex parsing for complex multi-line entries
    var match = LogEntryPattern.Match(currentText.Trim());  // ⚠️ Медленно!
    // ...
}
```

**Проблема:**
- Для многострочных записей всегда используется медленный Regex
- Regex парсинг в 10-20 раз медленнее `TryParseLogEntryFast`
- Много аллокаций памяти (Match, Group объекты)

**Производительность:**
- `TryParseLogEntryFast`: ~200-500 ns/запись
- Regex парсинг: ~2000-5000 ns/запись

**Приоритет:** 🟡 СРЕДНИЙ

---

#### 5. Построчное чтение без буферизации
**Файл:** `FileLogReader.cs:511-523`

```csharp
while (await sr.ReadLineAsync(cancellationToken).ConfigureAwait(false) is { } line)
{
    _lineCount++;
    yield return line;  // ⚠️ Возвращаем по одной строке

    if (progressReporter != null && _lineCount % 1000 == 0)
    {
        progressReporter.Report(fs.Position, fs.Length, $"Обработано {_lineCount} строк");
    }
}
```

**Проблема:**
- Каждый `yield return` - возврат в вызывающий код
- Парсинг происходит для каждой строки отдельно
- UI обновляется после каждой распарсенной записи
- Много переключений контекста

**Приоритет:** 🟡 СРЕДНИЙ

---

## 🚀 ФАЗА 1: Быстрые победы

**Оценка времени:** 2-3 часа
**Ожидаемый эффект:** Снижение памяти на 90-99%, ускорение загрузки в 10-20 раз

---

### 1.1 Вынести интервал polling в настройки

**Файлы:** `IAppConfig.cs`, `ServiceRegistration.cs`, `LogViewer.cs`

**Текущая проблема:**
```csharp
// LogViewer.cs:77 - хардкод интервала
_timer = new Timer(tm, null, 0, 2000); // Всегда 2 секунды
```

**Решение:**

1. Добавить интерфейс для настроек производительности:
```csharp
// Новый файл: IPerformanceConfig.cs
public interface IPerformanceConfig
{
    /// <summary>
    /// Интервал проверки новых записей в файле (миллисекунды)
    /// По умолчанию: 2000 (2 секунды)
    /// Рекомендуется: 5000-10000 для снижения нагрузки
    /// </summary>
    [DefaultValue(2000)]
    int PollingIntervalMs { get; set; }
}
```

2. Добавить в `IAppConfig`:
```csharp
public interface IAppConfig
{
    IFilterConfig FilterConfig { get; set; }
    IUIConfig UIConfig { get; set; }
    IPerformanceConfig PerformanceConfig { get; set; } // ← Новое
}
```

3. Использовать в `LogViewer`:
```csharp
private readonly IAppConfig _appConfig;

public LogViewer(ILogReaderFactory readerFactory,
                 MemoryConfiguration memoryConfig,
                 IAppConfig appConfig, // ← Inject
                 IProgressReporter progressReporter = null)
{
    _appConfig = appConfig;
    // ...
    Initialize();
}

private void Initialize()
{
    // ...
    var pollingInterval = _appConfig?.PerformanceConfig?.PollingIntervalMs ?? 2000;
    var tm = new TimerCallback(async obj => await ProcessAsync(obj));
    _timer = new Timer(tm, null, 0, pollingInterval);
}
```

**Преимущества:**
- ✅ Пользователь может настроить под свои нужды
- ✅ Простая реализация без сложной логики
- ✅ Значение по умолчанию 2000ms (обратная совместимость)

**Рекомендуемые значения:**
- **1000ms** - для активного мониторинга в реальном времени
- **2000ms** - по умолчанию (текущее поведение)
- **5000-10000ms** - для снижения нагрузки на CPU

**Эффект:** 🔥 Снижение CPU usage на 50-75% при увеличении интервала до 5-10 секунд

---

### 1.2 Включить CircularBuffer или виртуализацию

**Файл:** `ServiceRegistration.cs`

**Вариант A: CircularBuffer (для real-time мониторинга)**
```csharp
services.AddSingleton<MemoryConfiguration>(provider => new MemoryConfiguration
{
    MaxEntriesInMemory = 100_000,
    EnableDataVirtualization = false,
    VirtualizationPageSize = 100,
    MaxCachedPages = 10,
    UseCircularBuffer = true  // ← Включить!
});
```

**Вариант B: Виртуализация (для анализа больших файлов)**
```csharp
services.AddSingleton<MemoryConfiguration>(provider => new MemoryConfiguration
{
    MaxEntriesInMemory = 100_000,
    EnableDataVirtualization = true,  // ← Включить!
    VirtualizationPageSize = 100,
    MaxCachedPages = 10,
    UseCircularBuffer = false
});
```

**Как работает CircularBuffer:**
```
┌─────────────────────────────────────────┐
│     CircularBuffer (кольцевой буфер)    │
│  ┌──┬──┬──┬──┬──┬──┬──┬──┬──┬──┐       │
│  │ 1│ 2│ 3│ 4│ 5│ 6│ 7│ 8│ 9│10│       │ Емкость: 10
│  └──┴──┴──┴──┴──┴──┴──┴──┴──┴──┘       │
│      ↑_start              ↑_end         │
│                                         │
│  Добавляем 11-й элемент:               │
│  ┌──┬──┬──┬──┬──┬──┬──┬──┬──┬──┐       │
│  │11│ 2│ 3│ 4│ 5│ 6│ 7│ 8│ 9│10│       │
│  └──┴──┴──┴──┴──┴──┴──┴──┴──┴──┘       │
│   ↑_end                   ↑_start       │
│  Самая старая запись (1) удалена!      │
└─────────────────────────────────────────┘
```

**Преимущества:**
- ✅ Постоянное потребление памяти: 10-20 MB вместо сотен
- ✅ Нет аллокаций при добавлении новых записей (перезаписываем старые)
- ✅ O(1) сложность для Add/Get

**Trade-offs:**
- ⚠️ Теряются старые записи (хранится только последние 100k)
- ⚠️ Нужно решить, какое количество записей хранить

**Эффект:** 🔥 Снижение потребления памяти в 10-20 раз

**Рекомендация:**
- Для **просмотра в реальном времени** → `UseCircularBuffer = true`
- Для **анализа больших файлов** → `EnableDataVirtualization = true`

---

### 1.3 Улучшенный fast parser для многострочных записей

**Файл:** `FileLogReader.cs`

**Новый метод:**
```csharp
/// <summary>
/// Быстрый парсинг с поддержкой многострочных записей
/// Ищет pipes с конца строки вместо начала
/// </summary>
private bool TryParseLogEntryFastMultiline(ReadOnlySpan<char> logText, out ILogEntry entry)
{
    entry = null;

    // Многострочная запись может содержать \n в message, но формат остается:
    // "2024-08-21 10:30:45.1234 | INFO | Message\nwith\nnewlines | Source | 1234 | 5678"

    // Ищем первый pipe после даты
    int pipeIndex1 = logText.IndexOf('|');
    if (pipeIndex1 == -1) return false;

    var dateTimeSpan = logText[..pipeIndex1].Trim();
    if (!DateTime.TryParseExact(dateTimeSpan, "yyyy-MM-dd HH:mm:ss.ffff".AsSpan(),
                                 null, DateTimeStyles.None, out DateTime dateTime))
        return false;

    var remaining = logText[(pipeIndex1 + 1)..];
    int pipeIndex2 = remaining.IndexOf('|');
    if (pipeIndex2 == -1) return false;

    var levelSpan = remaining[..pipeIndex2].Trim();
    if (!Enum.TryParse(levelSpan, true, out LogEntryType type))
        type = LogEntryType.Fatal;

    remaining = remaining[(pipeIndex2 + 1)..];

    // КЛЮЧЕВОЕ ОТЛИЧИЕ: ищем pipe с КОНЦА строки для ThreadId
    // Формат: ... | Source | ProcessId | ThreadId
    //              ↑3       ↑2          ↑1 (последний pipe)

    int lastPipeIndex = remaining.LastIndexOf('|');
    if (lastPipeIndex == -1) return false;

    var threadSpan = remaining[(lastPipeIndex + 1)..].Trim();
    int.TryParse(threadSpan, out int thread);

    remaining = remaining[..lastPipeIndex];
    int secondLastPipeIndex = remaining.LastIndexOf('|');
    if (secondLastPipeIndex == -1) return false;

    var processSpan = remaining[(secondLastPipeIndex + 1)..].Trim();
    int.TryParse(processSpan, out int process);

    remaining = remaining[..secondLastPipeIndex];
    int thirdLastPipeIndex = remaining.LastIndexOf('|');
    if (thirdLastPipeIndex == -1) return false;

    var sourceSpan = remaining[(thirdLastPipeIndex + 1)..].Trim();
    var source = sourceSpan.ToString();

    // ВСЕ что осталось - это message (может содержать \n и любые символы!)
    var messageSpan = remaining[..thirdLastPipeIndex].Trim();
    var message = messageSpan.ToString();

    entry = new LogEntry(dateTime, type, message, source, process, thread);
    return true;
}
```

**Использование в ParseLogEntries:**
```csharp
// Сначала пытаемся улучшенный fast parser
if (TryParseLogEntryFastMultiline(currentTextSpan, out var fastEntry))
{
    yield return fastEntry;
    currentMessage.Clear();
}
else
{
    // Fallback на regex только для совсем странных случаев
    var match = LogEntryPattern.Match(currentText.Trim());
    // ...
}
```

**Эффект:** 🔥 Ускорение парсинга в 5-10 раз (особенно для многострочных логов)

---

## ⚡ ФАЗА 2: Средние улучшения

**Оценка времени:** 4-6 часов
**Ожидаемый эффект:** Дополнительное ускорение на 30-50%, улучшение работы с несколькими файлами

---

### 2.1 Батчинг (пакетная обработка) при чтении

**Файл:** `FileLogReader.cs`

**Новый метод:**
```csharp
/// <summary>
/// Читает файл пакетами вместо построчного чтения
/// </summary>
private async IAsyncEnumerable<IReadOnlyList<string>> ReadLogFileBatchedAsync(
    int batchSize = 1000,
    [EnumeratorCancellation] CancellationToken cancellationToken = default)
{
    if (!File.Exists(_path))
        yield break;

    using var fs = new FileStream(_path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
    using var sr = new StreamReader(fs, Encoding.UTF8);

    if (_pos > 0 && fs.Length >= _pos)
        fs.Seek(_pos, SeekOrigin.Begin);

    var batch = new List<string>(batchSize);

    while (await sr.ReadLineAsync(cancellationToken).ConfigureAwait(false) is { } line)
    {
        batch.Add(line);

        if (batch.Count >= batchSize)
        {
            yield return batch; // Возвращаем 1000 строк сразу!
            batch = new List<string>(batchSize);
        }
    }

    // Возвращаем остаток
    if (batch.Count > 0)
        yield return batch;

    _pos = fs.Position;
}
```

**Использование:**
```csharp
await foreach (var batch in ReadLogFileBatchedAsync(1000, cancellationToken))
{
    // Парсим 1000 строк
    var entries = new List<ILogEntry>(batch.Count);
    foreach (var line in batch)
    {
        if (TryParseLine(line, out var entry))
            entries.Add(entry);
    }

    // Добавляем 1000 записей в коллекцию ОДНОЙ операцией
    if (_memoryConfig?.UseCircularBuffer == true)
        _circularBuffer.AddRange(entries);
    else
        _logEntries.AddRange(entries);

    // Обновляем UI ОДИН раз для 1000 записей
    EntriesChanged?.Invoke();
}
```

**Преимущества:**
- ✅ UI обновляется реже (меньше "моргания")
- ✅ Меньше переключений контекста
- ✅ `AddRange` эффективнее чем 1000x `Add`
- ✅ Можно распараллелить парсинг батчей

**Эффект:** 🔥 Ускорение загрузки файлов на 30-50%

---

### 2.2 Memory-Mapped файлы для больших логов

**Файл:** `FileLogReader.cs`

**Новый метод:**
```csharp
/// <summary>
/// Использует Memory-Mapped файлы для эффективной работы с большими логами (>100MB)
/// </summary>
private async IAsyncEnumerable<ILogEntry> ReadUsingMemoryMappedFile(
    [EnumeratorCancellation] CancellationToken cancellationToken = default)
{
    var fileInfo = new FileInfo(_path);
    var fileSize = fileInfo.Length;

    // Маппим файл в память (НЕ загружает весь файл сразу!)
    using var mmf = MemoryMappedFile.CreateFromFile(
        _path,
        FileMode.Open,
        null,
        fileSize,
        MemoryMappedFileAccess.Read);

    using var accessor = mmf.CreateViewAccessor(0, fileSize, MemoryMappedFileAccess.Read);

    // Читаем большими блоками (16MB)
    const int chunkSize = 16 * 1024 * 1024;
    var buffer = new byte[chunkSize];
    long position = _pos;
    var lineBuilder = new StringBuilder();

    while (position < fileSize)
    {
        cancellationToken.ThrowIfCancellationRequested();

        // Читаем блок 16MB
        var bytesToRead = (int)Math.Min(chunkSize, fileSize - position);
        accessor.ReadArray(position, buffer, 0, bytesToRead);

        // Парсим строки из блока
        var text = Encoding.UTF8.GetString(buffer, 0, bytesToRead);
        var lines = text.Split('\n');

        foreach (var line in lines)
        {
            // ... парсинг ...
        }

        position += bytesToRead;
    }

    _pos = position;
}
```

**Рекомендация:** Использовать MMF только для файлов >100MB

**Эффект:** 🔥 Ускорение чтения больших файлов в 2-5 раз

---

### 2.3 K-way merge для CompositeLogRepository

**Файл:** `CompositeLogRepository.cs`

**Проблема:** Текущий код загружает ВСЕ строки из всех файлов в память, затем сортирует.

**Решение:** Потоковое слияние (как в MergeSort)

```csharp
/// <summary>
/// K-way merge: объединяет несколько отсортированных потоков без загрузки в память
/// </summary>
public async IAsyncEnumerable<string> ReadAllLinesAsync(
    [EnumeratorCancellation] CancellationToken cancellationToken = default)
{
    // Используем PriorityQueue для эффективного поиска минимума
    var pq = new PriorityQueue<
        (IAsyncEnumerator<string> enumerator, string line, string sourceId),
        DateTime>();

    // Инициализация: читаем первую строку из каждого источника
    foreach (var repo in _repositories)
    {
        var enumerator = repo.ReadAllLinesAsync(cancellationToken).GetAsyncEnumerator();
        if (await enumerator.MoveNextAsync())
        {
            var line = enumerator.Current;
            var timestamp = ExtractTimestamp(line);
            pq.Enqueue((enumerator, line, repo.SourceId), timestamp);
        }
    }

    // K-way merge: всегда выбираем строку с минимальным timestamp
    while (pq.Count > 0)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var (enumerator, line, sourceId) = pq.Dequeue();
        yield return line;

        // Читаем следующую строку из этого источника
        if (await enumerator.MoveNextAsync())
        {
            var nextLine = enumerator.Current;
            var nextTimestamp = ExtractTimestamp(nextLine);
            pq.Enqueue((enumerator, nextLine, sourceId), nextTimestamp);
        }
        else
        {
            // Источник исчерпан
            await enumerator.DisposeAsync();
        }
    }
}
```

**Визуализация:**
```
Файл 1: [10:00:01] [10:00:03] [10:00:05] [10:00:07]
              ↑ current

Файл 2: [10:00:02] [10:00:04] [10:00:06] [10:00:08]
              ↑ current

Выбираем MIN(10:00:01, 10:00:02) = 10:00:01 → yield return
Файл 1 сдвигается: current = 10:00:03

Результат: отсортированный поток без загрузки в память!
```

**Сложность:**
- Старая: O(N log N) memory, O(N log N) time
- Новая: O(K) memory, O(N log K) time
  - K = количество файлов
  - В памяти только K строк!

**Эффект:** 🔥 Снижение потребления памяти в 1000+ раз при работе с несколькими файлами

---

## 🎯 ФАЗА 3: Продвинутые оптимизации

**Оценка времени:** 8-12 часов
**Применять только если нужна максимальная производительность**

---

### 3.1 FileSystemWatcher вместо полинга

**Файл:** `LogViewer.cs`

**Гибридный подход:**
```csharp
private FileSystemWatcher _watcher;
private Timer _fallbackTimer;
private DateTime _lastProcessedTime = DateTime.UtcNow;

private void Initialize()
{
    // FileSystemWatcher для быстрой реакции
    InitializeFileWatcher();

    // Fallback timer каждые 30 секунд (на случай пропущенных событий)
    _fallbackTimer = new Timer(_ => CheckForMissedChanges(), null, 30000, 30000);
}

private readonly SemaphoreSlim _processingLock = new(1, 1);
private CancellationTokenSource _debounceCts;

private void OnFileChanged(object sender, FileSystemEventArgs e)
{
    // Debouncing: игнорируем события в течение 200ms
    _debounceCts?.Cancel();
    _debounceCts = new CancellationTokenSource();
    var token = _debounceCts.Token;

    _ = Task.Run(async () =>
    {
        try
        {
            await Task.Delay(200, token); // Debounce delay

            if (!await _processingLock.WaitAsync(0))
                return; // Уже обрабатывается

            try
            {
                await ProcessNewEntriesAsync();
                _lastProcessedTime = DateTime.UtcNow;
            }
            finally
            {
                _processingLock.Release();
            }
        }
        catch (OperationCanceledException) { }
        catch (Exception ex)
        {
            _logger.Error(ex, "Ошибка обработки изменений");
        }
    }, token);
}
```

**Преимущества:**
- ✅ Мгновенная реакция на изменения (<100ms)
- ✅ Нулевая нагрузка когда файл не изменяется
- ✅ Fallback timer защищает от потерянных событий

**Trade-offs:**
- ⚠️ Сложнее код
- ⚠️ Может не работать для network drives

**Эффект:** 🔥 Снижение latency с 2 секунд до <100ms, CPU usage почти 0%

---

### 3.2 Индексирование файла

**Новый класс:** `LogFileIndex.cs`

```csharp
public class LogFileIndex
{
    // Индекс: каждая N-я запись → позиция в файле
    private readonly Dictionary<int, long> _index = new();
    private const int IndexInterval = 1000; // Индексируем каждую 1000-ю запись

    public async Task BuildIndexAsync(string filePath, CancellationToken cancellationToken)
    {
        using var fs = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
        using var sr = new StreamReader(fs);

        int lineNumber = 0;

        while (await sr.ReadLineAsync(cancellationToken) is not null)
        {
            if (lineNumber % IndexInterval == 0)
            {
                // Сохраняем позицию каждой 1000-й строки
                _index[lineNumber] = fs.Position;
            }
            lineNumber++;
        }
    }

    public long GetApproximatePosition(int lineNumber)
    {
        var indexedLine = (lineNumber / IndexInterval) * IndexInterval;

        if (_index.TryGetValue(indexedLine, out var position))
            return position;

        // Ищем предыдущий индекс
        while (indexedLine > 0)
        {
            indexedLine -= IndexInterval;
            if (_index.TryGetValue(indexedLine, out position))
                return position;
        }

        return 0;
    }

    // Сохранение/загрузка индекса на диск
    public async Task SaveIndexAsync(string indexPath) { /* ... */ }
    public async Task LoadIndexAsync(string indexPath) { /* ... */ }
}
```

**Использование:**
```csharp
public async Task<ILogEntry> GetEntryAtAsync(int index)
{
    var position = _index.GetApproximatePosition(index);

    using var fs = new FileStream(_path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
    using var sr = new StreamReader(fs);
    fs.Seek(position, SeekOrigin.Begin);

    // Читаем оставшиеся строки (максимум 1000)
    var targetLineInChunk = index % 1000;
    for (int i = 0; i < targetLineInChunk; i++)
        await sr.ReadLineAsync();

    var line = await sr.ReadLineAsync();
    return ParseLine(line);
}
```

**Эффект:** 🔥 Random access к любой записи за <100ms вместо секунд

---

### 3.3 Фоновая обработка с pipeline

**Новый подход:** Разделение на 3 этапа через каналы

```csharp
using System.Threading.Channels;

public class LogViewer
{
    private readonly Channel<LogBatch> _processingChannel;
    private readonly Channel<LogBatch> _uiChannel;
    private Task _parsingTask;
    private Task _uiUpdateTask;

    private void Initialize()
    {
        // Unbounded channel для приема сырых строк
        _processingChannel = Channel.CreateUnbounded<LogBatch>();

        // Bounded channel для UI updates (ограничиваем backpressure)
        _uiChannel = Channel.CreateBounded<LogBatch>(new BoundedChannelOptions(10)
        {
            FullMode = BoundedChannelFullMode.DropOldest
        });

        // Dedicated parsing thread
        _parsingTask = Task.Run(ParsingWorker);

        // UI update thread
        _uiUpdateTask = Task.Run(UiUpdateWorker);
    }

    // Producer: читает файл и отправляет сырые строки
    private async Task FileReaderAsync()
    {
        await foreach (var batch in ReadLogFileBatchedAsync(1000, _cancellationToken))
        {
            await _processingChannel.Writer.WriteAsync(
                new LogBatch { RawLines = batch });
        }
        _processingChannel.Writer.Complete();
    }

    // Consumer 1: парсит строки (CPU-bound, параллельно)
    private async Task ParsingWorker()
    {
        await foreach (var batch in _processingChannel.Reader.ReadAllAsync())
        {
            var entries = new ConcurrentBag<ILogEntry>();

            Parallel.ForEach(batch.RawLines, new ParallelOptions
            {
                MaxDegreeOfParallelism = Environment.ProcessorCount
            }, line =>
            {
                if (TryParseLogEntryFast(line.AsSpan(), out var entry))
                    entries.Add(entry);
            });

            await _uiChannel.Writer.WriteAsync(
                new LogBatch { Entries = entries.ToList() });
        }
        _uiChannel.Writer.Complete();
    }

    // Consumer 2: обновляет UI (rate-limited)
    private async Task UiUpdateWorker()
    {
        await foreach (var batch in _uiChannel.Reader.ReadAllAsync())
        {
            if (_memoryConfig?.UseCircularBuffer == true)
                _circularBuffer.AddRange(batch.Entries);
            else
                _logEntries.AddRange(batch.Entries);

            EntriesChanged?.Invoke();

            await Task.Delay(16); // ~60 FPS
        }
    }
}
```

**Архитектура:**
```
┌────────────────┐
│  File Reader   │ (1 async task, I/O bound)
└───────┬────────┘
        │ Channel<RawLines>
        ▼
┌────────────────┐
│  Parser Pool   │ (N threads = CPU cores, CPU bound)
└───────┬────────┘
        │ Channel<Entries>
        ▼
┌────────────────┐
│  UI Updater    │ (1 thread, rate-limited to 60 FPS)
└────────────────┘
```

**Эффект:** 🔥 Ускорение парсинга в N раз (где N = количество CPU cores)

---

## 📊 Сводная таблица оптимизаций

| # | Оптимизация | Сложность | Время | Эффект | Память | CPU | Загрузка |
|---|-------------|-----------|-------|--------|--------|-----|----------|
| **ФАЗА 1** | | | **2-3 ч** | | | | |
| 1.1 | Polling в настройки | ⭐ Легко | 20 мин | 🔥🔥 | - | -50-75% | - |
| 1.2 | CircularBuffer/Виртуализация | ⭐⭐ Средне | 30 мин | 🔥🔥🔥 | -90-99% | - | -95% |
| 1.3 | Fast parser multiline | ⭐⭐ Средне | 1 час | 🔥🔥🔥 | -20% | -80% | -50% |
| **ФАЗА 2** | | | **4-6 ч** | | | | |
| 2.1 | Батчинг | ⭐⭐ Средне | 1.5 ч | 🔥🔥 | -10% | -20% | -30% |
| 2.2 | Memory-mapped | ⭐⭐⭐ Сложно | 2 ч | 🔥🔥 | -50% | - | -50% (>100MB) |
| 2.3 | K-way merge | ⭐⭐⭐ Сложно | 2 ч | 🔥🔥🔥 | -99% | -50% | - (мульти) |
| **ФАЗА 3** | | | **8-12 ч** | | | | |
| 3.1 | FileSystemWatcher | ⭐⭐ Средне | 2 ч | 🔥🔥 | - | -95% | latency -95% |
| 3.2 | Индексирование | ⭐⭐⭐ Сложно | 3 ч | 🔥🔥 | +0.1% | - | Random +99% |
| 3.3 | Background pipeline | ⭐⭐⭐⭐ Сложно | 4 ч | 🔥🔥🔥 | - | +N cores | -70% |

---

## 🎯 Рекомендации по приоритетам

### Начать с ФАЗЫ 1 (2-3 часа)

**Обязательно:**
1. ✅ Вынести интервал polling в настройки приложения (`IPerformanceConfig`)
2. ✅ Включить CircularBuffer или виртуализацию (`ServiceRegistration.cs`)
3. ✅ Улучшить fast parser для многострочных записей

**Ожидаемый результат:**
- Снижение памяти на 90-99%
- Ускорение загрузки больших файлов в 10-20 раз
- Снижение CPU usage на 50-75%

---

### Затем ФАЗА 2 (если проблема остается)

**Рекомендуется:**
4. ✅ Батчинг чтения (30-50% ускорение)
5. ✅ K-way merge для CompositeLogRepository (только если используются несколько файлов)

**Опционально:**
6. Memory-Mapped файлы (только для файлов >100MB)

---

### ФАЗА 3 - опционально

**Только если нужна максимальная производительность:**
7. FileSystemWatcher (real-time обновления)
8. Индексирование (быстрый random access)
9. Background pipeline (максимальная утилизация CPU)

---

## 📝 Чеклист реализации

### Фаза 1
- [ ] **1.1 Polling в настройки:**
  - [ ] Создать `IPerformanceConfig.cs` с параметром `PollingIntervalMs`
  - [ ] Добавить `IPerformanceConfig` в `IAppConfig`
  - [ ] Inject `IAppConfig` в `LogViewer` конструктор
  - [ ] Использовать значение из конфига в `Initialize()`
  - [ ] Добавить UI для настройки интервала (опционально)
- [ ] **1.2 CircularBuffer или виртуализация:**
  - [ ] Изменить `ServiceRegistration.cs`: включить `UseCircularBuffer = true` ИЛИ `EnableDataVirtualization = true`
  - [ ] Протестировать оба варианта на файлах >100MB
  - [ ] Выбрать подходящий вариант для use case
- [ ] **1.3 Fast parser multiline:**
  - [ ] Реализовать `TryParseLogEntryFastMultiline` в `FileLogReader.cs`
  - [ ] Заменить порядок вызова: multiline fast first, потом regex fallback
  - [ ] Протестировать на многострочных логах
- [ ] **Тестирование:**
  - [ ] Протестировать на файлах разных размеров (1MB, 10MB, 100MB, 1GB)
  - [ ] Замерить потребление памяти до/после
  - [ ] Замерить время загрузки до/после
  - [ ] Замерить CPU usage в idle состоянии

### Фаза 2
- [ ] Реализовать `ReadLogFileBatchedAsync` в `FileLogReader.cs`
- [ ] Обновить `LogViewer.ProcessAsync` для использования батчинга
- [ ] Реализовать K-way merge в `CompositeLogRepository.cs`
- [ ] (Опционально) Добавить Memory-Mapped файлы для больших логов
- [ ] Протестировать с несколькими файлами одновременно

### Фаза 3
- [ ] Реализовать `FileSystemWatcher` в `LogViewer.cs`
- [ ] Создать класс `LogFileIndex`
- [ ] Интеграция индексирования с виртуализацией
- [ ] Реализовать background pipeline с каналами
- [ ] Benchmarking и профилирование

---

## 🔗 Связанные файлы

- `nLogViewer/Services/ServiceRegistration.cs` - конфигурация памяти
- `nLogViewer/Services/LogViewer/LogViewer.cs` - основная логика чтения
- `nLogViewer/Services/LogReader/FileLogReader/FileLogReader.cs` - парсинг файлов
- `nLogViewer/Services/LogReader/Repository/CompositeLogRepository.cs` - мульти-файл
- `nLogViewer/Infrastructure/Collections/CircularBuffer.cs` - кольцевой буфер
- `nLogViewer/Infrastructure/Collections/VirtualizingLogCollection.cs` - виртуализация
- `nLogViewer/Model/AppSettings/AppConfig/IAppConfig.cs` - интерфейс конфигурации
- `nLogViewer/Model/AppSettings/AppConfig/IPerformanceConfig.cs` - **[СОЗДАТЬ]** настройки производительности

---

## 📚 Дополнительные ресурсы

### Бенчмарки производительности
- Regex vs IndexOf parsing: https://benchmarksgame-team.pages.debian.net/benchmarksgame/
- Memory-Mapped Files: https://docs.microsoft.com/en-us/dotnet/standard/io/memory-mapped-files

### Паттерны
- Producer-Consumer с Channels: https://devblogs.microsoft.com/dotnet/an-introduction-to-system-threading-channels/
- K-way merge: https://en.wikipedia.org/wiki/K-way_merge_algorithm

---

**Последнее обновление:** 2025-11-08
**Версия документа:** 1.0
