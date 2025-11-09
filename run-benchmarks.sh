#!/bin/bash

echo "═══════════════════════════════════════════════════════════════"
echo "        nLogViewer Performance Benchmark Suite"
echo "        Phase 1 Optimization Testing"
echo "═══════════════════════════════════════════════════════════════"
echo ""

# Цвета для вывода
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
RED='\033[0;31m'
NC='\033[0m' # No Color

# Создаем директорию для результатов
echo -e "${YELLOW}Creating benchmark-results directory...${NC}"
mkdir -p benchmark-results
cd benchmark-results || exit

# Функция для запуска теста с обработкой ошибок
run_test() {
    local test_name=$1
    local command=$2

    echo ""
    echo -e "${YELLOW}━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━${NC}"
    echo -e "${YELLOW}Running: $test_name${NC}"
    echo -e "${YELLOW}━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━${NC}"

    if eval "$command"; then
        echo -e "${GREEN}✓ $test_name completed successfully${NC}"
        return 0
    else
        echo -e "${RED}✗ $test_name failed${NC}"
        return 1
    fi
}

# Проверяем доступность dotnet
if ! command -v dotnet &> /dev/null; then
    echo -e "${RED}Error: dotnet CLI not found${NC}"
    echo "Please install .NET SDK from https://dotnet.microsoft.com/download"
    exit 1
fi

# Счетчики успешных/неудачных тестов
total_tests=0
passed_tests=0
failed_tests=0

# Тест 1: 10k записей (quick baseline)
total_tests=$((total_tests + 1))
if run_test "[1/4] Test 10k entries" \
    "dotnet run --project ../nLogViewer.Tester/nLogViewer.Tester.csproj -- benchmark generate test-10k.log 10000 && \
     dotnet run --project ../nLogViewer.Tester/nLogViewer.Tester.csproj -- benchmark compare test-10k.log --output results-10k.json"; then
    passed_tests=$((passed_tests + 1))
else
    failed_tests=$((failed_tests + 1))
fi

# Тест 2: 100k записей (standard test)
total_tests=$((total_tests + 1))
if run_test "[2/4] Test 100k entries" \
    "dotnet run --project ../nLogViewer.Tester/nLogViewer.Tester.csproj -- benchmark generate test-100k.log 100000 && \
     dotnet run --project ../nLogViewer.Tester/nLogViewer.Tester.csproj -- benchmark compare test-100k.log --output results-100k.json"; then
    passed_tests=$((passed_tests + 1))
else
    failed_tests=$((failed_tests + 1))
fi

# Тест 3: 1M записей (stress test)
total_tests=$((total_tests + 1))
if run_test "[3/4] Test 1M entries (this may take a while)" \
    "dotnet run --project ../nLogViewer.Tester/nLogViewer.Tester.csproj -- benchmark generate test-1m.log 1000000 && \
     dotnet run --project ../nLogViewer.Tester/nLogViewer.Tester.csproj -- benchmark compare test-1m.log --output results-1m.json"; then
    passed_tests=$((passed_tests + 1))
else
    failed_tests=$((failed_tests + 1))
fi

# Тест 4: Многострочные записи
total_tests=$((total_tests + 1))
if run_test "[4/4] Test multiline entries" \
    "dotnet run --project ../nLogViewer.Tester/nLogViewer.Tester.csproj -- benchmark generate test-multiline.log 100000 --multiline && \
     dotnet run --project ../nLogViewer.Tester/nLogViewer.Tester.csproj -- benchmark compare test-multiline.log --output results-multiline.json"; then
    passed_tests=$((passed_tests + 1))
else
    failed_tests=$((failed_tests + 1))
fi

# Итоговый отчет
echo ""
echo -e "${YELLOW}═══════════════════════════════════════════════════════════════${NC}"
echo -e "${YELLOW}                    BENCHMARK SUITE SUMMARY${NC}"
echo -e "${YELLOW}═══════════════════════════════════════════════════════════════${NC}"
echo ""
echo -e "Total tests:    $total_tests"
echo -e "${GREEN}Passed:         $passed_tests${NC}"
if [ $failed_tests -gt 0 ]; then
    echo -e "${RED}Failed:         $failed_tests${NC}"
else
    echo -e "Failed:         $failed_tests"
fi
echo ""

if [ $failed_tests -eq 0 ]; then
    echo -e "${GREEN}✓ All tests completed successfully!${NC}"
    echo ""
    echo "Results saved in: $(pwd)"
    echo ""
    echo "JSON files:"
    ls -1 results-*.json 2>/dev/null | sed 's/^/  - /'
    echo ""
    echo "Log files:"
    ls -1 test-*.log 2>/dev/null | sed 's/^/  - /'
    echo ""
    echo -e "${YELLOW}Next steps:${NC}"
    echo "1. Review the results in JSON files"
    echo "2. Verify that Memory Used < 20 MB for all tests"
    echo "3. Verify that Throughput > 40,000 entries/sec"
    echo "4. Commit the results to git"
    exit 0
else
    echo -e "${RED}✗ Some tests failed. Please review the errors above.${NC}"
    echo ""
    echo "Common issues:"
    echo "- On Linux: WPF project cannot be compiled (expected)"
    echo "- Solution: Run tests on Windows machine"
    echo "- Or: Use pre-built binaries if available"
    exit 1
fi
