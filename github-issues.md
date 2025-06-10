# GitHub Issues для улучшения nLogViewer

## Критические исправления (Priority: High)

### Issue #1: Implement async file operations in FileLogReader ✅
**Title:** Implement async file operations to prevent UI blocking
**Labels:** `bug`, `performance`, `priority:high`
**Description:**
Currently, all file I/O operations in `FileLogReader` are synchronous, which blocks the UI thread when reading large log files.

**Tasks:**
- [x] Convert `ReadLogFile()` method to async
- [x] Implement `GetAllAsync()` and `GetNewAsync()` methods
- [x] Use `IAsyncEnumerable<string>` for streaming file content
- [x] Add `CancellationToken` support
- [x] Update `ILogReader` interface to include async methods

**Affected files:**
- `Services/LogReader/ILogReader.cs` ✅
- `Services/LogReader/FileLogReader/FileLogReader.cs` ✅
- `Services/LogViewer/LogViewer.cs` ✅ (обновлен для использования async методов)

---

### Issue #2: Fix memory leaks in LogViewer disposal ✅
**Title:** Implement proper disposal pattern to prevent memory leaks
**Labels:** `bug`, `memory-leak`, `priority:high`
**Description:**
The `LogViewer` class doesn't properly dispose of resources, leading to potential memory leaks.

**Tasks:**
- [x] Dispose `_reader` field in `LogViewer.Dispose()`
- [x] Stop timer before disposal
- [x] Implement disposal pattern in `FileLogReader`
- [x] Add event handler cleanup in `LogViewerViewModel`
- [x] Unsubscribe from `_filter.RefreshFilter` event

**Affected files:**
- `Services/LogViewer/LogViewer.cs` ✅
- `Services/LogReader/FileLogReader/FileLogReader.cs` ✅
- `ViewModels/LogViewerVM/LogViewerViewModel.cs` ✅

---

### Issue #3: Add comprehensive error handling in LogViewer.Process() ✅
**Title:** Add try-catch in timer callback to prevent application crashes
**Labels:** `bug`, `stability`, `priority:high`
**Description:**
The `Process()` method in `LogViewer` runs in a timer callback without error handling. Any exception will crash the application.

**Tasks:**
- [x] Wrap `Process()` method content in try-catch
- [x] Add logging for caught exceptions
- [x] Implement error recovery strategy
- [x] Add aggregate error reporting for batch operations

**Affected files:**
- `Services/LogViewer/LogViewer.cs` ✅ (добавлена полная обработка ошибок с агрегацией)

---

## Архитектурные улучшения (Priority: Medium)

### Issue #4: Split ILogReader interface following ISP ✅
**Title:** Refactor ILogReader interface to follow Interface Segregation Principle
**Labels:** `refactoring`, `architecture`, `priority:medium`
**Description:**
The `ILogReader` interface violates ISP by including the `Clear()` method, which doesn't belong to a reader.

**Tasks:**
- [x] Create new `ILogSource` interface extending `ILogReader`
- [x] Move `Clear()` method to `ILogSource`
- [x] Update implementations and dependencies
- [x] Add async version: `ClearAsync()`

**Affected files:**
- `Services/LogReader/ILogReader.cs` ✅
- `Services/LogReader/ILogSource.cs` ✅ (новый файл)
- `Services/LogReader/FileLogReader/FileLogReader.cs` ✅
- `Services/LogReader/Factory/ILogReaderFactory.cs` ✅
- `Services/LogReader/FileLogReader/FileLogReaderFactory.cs` ✅
- `Services/LogViewer/LogViewer.cs` ✅

---

### Issue #5: Extract duplicate filter property logic
**Title:** Eliminate code duplication in filter properties
**Labels:** `refactoring`, `code-quality`, `priority:medium`
**Description:**
Filter properties are duplicated between `LogEntryFilter.cs` and `MainWindowViewModel.cs`.

**Tasks:**
- [ ] Create base class `FilterPropertyBase`
- [ ] Extract common property setter logic
- [ ] Consider using source generators
- [ ] Reduce 6x duplication to single implementation

**Affected files:**
- `Services/Filter/LogEntryFilter.cs`
- `ViewModels/MainWindowsVM/MainWindowViewModel.cs`

---

### Issue #6: Implement Repository pattern for log sources
**Title:** Add repository abstraction for multiple log sources
**Labels:** `enhancement`, `architecture`, `priority:medium`
**Description:**
Create abstraction layer to support different log sources (files, databases, network).

**Tasks:**
- [ ] Create `ILogRepository` interface
- [ ] Implement `FileLogRepository`
- [ ] Add factory for repository creation
- [ ] Support for multiple simultaneous sources

---

## Новые функции (Priority: Medium)

### Issue #7: Add search functionality with regex support
**Title:** Implement log search with regular expressions
**Labels:** `enhancement`, `feature`, `priority:medium`
**Description:**
Users need ability to search through logs using text and regex patterns.

**Tasks:**
- [ ] Add search UI controls
- [ ] Implement search service with regex support
- [ ] Add search highlighting in results
- [ ] Support case-sensitive/insensitive search
- [ ] Add search history

---

### Issue #8: Implement multiple export formats
**Title:** Add export functionality for CSV, JSON, and Excel
**Labels:** `enhancement`, `feature`, `priority:medium`
**Description:**
Allow users to export filtered logs in different formats.

