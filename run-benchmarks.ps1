# nLogViewer Performance Benchmark Suite
# Phase 1 Optimization Testing

Write-Host "═══════════════════════════════════════════════════════════════" -ForegroundColor Cyan
Write-Host "        nLogViewer Performance Benchmark Suite" -ForegroundColor Cyan
Write-Host "        Phase 1 Optimization Testing" -ForegroundColor Cyan
Write-Host "═══════════════════════════════════════════════════════════════" -ForegroundColor Cyan
Write-Host ""

# Функция для запуска теста
function Run-Test {
    param(
        [string]$TestName,
        [string]$GenerateArgs,
        [string]$CompareArgs
    )

    Write-Host ""
    Write-Host "━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━" -ForegroundColor Yellow
    Write-Host "Running: $TestName" -ForegroundColor Yellow
    Write-Host "━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━" -ForegroundColor Yellow

    try {
        # Генерация файла
        $generateCmd = "dotnet run --project ..\nLogViewer.Tester\nLogViewer.Tester.csproj -- $GenerateArgs"
        Write-Host "Executing: $generateCmd" -ForegroundColor Gray
        Invoke-Expression $generateCmd
        if ($LASTEXITCODE -ne 0) { throw "Generate failed" }

        # Запуск теста
        $compareCmd = "dotnet run --project ..\nLogViewer.Tester\nLogViewer.Tester.csproj -- $CompareArgs"
        Write-Host "Executing: $compareCmd" -ForegroundColor Gray
        Invoke-Expression $compareCmd
        if ($LASTEXITCODE -ne 0) { throw "Compare failed" }

        Write-Host "✓ $TestName completed successfully" -ForegroundColor Green
        return $true
    }
    catch {
        Write-Host "✗ $TestName failed: $_" -ForegroundColor Red
        return $false
    }
}

# Проверяем доступность dotnet
try {
    $null = dotnet --version
}
catch {
    Write-Host "Error: dotnet CLI not found" -ForegroundColor Red
    Write-Host "Please install .NET SDK from https://dotnet.microsoft.com/download" -ForegroundColor Yellow
    exit 1
}

# Создаем директорию для результатов
Write-Host "Creating benchmark-results directory..." -ForegroundColor Yellow
New-Item -ItemType Directory -Force -Path benchmark-results | Out-Null
Set-Location benchmark-results

# Счетчики
$totalTests = 0
$passedTests = 0
$failedTests = 0

# Тест 1: 10k записей
$totalTests++
if (Run-Test -TestName "[1/4] Test 10k entries" `
             -GenerateArgs "benchmark generate test-10k.log 10000" `
             -CompareArgs "benchmark compare test-10k.log --output results-10k.json") {
    $passedTests++
} else {
    $failedTests++
}

# Тест 2: 100k записей
$totalTests++
if (Run-Test -TestName "[2/4] Test 100k entries" `
             -GenerateArgs "benchmark generate test-100k.log 100000" `
             -CompareArgs "benchmark compare test-100k.log --output results-100k.json") {
    $passedTests++
} else {
    $failedTests++
}

# Тест 3: 1M записей
$totalTests++
if (Run-Test -TestName "[3/4] Test 1M entries (this may take a while)" `
             -GenerateArgs "benchmark generate test-1m.log 1000000" `
             -CompareArgs "benchmark compare test-1m.log --output results-1m.json") {
    $passedTests++
} else {
    $failedTests++
}

# Тест 4: Многострочные
$totalTests++
if (Run-Test -TestName "[4/4] Test multiline entries" `
             -GenerateArgs "benchmark generate test-multiline.log 100000 --multiline" `
             -CompareArgs "benchmark compare test-multiline.log --output results-multiline.json") {
    $passedTests++
} else {
    $failedTests++
}

# Итоговый отчет
Write-Host ""
Write-Host "═══════════════════════════════════════════════════════════════" -ForegroundColor Cyan
Write-Host "                    BENCHMARK SUITE SUMMARY" -ForegroundColor Cyan
Write-Host "═══════════════════════════════════════════════════════════════" -ForegroundColor Cyan
Write-Host ""
Write-Host "Total tests:    $totalTests"
Write-Host "Passed:         $passedTests" -ForegroundColor Green
if ($failedTests -gt 0) {
    Write-Host "Failed:         $failedTests" -ForegroundColor Red
} else {
    Write-Host "Failed:         $failedTests"
}
Write-Host ""

if ($failedTests -eq 0) {
    Write-Host "✓ All tests completed successfully!" -ForegroundColor Green
    Write-Host ""
    Write-Host "Results saved in: $(Get-Location)"
    Write-Host ""
    Write-Host "JSON files:"
    Get-ChildItem -Filter "results-*.json" | ForEach-Object { Write-Host "  - $($_.Name)" }
    Write-Host ""
    Write-Host "Log files:"
    Get-ChildItem -Filter "test-*.log" | ForEach-Object { Write-Host "  - $($_.Name)" }
    Write-Host ""
    Write-Host "Next steps:" -ForegroundColor Yellow
    Write-Host "1. Review the results in JSON files"
    Write-Host "2. Verify that Memory Used < 20 MB for all tests"
    Write-Host "3. Verify that Throughput > 40,000 entries/sec"
    Write-Host "4. Commit the results to git"
    exit 0
} else {
    Write-Host "✗ Some tests failed. Please review the errors above." -ForegroundColor Red
    Write-Host ""
    Write-Host "Common issues:"
    Write-Host "- Missing .NET SDK or WPF workload"
    Write-Host "- Run: dotnet workload install wpf"
    exit 1
}
