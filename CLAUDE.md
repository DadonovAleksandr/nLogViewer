# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project Overview

nLogViewer is a WPF-based log viewer application for viewing and analyzing NLog files. The solution consists of three projects:
- **nLogViewer**: Main WPF application for viewing log files
- **nLogViewer.Tester**: Test application for generating log entries
- **nLogViewer.Tests**: Unit tests using NUnit framework

## Build Commands

```bash
# Build the entire solution
dotnet build

# Build in Release mode
dotnet build -c Release

# Run the main application
dotnet run --project nLogViewer/nLogViewer.csproj

# Run the tester application
dotnet run --project nLogViewer.Tester/nLogViewer.Tester.csproj

# Run tests
dotnet test nLogViewer.Tests/nLogViewer.Tests.csproj

# Run a specific test
dotnet test nLogViewer.Tests/nLogViewer.Tests.csproj --filter "FullyQualifiedName~TestName"

# Run tests with coverage
dotnet test nLogViewer.Tests/nLogViewer.Tests.csproj --collect:"XPlat Code Coverage"
```

## Technology Stack

- **Target Framework**: .NET 8.0 Windows (net8.0-windows)
- **UI Framework**: WPF (Windows Presentation Foundation)
- **Language Version**: C# 11
- **Hosting**: Microsoft.Extensions.Hosting with dependency injection
- **Logging**: NLog with extensions for Microsoft.Extensions.Logging
- **Configuration**: Config.Net for settings management
- **Testing**: NUnit with Moq for mocking, coverlet for coverage
- **Dialogs**: Ookii.Dialogs.Wpf for native Windows dialogs

## Architecture Overview

### Dependency Injection & Hosting
The application uses Microsoft.Extensions.Hosting with dependency injection. Service registration happens in two places:
- `nLogViewer/Services/ServiceRegistration.cs`: Registers core services (log readers, filters, viewers)
- `nLogViewer/ViewModels/ViewModelRegistration.cs`: Registers ViewModels

### Core Components

1. **Log Reading Pipeline**:
   - `ILogReader` → Reads log entries from sources (currently file-based)
   - `ILogReaderFactory` → Creates appropriate log readers
   - `FileLogReader` → Concrete implementation for reading log files

2. **Repository Pattern (v1.7.1+)**:
   - `ILogRepository` → Unified interface for log data sources
   - `FileLogRepository` → File-based log repository implementation
   - `CompositeLogRepository` → Supports multiple simultaneous sources
   - `LogRepositoryFactory` → Creates repositories based on source type

3. **Log Viewing & Virtualization**:
   - `ILogViewer` → Manages log entry collection and state
   - `LogViewerViewModel` → ViewModel for the log viewer UI
   - `VirtualizingLogCollection` → Handles large datasets with pagination (v1.7.1+)
   - `CircularBuffer` → Memory-bounded buffer for log entries
   - Supports real-time updates, filtering, and state management (Start/Stop/Pause/Clear)

4. **Memory Management**:
   - `MemoryConfiguration` → Configures virtualization and memory limits
   - Supports data virtualization with configurable page size and cache limits
   - Circular buffer option for memory-constrained scenarios

5. **Log Entry Model**:
   - `ILogEntry` → Core log entry interface
   - `LogEntry` → Implementation with DateTime, LogEntryType, Message, Source, ProcessId, ThreadId
   - `LogEntryType` → Enum for log levels (Trace, Debug, Info, Warning, Error, Fatal)

6. **Filtering**:
   - `ILogEntryFilter` → Interface for filtering log entries
   - `LogEntryFilter` → Implementation extending `FilterBase` for various criteria
   - `FilterBase` → Base class with common property management logic

7. **Progress & User Interaction**:
   - `IProgressReporter` & `ProgressReporter` → File loading progress tracking
   - `IWindowProgressService` → Modal progress window management
   - `IUserDialogService` → Native Windows dialog integration

### MVVM Pattern
The application follows MVVM pattern with:
- Views in `Views/` directory (XAML files)
- ViewModels in `ViewModels/` directory
- Base classes: `ViewModel` and `BaseViewModel` for common functionality
- Commands using `LambdaCommand` and `RelayCommand` implementations

### Configuration
- Application settings via `appsettings.json`
- NLog configuration via `nlog.config`
- Recent logs tracking through `IRecentLogsRepository`

### Key Features
- Real-time log file monitoring with file system watchers
- Multi-line log entry parsing (v1.7.0+)
- Data virtualization for handling large log files (v1.7.1+)
- Progress indication for large file loading (10MB+ threshold)
- Log level filtering with live updates
- Context menu support for log files (Open in Notepad/Notepad++, Show in Explorer)
- Copy log entries via Ctrl+C or context menu
- Export functionality
- Recent logs management with persistence
- Thread and process ID tracking
- Memory management with circular buffers and configurable limits

## Development Notes

- The application uses Russian comments in some areas of the codebase
- Internal visibility is configured for test assemblies via `InternalsVisibleTo`
- Progress reporting activates automatically for files larger than 10MB
- Memory configuration can be adjusted in `ServiceRegistration.cs`
- Data virtualization and circular buffers are temporarily disabled in current config for debugging

## Version Information
Current version: 1.7.1 (set in nLogViewer.csproj)
Version history tracked in `nLogViewer/changes.md`