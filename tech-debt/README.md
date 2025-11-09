# Tech Debt & Performance Optimization

Эта директория содержит документацию по техническому долгу и оптимизации производительности nLogViewer.

---

## 📚 Документы

### Основные документы

1. **[performance-optimization-plan.md](performance-optimization-plan.md)** ⭐
   - Полный план оптимизации производительности (3 фазы)
   - Анализ узких мест
   - Реализованные оптимизации Фазы 1
   - Рекомендации по приоритетам

2. **[benchmark-guide.md](benchmark-guide.md)** 🧪
   - Полное руководство по тестированию производительности
   - Генерация тестовых файлов
   - Запуск бенчмарков
   - Интерпретация результатов
   - Критерии успеха

3. **[benchmark-tools-summary.md](benchmark-tools-summary.md)** 📊
   - Краткое резюме инструментов бенчмаркинга
   - Быстрый старт
   - Примеры использования

---

## 🚀 Быстрый старт: Тестирование производительности

### На Windows (рекомендуется)

```powershell
# Запуск полного набора тестов
.\run-benchmarks.ps1
```

### На Linux/macOS

```bash
# Запуск полного набора тестов
./run-benchmarks.sh
```

### Ручной запуск

```bash
# Генерация тестового файла
dotnet run --project nLogViewer.Tester benchmark generate test-100k.log 100000

# Запуск сравнительных тестов
dotnet run --project nLogViewer.Tester benchmark compare test-100k.log --output results.json
```

---

## 📋 Статус оптимизаций

### ✅ Фаза 1: Завершена (2025-11-09)

**Коммиты:** `604fc67`, `768947a`
**Ветка:** `feature/perfomance-phase-1`

**Реализовано:**
1. ✅ Настраиваемый интервал polling через UI
2. ✅ Включен CircularBuffer для оптимизации памяти
3. ✅ Улучшенный fast parser для многострочных записей

**Ожидаемые результаты:**
- Снижение памяти: **-90-99%**
- Ускорение загрузки: **+1000-2000%**
- Снижение CPU usage: **-50-75%**

**Требуется:**
- [ ] Тестирование на Windows
- [ ] Замер реальных метрик
- [ ] Pull Request

### 🔜 Фаза 2: Средние улучшения

**Оценка времени:** 4-6 часов

Планируется:
- Батчинг (пакетная обработка) при чтении
- Memory-Mapped файлы для больших логов (>100MB)
- K-way merge для CompositeLogRepository

### 🔮 Фаза 3: Продвинутые оптимизации

**Оценка времени:** 8-12 часов

Планируется:
- FileSystemWatcher вместо полинга
- Индексирование файла
- Фоновая обработка с pipeline

---

## 🧪 Инструменты бенчмаркинга

### Структура

```
nLogViewer/
├── Infrastructure/
│   └── Benchmarking/
│       ├── BenchmarkResult.cs       # Результаты тестов
│       ├── MetricsCollector.cs      # Сборщик метрик
│       └── LogViewerBenchmark.cs    # Основной класс
│
└── nLogViewer.Tester/
    └── BenchmarkRunner.cs           # Консольная утилита

Scripts:
├── run-benchmarks.sh                # Bash скрипт
└── run-benchmarks.ps1               # PowerShell скрипт
```

### Команды

```bash
# Генерация тестового файла
benchmark generate <path> <count> [--multiline]

# Запуск одиночного теста
benchmark run <path> [--polling <ms>] [--output <json>]

# Сравнительные тесты (3 конфигурации)
benchmark compare <path> [--output <json>]
```

### Примеры

```bash
# Маленький файл (10k)
dotnet run --project nLogViewer.Tester benchmark generate test-10k.log 10000
dotnet run --project nLogViewer.Tester benchmark compare test-10k.log

# Средний файл (100k)
dotnet run --project nLogViewer.Tester benchmark generate test-100k.log 100000
dotnet run --project nLogViewer.Tester benchmark compare test-100k.log

# Большой файл (1M)
dotnet run --project nLogViewer.Tester benchmark generate test-1m.log 1000000
dotnet run --project nLogViewer.Tester benchmark compare test-1m.log

# Многострочные записи
dotnet run --project nLogViewer.Tester benchmark generate test-ml.log 100000 --multiline
dotnet run --project nLogViewer.Tester benchmark compare test-ml.log
```

---

## 📊 Метрики производительности

### Что измеряется

- **Load Time:** Время загрузки файла (мс)
- **Memory Used:** Потребление памяти (байты)
- **Peak Memory:** Пиковая память (байты)
- **Throughput:** Записей в секунду
- **CPU Usage:** Среднее CPU (%) - только Windows

### Целевые значения (100k записей)

| Метрика | До | После Фазы 1 | Улучшение |
|---------|-----|--------------|-----------|
| Load Time | 4-6 сек | < 2.5 сек | -50% |
| Memory Used | 120-180 MB | < 20 MB | -90% |
| Throughput | 15-25k e/s | > 40k e/s | +100% |

---

## 🔗 Связанные PR

- **Фаза 1:** https://github.com/DadonovAleksandr/nLogViewer/pull/new/feature/perfomance-phase-1

---

## 📝 Чек-лист для тестирования

### Автоматические тесты

- [ ] Запустить `./run-benchmarks.sh` или `.\run-benchmarks.ps1`
- [ ] Проверить результаты в `benchmark-results/*.json`
- [ ] Убедиться, что Memory Used < 20 MB
- [ ] Убедиться, что Throughput > 40,000 e/s

### Ручные тесты

- [ ] Протестировать на реальных файлах логов
- [ ] Проверить работу UI настроек polling
- [ ] Убедиться, что многострочные логи парсятся корректно
- [ ] Проверить, что CircularBuffer работает (старые записи удаляются)

---

## 🆘 Поддержка

### Проблемы с компиляцией на Linux

**Ошибка:**
```
error MSB4019: Microsoft.NET.Sdk.WindowsDesktop.targets was not found
```

**Решение:**
- WPF проекты требуют Windows
- Используйте Windows машину для тестирования
- Или используйте удаленный Windows сервер

### CPU Usage показывает 0%

**Причина:**
- `PerformanceCounter` не работает на Linux

**Решение:**
- Это нормально на Linux
- Для замера CPU используйте `top` или `htop` вручную

---

## 📅 История изменений

### 2025-11-09 - Добавлены инструменты бенчмаркинга

- Создана инфраструктура для автоматического тестирования
- Добавлена консольная утилита `BenchmarkRunner`
- Добавлены скрипты для автоматизации (`run-benchmarks.sh`, `run-benchmarks.ps1`)
- Создана подробная документация (`benchmark-guide.md`)

### 2025-11-09 - Фаза 1 завершена

- Реализован `IPerformanceConfig` для настройки polling
- Включен CircularBuffer
- Добавлен улучшенный fast parser для многострочных записей
- Добавлен UI для настройки интервала опроса

### 2025-11-08 - План оптимизации создан

- Проанализированы узкие места
- Определены 3 фазы оптимизации
- Создан детальный план с оценками времени

---

**Последнее обновление:** 2025-11-09
**Версия документа:** 1.0
