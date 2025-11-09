# Changelog: Инструменты бенчмаркинга

**Дата:** 2025-11-09
**Версия:** 1.0.0
**Статус:** ✅ Готово к использованию

---

## 🎯 Цель

Создать набор инструментов для автоматического тестирования результатов оптимизаций Фазы 1:
- Замер времени загрузки файлов
- Отслеживание потребления памяти
- Мониторинг CPU usage
- Сравнение производительности до/после оптимизаций

---

## ✨ Добавленные компоненты

### 1. Инфраструктура бенчмаркинга

#### `nLogViewer/Infrastructure/Benchmarking/BenchmarkResult.cs`
- Класс для хранения результатов бенчмарка
- Вычисляемые свойства: EntriesPerSecond, MbPerSecond, BytesPerEntry
- Форматированный вывод результатов
- Метаданные конфигурации

**Основные поля:**
```csharp
public long LoadTimeMs           // Время загрузки
public long MemoryUsedBytes      // Потребление памяти
public long PeakMemoryBytes      // Пиковая память
public double AverageCpuPercent  // CPU usage
public int TotalEntries          // Количество записей
```

---

#### `nLogViewer/Infrastructure/Benchmarking/MetricsCollector.cs`
- Сборщик метрик производительности
- Использует `Stopwatch` для замера времени
- Отслеживает память через `GC.GetTotalMemory()`
- Мониторит CPU через `PerformanceCounter` (Windows only)
- Периодический сбор метрик каждые 100ms

**Использование:**
```csharp
using var collector = new MetricsCollector();
// ... выполнение теста ...
var snapshot = collector.Stop();
```

---

#### `nLogViewer/Infrastructure/Benchmarking/LogViewerBenchmark.cs`
- Основной класс для запуска бенчмарков
- Поддержка различных конфигураций памяти
- Автоматические сравнительные тесты
- Экспорт результатов в JSON

**Методы:**
```csharp
Task<BenchmarkResult> RunLoadTestAsync(int pollingIntervalMs)
Task<List<BenchmarkResult>> RunComparisonTestsAsync()
static void PrintComparisonTable(List<BenchmarkResult> results)
static Task SaveResultsAsync(List<BenchmarkResult> results, string path)
```

---

### 2. Консольная утилита

#### `nLogViewer.Tester/BenchmarkRunner.cs`
- Точка входа для консольного приложения
- 3 команды: `generate`, `run`, `compare`
- Генерация тестовых файлов с контролируемым содержимым
- Поддержка аргументов командной строки

**Команды:**

```bash
# Генерация файла
benchmark generate <path> <count> [--multiline]

# Одиночный тест
benchmark run <path> [--polling <ms>] [--output <json>]

# Сравнительные тесты
benchmark compare <path> [--output <json>]
```

**Примеры:**
```bash
dotnet run --project nLogViewer.Tester benchmark generate test.log 100000
dotnet run --project nLogViewer.Tester benchmark compare test.log --output results.json
```

---

### 3. Автоматизационные скрипты

#### `run-benchmarks.sh` (Linux/macOS)
- Bash скрипт для запуска полного набора тестов
- Цветной вывод (зеленый/желтый/красный)
- Обработка ошибок
- Итоговая статистика

**Запуск:**
```bash
chmod +x run-benchmarks.sh
./run-benchmarks.sh
```

---

#### `run-benchmarks.ps1` (Windows)
- PowerShell скрипт для запуска полного набора тестов
- Цветной вывод
- Обработка ошибок
- Итоговая статистика

**Запуск:**
```powershell
.\run-benchmarks.ps1
```

---

### 4. Документация

#### `tech-debt/benchmark-guide.md` (15+ страниц)
Полное руководство по использованию инструментов бенчмаркинга:
- Быстрый старт
- Генерация тестовых файлов
- Запуск бенчмарков
- Интерпретация результатов
- Рекомендуемые тесты
- Критерии успеха Фазы 1
- Bash/PowerShell скрипты

---

#### `tech-debt/benchmark-tools-summary.md`
Краткое резюме:
- Что было создано
- Быстрый старт
- Метрики производительности
- Критерии успеха
- Следующие шаги

---

#### `tech-debt/README.md`
Обзор директории tech-debt:
- Список документов
- Быстрый старт
- Статус оптимизаций
- Чек-листы

---

#### `tech-debt/CHANGELOG-benchmarking.md` (этот файл)
История изменений и описание добавленных компонентов

