#!/bin/bash

# Скрипт для запуска бенчмарков nLogViewer

echo "════════════════════════════════════════════════════════"
echo "  nLogViewer Benchmark Runner"
echo "════════════════════════════════════════════════════════"
echo ""

# Цвета для вывода
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
RED='\033[0;31m'
NC='\033[0m' # No Color

# Проверяем наличие .NET SDK
if ! command -v dotnet &> /dev/null; then
    echo -e "${RED}❌ Ошибка: .NET SDK не установлен${NC}"
    echo "   Установите .NET 9.0 SDK: https://dotnet.microsoft.com/download"
    exit 1
fi

# Проверяем версию .NET
DOTNET_VERSION=$(dotnet --version)
echo -e "${GREEN}✓ .NET SDK версия: $DOTNET_VERSION${NC}"
echo ""

# Собираем проект
echo -e "${YELLOW}→ Сборка проекта nLogViewer.Benchmark...${NC}"
dotnet build nLogViewer.Benchmark/nLogViewer.Benchmark.csproj -c Release --nologo --verbosity quiet

if [ $? -eq 0 ]; then
    echo -e "${GREEN}✓ Проект успешно собран${NC}"
    echo ""
else
    echo -e "${RED}❌ Ошибка сборки проекта${NC}"
    echo "   Запустите: dotnet build nLogViewer.Benchmark/nLogViewer.Benchmark.csproj"
    exit 1
fi

# Запускаем бенчмарки
echo -e "${YELLOW}→ Запуск бенчмарков...${NC}"
echo ""

dotnet run --project nLogViewer.Benchmark/nLogViewer.Benchmark.csproj -c Release --no-build -- "$@"
