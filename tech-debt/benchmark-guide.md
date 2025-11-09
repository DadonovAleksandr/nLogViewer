# Руководство по тестированию производительности nLogViewer

**Дата:** 2025-11-09
**Версия:** 1.0
**Для:** Фаза 1 оптимизаций производительности

---

## 📋 Содержание

- [Обзор](#обзор)
- [Быстрый старт](#быстрый-старт)
- [Генерация тестовых файлов](#генерация-тестовых-файлов)
- [Запуск бенчмарков](#запуск-бенчмарков)
- [Интерпретация результатов](#интерпретация-результатов)
- [Рекомендуемые тесты](#рекомендуемые-тесты)
- [Автоматизация](#автоматизация)

---

## 🔍 Обзор

Для оценки результатов оптимизаций Фазы 1 создан набор инструментов для автоматического бенчмаркинга:

### Что измеряется

1. **Время загрузки файла** (LoadTimeMs)
   - Замеряется от старта `LogViewer` до завершения загрузки
   - Включает парсинг, фильтрацию и добавление в коллекцию

2. **Потребление памяти** (MemoryUsedBytes, PeakMemoryBytes)
   - Измеряется через `GC.GetTotalMemory()`
   - Отслеживается пиковое потребление во время загрузки

3. **CPU usage** (AverageCpuPercent)
   - Среднее потребление CPU во время загрузки
   - Замеряется через `PerformanceCounter` (Windows only)

4. **Производительность парсинга** (EntriesPerSecond)
   - Вычисляется как `TotalEntries / LoadTimeMs`
   - Показывает скорость обработки записей

### Что сравнивается

- **Baseline:** Без CircularBuffer, без оптимизаций
- **Optimized:** CircularBuffer (100k entries), fast parser multiline
- **Варианты polling:** 2 сек vs 5 сек vs 10 сек

---

## ⚡ Быстрый старт

### 1. Сгенерировать тестовый файл

```bash
# 100,000 записей (~10 MB)
dotnet run --project nLogViewer.Tester benchmark generate test-100k.log 100000

# 1,000,000 записей (~100 MB)
dotnet run --project nLogViewer.Tester benchmark generate test-1m.log 1000000

# С многострочными записями
dotnet run --project nLogViewer.Tester benchmark generate test-multiline.log 100000 --multiline
```

### 2. Запустить сравнительный тест

```bash
dotnet run --project nLogViewer.Tester benchmark compare test-100k.log --output results.json
```

### 3. Посмотреть результаты

Результаты выводятся в консоль и сохраняются в JSON файл:

```
═══════════════════════════════════════════════════════════════════════════════════════
                            PERFORMANCE COMPARISON TABLE
═══════════════════════════════════════════════════════════════════════════════════════

Test Name                                Load Time       Memory Used     Peak Mem        Entries/sec
────────────────────────────────────────────────────────────────────────────────────────────────────────
Baseline (No CircularBuffer)                5,234 ms      152.3 MB       210.5 MB          19,106
Optimized (CircularBuffer 100k)             1,821 ms (-65%)  12.1 MB (-92%)   15.3 MB          54,890
Optimized (CircularBuffer + 5sec polling)   1,798 ms (-66%)  12.0 MB (-92%)   15.2 MB          55,617
```

---

## 📝 Генерация тестовых файлов

### Синтаксис

```bash
dotnet run --project nLogViewer.Tester benchmark generate <output-path> <num-entries> [--multiline]
```

### Параметры

- `<output-path>` - путь к выходному файлу (будет перезаписан!)
- `<num-entries>` - количество записей для генерации
- `--multiline` - (опционально) генерировать 20% многострочных записей

### Рекомендуемые размеры файлов

| Записей | Размер файла | Назначение |
|---------|--------------|------------|
| 10,000 | ~1 MB | Быстрый тест, отладка |
| 100,000 | ~10 MB | Стандартный тест |
| 1,000,000 | ~100 MB | Большой файл, memory stress test |
| 10,000,000 | ~1 GB | Экстремальный тест (требует ~10-20 минут) |

### Примеры

```bash
# Маленький файл для быстрого теста
dotnet run --project nLogViewer.Tester benchmark generate small.log 10000

# Средний файл с многострочными записями
dotnet run --project nLogViewer.Tester benchmark generate medium.log 100000 --multiline

# Большой файл для stress теста
dotnet run --project nLogViewer.Tester benchmark generate large.log 1000000

# Экстремальный файл (1 GB)
dotnet run --project nLogViewer.Tester benchmark generate extreme.log 10000000
```

### Формат генерируемых записей

```
2025-11-09 10:30:45.1234 | INFO | Log message number 123 - 9f8e7d6c-5b4a-3210-fedc-ba9876543210 | Source.5 | 1234 | 7
2025-11-09 10:30:45.2340 | DEBUG | Message 124
Line 2: a1b2c3d4-e5f6-7890-abcd-ef1234567890
Line 3: Details | Source.2 | 5678 | 3
```

---

## 🚀 Запуск бенчмарков

### 1. Одиночный тест

Запускает тест с указанными параметрами:

```bash
dotnet run --project nLogViewer.Tester benchmark run <log-file> [--polling <ms>] [--output <path>]
```

**Примеры:**

```bash
# Baseline тест (2 сек polling)
dotnet run --project nLogViewer.Tester benchmark run test.log

# Тест с 5 сек polling
dotnet run --project nLogViewer.Tester benchmark run test.log --polling 5000

# Сохранить результаты в файл
dotnet run --project nLogViewer.Tester benchmark run test.log --output baseline.json
```

### 2. Сравнительный тест (рекомендуется)

Автоматически запускает 3 теста:
1. Baseline (без CircularBuffer)
2. Optimized (CircularBuffer 100k, polling 2 сек)
3. Optimized (CircularBuffer 100k, polling 5 сек)

```bash
dotnet run --project nLogViewer.Tester benchmark compare <log-file> [--output <path>]
```

**Примеры:**

```bash
# Сравнительный тест на 100k записях
dotnet run --project nLogViewer.Tester benchmark compare test-100k.log

# Сравнительный тест с сохранением результатов
dotnet run --project nLogViewer.Tester benchmark compare test-1m.log --output results-1m.json
```

### 3. Таймауты

- По умолчанию: **5 минут** на тест
- Если тест не завершился за 5 минут → исключение `TimeoutException`
- Для файлов >1 GB может потребоваться увеличение таймаута в коде

---

## 📊 Интерпретация результатов

### Структура вывода

Каждый тест выводит детальную статистику:

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

### Ключевые метрики

#### 1. Load Time (Время загрузки)

**Что показывает:** Сколько времени заняло чтение и парсинг файла

**Ожидаемые улучшения Фазы 1:**
- Fast parser multiline: **-50% до -80%** для многострочных логов
- CircularBuffer: **-10% до -30%** (меньше GC pressure)

**Примеры:**
- ✅ `1,821 ms (-65%)` - отличное улучшение
- ⚠️ `4,500 ms (-15%)` - слабое улучшение, проверить конфигурацию
- ❌ `6,000 ms (+15%)` - регрессия, что-то не так!

#### 2. Memory Used (Потребление памяти)

**Что показывает:** Сколько памяти занимает загруженный файл

**Ожидаемые улучшения Фазы 1:**
- CircularBuffer: **-90% до -99%** (постоянный размер 100k записей)

**Примеры:**
- ✅ `12.1 MB (-92%)` для 100k записей - идеально!
- ✅ `~10-20 MB` при любом размере файла (CircularBuffer работает)
- ❌ `152 MB` для 100k записей - CircularBuffer не включен!

**Формула без оптимизаций:**
```
Memory ≈ TotalEntries * ~150-200 bytes/entry
```

**Формула с CircularBuffer:**
```
Memory ≈ MaxEntriesInMemory * ~100-150 bytes/entry
Memory ≈ 100,000 * 120 bytes ≈ 12 MB (постоянно!)
```

#### 3. Peak Memory (Пиковая память)

**Что показывает:** Максимальное потребление памяти во время загрузки

**Почему важно:**
- Показывает GC pressure
- Если Peak >> Used → много временных аллокаций

**Ожидаемые значения:**
- ✅ `Peak ≈ Used + 20-30%` - нормально
- ⚠️ `Peak = Used * 2` - высокий GC pressure
- ❌ `Peak > Used * 3` - проблема с аллокациями

#### 4. Throughput (Производительность)

**Entries/sec:** Количество записей в секунду

**Ожидаемые значения:**
- ✅ `50,000+ entries/sec` - отлично (fast parser работает)
- ⚠️ `20,000-50,000 entries/sec` - средне
- ❌ `< 20,000 entries/sec` - медленно (regex парсинг?)

**MB/sec:** Мегабайт в секунду

**Ожидаемые значения:**
- ✅ `5-10 MB/sec` - отлично
- ⚠️ `2-5 MB/sec` - средне
- ❌ `< 2 MB/sec` - медленно

#### 5. CPU Usage

**Что показывает:** Среднее потребление CPU во время загрузки

**Примечание:** Замеряется только на Windows через `PerformanceCounter`

**Ожидаемые значения:**
- ✅ `30-60%` - нормальная загрузка
- ⚠️ `60-80%` - высокая нагрузка
- ❌ `> 80%` - возможно bottleneck

---

## ✅ Рекомендуемые тесты

### Чек-лист для тестирования Фазы 1

Запустите эти тесты **до и после** применения оптимизаций:

#### Тест 1: Маленький файл (baseline)

```bash
# Генерация
dotnet run --project nLogViewer.Tester benchmark generate test-10k.log 10000

# Тест
dotnet run --project nLogViewer.Tester benchmark compare test-10k.log --output results-10k.json
```

**Ожидаемые результаты:**
- Load Time: < 500 ms
- Memory Used: < 5 MB (CircularBuffer)
- Throughput: > 20,000 entries/sec

---

#### Тест 2: Средний файл

```bash
# Генерация
dotnet run --project nLogViewer.Tester benchmark generate test-100k.log 100000

# Тест
dotnet run --project nLogViewer.Tester benchmark compare test-100k.log --output results-100k.json
```

**Ожидаемые результаты:**
- Load Time: 1,500-3,000 ms
- Memory Used: 10-20 MB (CircularBuffer)
- Throughput: > 40,000 entries/sec

---

#### Тест 3: Большой файл

```bash
# Генерация
dotnet run --project nLogViewer.Tester benchmark generate test-1m.log 1000000

# Тест
dotnet run --project nLogViewer.Tester benchmark compare test-1m.log --output results-1m.json
```

**Ожидаемые результаты:**
- Load Time: 15,000-30,000 ms (15-30 сек)
- Memory Used: 10-20 MB (CircularBuffer!) - не зависит от размера файла!
- Throughput: > 30,000 entries/sec

---

#### Тест 4: Многострочные записи

```bash
# Генерация
dotnet run --project nLogViewer.Tester benchmark generate test-multiline.log 100000 --multiline

# Тест
dotnet run --project nLogViewer.Tester benchmark compare test-multiline.log --output results-multiline.json
```

**Ожидаемые улучшения:**
- Load Time: **-50% до -80%** (fast parser multiline!)
- Throughput: **+100% до +400%**

---

## 🤖 Автоматизация

### Bash скрипт для полного цикла тестов

Создайте файл `run-benchmarks.sh`:

```bash
#!/bin/bash

echo "Starting nLogViewer Performance Tests"
echo "======================================"

# Создаем директорию для результатов
mkdir -p benchmark-results
cd benchmark-results

# Тест 1: 10k записей
echo "\n[1/4] Testing 10k entries..."
dotnet run --project ../nLogViewer.Tester benchmark generate test-10k.log 10000
dotnet run --project ../nLogViewer.Tester benchmark compare test-10k.log --output results-10k.json

# Тест 2: 100k записей
echo "\n[2/4] Testing 100k entries..."
dotnet run --project ../nLogViewer.Tester benchmark generate test-100k.log 100000
dotnet run --project ../nLogViewer.Tester benchmark compare test-100k.log --output results-100k.json

# Тест 3: 1M записей
echo "\n[3/4] Testing 1M entries..."
dotnet run --project ../nLogViewer.Tester benchmark generate test-1m.log 1000000
dotnet run --project ../nLogViewer.Tester benchmark compare test-1m.log --output results-1m.json

# Тест 4: Многострочные
echo "\n[4/4] Testing multiline entries..."
dotnet run --project ../nLogViewer.Tester benchmark generate test-multiline.log 100000 --multiline
dotnet run --project ../nLogViewer.Tester benchmark compare test-multiline.log --output results-multiline.json

echo "\n======================================"
echo "All tests completed!"
echo "Results saved in benchmark-results/"
```

**Запуск:**

```bash
chmod +x run-benchmarks.sh
./run-benchmarks.sh
```

### PowerShell скрипт (Windows)

Создайте файл `run-benchmarks.ps1`:

```powershell
Write-Host "Starting nLogViewer Performance Tests" -ForegroundColor Green
Write-Host "======================================"

# Создаем директорию для результатов
New-Item -ItemType Directory -Force -Path benchmark-results
Set-Location benchmark-results

# Тест 1: 10k записей
Write-Host "`n[1/4] Testing 10k entries..." -ForegroundColor Yellow
dotnet run --project ..\nLogViewer.Tester benchmark generate test-10k.log 10000
dotnet run --project ..\nLogViewer.Tester benchmark compare test-10k.log --output results-10k.json

# Тест 2: 100k записей
Write-Host "`n[2/4] Testing 100k entries..." -ForegroundColor Yellow
dotnet run --project ..\nLogViewer.Tester benchmark generate test-100k.log 100000
dotnet run --project ..\nLogViewer.Tester benchmark compare test-100k.log --output results-100k.json

# Тест 3: 1M записей
Write-Host "`n[3/4] Testing 1M entries..." -ForegroundColor Yellow
dotnet run --project ..\nLogViewer.Tester benchmark generate test-1m.log 1000000
dotnet run --project ..\nLogViewer.Tester benchmark compare test-1m.log --output results-1m.json

# Тест 4: Многострочные
Write-Host "`n[4/4] Testing multiline entries..." -ForegroundColor Yellow
dotnet run --project ..\nLogViewer.Tester benchmark generate test-multiline.log 100000 --multiline
dotnet run --project ..\nLogViewer.Tester benchmark compare test-multiline.log --output results-multiline.json

Write-Host "`n======================================" -ForegroundColor Green
Write-Host "All tests completed!" -ForegroundColor Green
Write-Host "Results saved in benchmark-results/"
```

**Запуск:**

```powershell
.\run-benchmarks.ps1
```

---

## 📈 Анализ результатов JSON

Результаты сохраняются в формате JSON и могут быть проанализированы программно:

```json
[
  {
    "TestName": "Baseline (No CircularBuffer)",
    "FileSizeBytes": 11036672,
    "TotalEntries": 100000,
    "LoadTimeMs": 5234,
    "MemoryUsedBytes": 159776768,
    "PeakMemoryBytes": 220889088,
    "AverageCpuPercent": 52.3,
    "Metadata": {
      "PollingIntervalMs": 2000,
      "UseCircularBuffer": false,
      "MaxEntriesInMemory": 2147483647,
      "EnableDataVirtualization": false
    }
  },
  {
    "TestName": "Optimized (CircularBuffer 100k)",
    "LoadTimeMs": 1821,
    "MemoryUsedBytes": 12689408,
    ...
  }
]
```

---

## ⚠️ Важные замечания

### 1. Прогрев JIT

Первый запуск может быть медленнее из-за JIT-компиляции. Рекомендуется:
- Запускать тесты минимум 2 раза
- Или добавить "прогревочный" тест перед основным

### 2. GC влияние

Между тестами важно дать GC время очистить память:

```csharp
GC.Collect();
GC.WaitForPendingFinalizers();
await Task.Delay(2000);
```

Это уже встроено в `RunComparisonTestsAsync()`.

### 3. Фоновые процессы

Для точных результатов:
- Закройте другие приложения
- Не запускайте тесты во время активной работы
- Отключите антивирус (может замедлить I/O)

### 4. CPU counter на Linux

`PerformanceCounter` не работает на Linux. CPU usage будет 0%.

Альтернатива для Linux:
```bash
# Запустить тест в фоне
dotnet run --project nLogViewer.Tester benchmark run test.log &
PID=$!

# Мониторить CPU через top
top -p $PID -b -d 1 | grep $PID
```

---

## 🎯 Критерии успеха Фазы 1

После применения всех оптимизаций Фазы 1:

### Обязательные улучшения

- ✅ **Memory Used:** Снижение на **90-99%** (CircularBuffer)
- ✅ **Load Time:** Ускорение на **50-80%** (fast parser multiline)
- ✅ **Throughput:** Увеличение на **100-400%** (entries/sec)

### Проверочные значения (файл 100k записей)

| Метрика | До оптимизации | После оптимизации | Цель |
|---------|----------------|-------------------|------|
| Load Time | 4,000-6,000 ms | < 2,500 ms | -50% |
| Memory Used | 120-180 MB | < 20 MB | -90% |
| Peak Memory | 200-300 MB | < 30 MB | -85% |
| Throughput | 15,000-25,000 entries/sec | > 40,000 entries/sec | +100% |

---

## 📞 Поддержка

Если результаты тестов не соответствуют ожиданиям:

1. Проверьте конфигурацию в `ServiceRegistration.cs`:
   ```csharp
   UseCircularBuffer = true  // ← Должно быть true!
   MaxEntriesInMemory = 100_000
   ```

2. Проверьте порядок парсинга в `FileLogReader.cs`:
   ```csharp
   // 1. TryParseLogEntryFastMultiline (новый)
   // 2. TryParseLogEntryFast
   // 3. Regex fallback
   ```

3. Создайте issue с результатами JSON для анализа

---

**Последнее обновление:** 2025-11-09
**Автор:** Техническая документация
**Версия:** 1.0