---

## 📊 Функциональность

### Генерация тестовых файлов

**Поддерживаемые размеры:**
- 10,000 записей (~1 MB) - быстрый тест
- 100,000 записей (~10 MB) - стандартный тест
- 1,000,000 записей (~100 MB) - stress test
- 10,000,000+ записей (~1 GB) - extreme test

**Форматы записей:**
- Однострочные записи (80%)
- Многострочные записи (20% при --multiline)
- Различные уровни логирования (INFO, DEBUG, WARNING, ERROR, FATAL)
- Реалистичные timestamp'ы с интервалом 100ms

**Пример записи:**
```
2025-11-09 10:30:45.1234 | INFO | Log message number 123 - UUID | Source.5 | 1234 | 7
```

---

### Замеряемые метрики

#### 1. Load Time (Время загрузки)
- **Что:** Время от старта LogViewer до завершения загрузки
- **Единицы:** миллисекунды (ms)
- **Цель Фазы 1:** -50% до -80%

#### 2. Memory Used (Потребление памяти)
- **Что:** Память после загрузки минус память до загрузки
- **Единицы:** байты (выводится в MB)
- **Цель Фазы 1:** -90% до -99% (CircularBuffer)

#### 3. Peak Memory (Пиковая память)
- **Что:** Максимальное потребление во время загрузки
- **Единицы:** байты (выводится в MB)
- **Цель Фазы 1:** -85% до -95%

#### 4. Throughput (Производительность)
- **Entries/sec:** Записей в секунду
- **MB/sec:** Мегабайт в секунду
- **Цель Фазы 1:** +100% до +400%

#### 5. CPU Usage (Загрузка процессора)
- **Что:** Среднее CPU во время теста
- **Единицы:** проценты (%)
- **Платформа:** Только Windows
- **Цель Фазы 1:** -50% до -75% (при увеличении polling)

---

### Сравнительные тесты

Автоматически запускаются 3 конфигурации:

#### Test 1: Baseline (No CircularBuffer)
```csharp
UseCircularBuffer = false
MaxEntriesInMemory = int.MaxValue
PollingInterval = 2000ms
```

#### Test 2: Optimized (CircularBuffer 100k)
```csharp
UseCircularBuffer = true
MaxEntriesInMemory = 100_000
PollingInterval = 2000ms
```

#### Test 3: Optimized (CircularBuffer + 5sec polling)
```csharp
UseCircularBuffer = true
MaxEntriesInMemory = 100_000
PollingInterval = 5000ms
```

---

## 📈 Пример вывода

### Генерация файла

```
Generating test file: test-100k.log
Number of entries: 100,000
Multiline mode: false

Progress: 100,000 / 100,000 (100.0%)
✅ Generated 100,000 entries
   File size: 10.52 MB
   Path: /full/path/test-100k.log
```

### Сравнительная таблица

```
═══════════════════════════════════════════════════════════════════════════════════════
                            PERFORMANCE COMPARISON TABLE
═══════════════════════════════════════════════════════════════════════════════════════

Test Name                                Load Time       Memory Used     Peak Mem        Entries/sec
────────────────────────────────────────────────────────────────────────────────────────────────────────
Baseline (No CircularBuffer)                5,234 ms      152.3 MB       210.5 MB          19,106
Optimized (CircularBuffer 100k)             1,821 ms (-65%)  12.1 MB (-92%)   15.3 MB          54,890
Optimized (CircularBuffer + 5sec polling)   1,798 ms (-66%)  12.0 MB (-92%)   15.2 MB          55,617
────────────────────────────────────────────────────────────────────────────────────────────────────────
```

### Детальная статистика

```
═══════════════════════════════════════════════════════════════
BENCHMARK RESULT: Optimized (CircularBuffer 100k)
═══════════════════════════════════════════════════════════════
File:             10.52 MB (100,000 entries)
Load Time:        1,821 ms (1.82 sec)
Throughput:       54,890 entries/sec | 5.77 MB/sec
Memory Used:      12.1 MB
Peak Memory:      15.3 MB
Avg CPU:          45.2%
Memory/Entry:     127 bytes
Timestamp:        2025-11-09 14:23:45
Metadata:
  PollingIntervalMs   : 2000
  UseCircularBuffer   : true
  MaxEntriesInMemory  : 100000
  EnableDataVirtualization : false
  FileName            : test-100k.log
═══════════════════════════════════════════════════════════════
```

