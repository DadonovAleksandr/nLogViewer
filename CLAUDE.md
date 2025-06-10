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
```

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

2. **Log Viewing**:
   - `ILogViewer` → Manages log entry collection and state
   - `LogViewerViewModel` → ViewModel for the log viewer UI
   - Supports real-time updates, filtering, and state management (Start/Stop/Pause/Clear)

3. **Log Entry Model**:
   - `ILogEntry` → Core log entry interface
   - `LogEntry` → Implementation with DateTime, LogEntryType, Message, Source, ProcessId, ThreadId
   - `LogEntryType` → Enum for log levels (Trace, Debug, Info, Warning, Error, Fatal)

4. **Filtering**:
   - `ILogEntryFilter` → Interface for filtering log entries
   - `LogEntryFilter` → Implementation for filtering by various criteria

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
- Real-time log file monitoring
- Multi-line log entry parsing (v1.7.0+)
- Log level filtering
- Export functionality
- Recent logs management
- Thread and process ID tracking

## Version Information
Current version: 1.7.1 (set in nLogViewer.csproj)
Version history tracked in `nLogViewer/changes.md`