@echo off
chcp 65001 >nul
setlocal enabledelayedexpansion

:: Скрипт для запуска бенчмарков nLogViewer на Windows

echo ════════════════════════════════════════════════════════
echo   nLogViewer Benchmark Runner
echo ════════════════════════════════════════════════════════
echo.

:: Проверяем наличие .NET SDK
where dotnet >nul 2>&1
if %errorlevel% neq 0 (
    echo [31m❌ Ошибка: .NET SDK не установлен[0m
    echo    Установите .NET 9.0 SDK: https://dotnet.microsoft.com/download
    pause
    exit /b 1
)

:: Проверяем версию .NET
for /f "tokens=*" %%i in ('dotnet --version') do set DOTNET_VERSION=%%i
echo [32m✓ .NET SDK версия: %DOTNET_VERSION%[0m
echo.

:: Собираем проект
echo [33m→ Сборка проекта nLogViewer.Benchmark...[0m
dotnet build nLogViewer.Benchmark\nLogViewer.Benchmark.csproj -c Release --nologo --verbosity quiet >nul 2>&1

if %errorlevel% equ 0 (
    echo [32m✓ Проект успешно собран[0m
    echo.
) else (
    echo [31m❌ Ошибка сборки проекта[0m
    echo    Запустите: dotnet build nLogViewer.Benchmark\nLogViewer.Benchmark.csproj
    pause
    exit /b 1
)

:: Запускаем бенчмарки
echo [33m→ Запуск бенчмарков...[0m
echo.

dotnet run --project nLogViewer.Benchmark\nLogViewer.Benchmark.csproj -c Release --no-build -- %*

endlocal