---

## 🔧 Технические детали

### Платформозависимость

#### Windows
- ✅ Полная поддержка всех метрик
- ✅ CPU monitoring через `PerformanceCounter`
- ✅ Компиляция WPF проекта

#### Linux/macOS
- ⚠️ CPU monitoring не работает (возвращает 0%)
- ❌ Невозможна компиляция WPF проекта
- ✅ Остальные метрики работают корректно

**Решение для Linux:**
- Запускать тесты на Windows машине
- Или использовать pre-built binaries
- Для CPU мониторинга использовать `top`/`htop` вручную

---

### Точность измерений

#### GC влияние
Между тестами выполняется:
```csharp
GC.Collect();
GC.WaitForPendingFinalizers();
GC.Collect();
await Task.Delay(2000);
```

Это минимизирует влияние предыдущих тестов на следующие.

#### JIT компиляция
- Первый запуск может быть медленнее (JIT-компиляция)
- Рекомендуется запускать тесты минимум 2 раза
- Или добавить "прогревочный" тест

#### Фоновые процессы
Для максимальной точности:
- Закрыть другие приложения
- Отключить антивирус (может замедлить I/O)
- Не запускать во время активной работы

---

### Таймауты

- **По умолчанию:** 5 минут на тест
- **Исключение:** `TimeoutException` при превышении
- **Для >1GB файлов:** Может потребоваться увеличение таймаута в коде

---

## ✅ Критерии успеха

### Для файла 100,000 записей (~10 MB)

| Метрика | До оптимизации | После Фазы 1 | Улучшение |
|---------|----------------|--------------|-----------|
| Load Time | 4,000-6,000 ms | < 2,500 ms | -50% |
| Memory Used | 120-180 MB | < 20 MB | -90% |
| Peak Memory | 200-300 MB | < 30 MB | -85% |
| Throughput | 15,000-25,000 e/s | > 40,000 e/s | +100% |

### Ключевой индикатор CircularBuffer

Memory Used должен оставаться **~10-20 MB** вне зависимости от размера файла:

| Размер файла | Memory Used (без CircularBuffer) | Memory Used (с CircularBuffer) |
|--------------|-----------------------------------|--------------------------------|
| 10k записей | ~15 MB | ~10 MB |
| 100k записей | ~150 MB | ~12 MB |
| 1M записей | ~1.5 GB | ~12-15 MB ⭐ |
| 10M записей | ~15 GB | ~15-20 MB ⭐⭐⭐ |

---

## 🚀 Следующие шаги

### 1. Компиляция (Windows)
```bash
dotnet build
```

### 2. Запуск тестов
```bash
# Полный набор
.\run-benchmarks.ps1

# Или вручную
dotnet run --project nLogViewer.Tester benchmark compare test-100k.log
```

### 3. Проверка результатов
- [ ] Memory Used < 20 MB для всех тестов
- [ ] Load Time улучшение > 50%
- [ ] Throughput > 40,000 entries/sec
- [ ] Нет регрессий

### 4. Коммит результатов
```bash
git add benchmark-results/
git commit -m "Добавлены результаты бенчмарков Фазы 1"
```

### 5. Pull Request
- Включить результаты JSON
- Добавить скриншоты таблицы сравнения
- Описать улучшения в метриках

---

## 🔗 Связанные файлы

### Реализация
- `nLogViewer/Infrastructure/Benchmarking/BenchmarkResult.cs`
- `nLogViewer/Infrastructure/Benchmarking/MetricsCollector.cs`
- `nLogViewer/Infrastructure/Benchmarking/LogViewerBenchmark.cs`
- `nLogViewer.Tester/BenchmarkRunner.cs`

### Скрипты
- `run-benchmarks.sh` (Linux/macOS)
- `run-benchmarks.ps1` (Windows)

### Документация
- `tech-debt/benchmark-guide.md` - полное руководство
- `tech-debt/benchmark-tools-summary.md` - краткое резюме
- `tech-debt/README.md` - обзор директории
- `tech-debt/performance-optimization-plan.md` - план оптимизаций

---

## 📝 Лицензия

Инструменты являются частью проекта nLogViewer и используют ту же лицензию.

---

**Создано:** 2025-11-09
**Автор:** Техническая документация
**Версия:** 1.0.0
**Статус:** ✅ Готово к использованию