**Tasks:**
- [ ] Create `ILogExporter` interface
- [ ] Implement CSV exporter
- [ ] Implement JSON exporter
- [ ] Implement Excel exporter (using EPPlus or similar)
- [ ] Add export dialog with format selection

---

### Issue #9: Add dark theme support
**Title:** Implement dark theme for better visibility
**Labels:** `enhancement`, `ui/ux`, `priority:medium`
**Description:**
Add dark theme option for users who prefer it or work in low-light conditions.

**Tasks:**
- [ ] Create dark theme resource dictionary
- [ ] Add theme switching mechanism
- [ ] Save theme preference
- [ ] Update all controls to support theming
- [ ] Add theme toggle in settings

---

### Issue #10: Add keyboard shortcuts
**Title:** Implement keyboard shortcuts for common actions
**Labels:** `enhancement`, `ui/ux`, `priority:medium`
**Description:**
Add keyboard shortcuts for improved productivity.

**Tasks:**
- [ ] Define shortcut mappings (Ctrl+F for search, etc.)
- [ ] Implement command bindings
- [ ] Add shortcuts to menu items
- [ ] Create shortcuts documentation
- [ ] Add customizable shortcuts in settings

---

## UI/UX улучшения (Priority: Low)

### Issue #11: Implement list virtualization
**Title:** Add virtualization for handling millions of log entries
**Labels:** `enhancement`, `performance`, `priority:low`
**Description:**
Current implementation loads all entries into memory. Need virtualization for large files.

**Tasks:**
- [ ] Replace ListView with VirtualizingStackPanel
- [ ] Implement data virtualization
- [ ] Add lazy loading for entries
- [ ] Optimize memory usage for large datasets

---

### Issue #12: Add progress indicators
**Title:** Show progress bar when loading large files
**Labels:** `enhancement`, `ui/ux`, `priority:low`
**Description:**
Users have no feedback when loading large log files.

**Tasks:**
- [ ] Add progress bar control
- [ ] Implement progress reporting in FileLogReader
- [ ] Show estimated time remaining
- [ ] Add cancel button for long operations

---

### Issue #13: Add context menu
**Title:** Implement right-click context menu for log entries
**Labels:** `enhancement`, `ui/ux`, `priority:low`
**Description:**
Add context menu for quick actions on log entries.

**Tasks:**
- [ ] Create context menu with common actions
- [ ] Add "Copy" functionality
- [ ] Add "Filter by this value" option
- [ ] Add "Go to source" for file entries
- [ ] Add "Mark as important" option

---

## Качество кода (Priority: Low)

### Issue #14: Increase test coverage
**Title:** Add comprehensive unit tests
**Labels:** `testing`, `code-quality`, `priority:low`
**Description:**
Current test coverage is minimal (only FileLogReaderTests exists).

**Tasks:**
- [ ] Add tests for LogViewer service
- [ ] Add tests for LogEntryFilter
- [ ] Add tests for ViewModels
- [ ] Add integration tests
- [ ] Set up code coverage reporting
- [ ] Aim for 80% coverage

---

### Issue #15: Set up CI/CD pipeline
**Title:** Configure GitHub Actions for automated builds
**Labels:** `devops`, `automation`, `priority:low`
**Description:**
Automate build, test, and release process.

**Tasks:**
- [ ] Create `.github/workflows/build.yml`
- [ ] Add build step for all configurations
- [ ] Add test execution step
- [ ] Add artifact publishing
- [ ] Configure automatic releases

---

### Issue #16: Add XML documentation
**Title:** Document public API with XML comments
**Labels:** `documentation`, `code-quality`, `priority:low`
**Description:**
Add comprehensive XML documentation for all public interfaces and classes.

**Tasks:**
- [ ] Document all public interfaces
- [ ] Document all public methods
- [ ] Generate documentation file
- [ ] Consider using DocFX for documentation site

---

## Оптимизация производительности (Priority: Low)

### Issue #17: Optimize log parsing with Span<T>
**Title:** Use Memory<T> and Span<T> for efficient parsing
**Labels:** `enhancement`, `performance`, `priority:low`
**Description:**
Current string parsing creates many allocations. Use Span<T> for better performance.

**Tasks:**
- [ ] Refactor ParseLogEntry to use Span<T>
- [ ] Reduce string allocations
- [ ] Benchmark improvements
- [ ] Profile memory usage

---

### Issue #18: Implement caching for filtered results
**Title:** Add caching layer for filter operations
**Labels:** `enhancement`, `performance`, `priority:low`
**Description:**
Filtering is performed on every update. Cache results for better performance.

**Tasks:**
- [ ] Implement filter result caching
- [ ] Add cache invalidation logic
- [ ] Add memory-aware cache limits
- [ ] Measure performance improvements

---

### Issue #19: Add log file indexing
**Title:** Create index for fast searching in large files
**Labels:** `enhancement`, `performance`, `priority:low`
**Description:**
Searching large files is slow. Create index for faster searches.

**Tasks:**
- [ ] Design index structure
- [ ] Implement index builder
- [ ] Add index persistence
- [ ] Use index for searches
- [ ] Add index rebuild functionality

---

## Конфигурация (Priority: Low)

### Issue #20: Add settings profiles
**Title:** Implement configuration profiles support
**Labels:** `enhancement`, `feature`, `priority:low`
**Description:**
Allow users to save and switch between different configuration profiles.

**Tasks:**
- [ ] Design profile structure
- [ ] Add profile management UI
- [ ] Implement profile save/load
- [ ] Add profile switching
- [ ] Include filters in profiles
