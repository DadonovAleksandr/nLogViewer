using System.Diagnostics;
using System.Text.Json;

namespace nLogViewer.Benchmark;

class Program
{
    static async Task<int> Main(string[] args)
    {
        Console.ForegroundColor = ConsoleColor.Cyan;
        Console.WriteLine("═══════════════════════════════════════════════════════════════");
        Console.WriteLine("        nLogViewer Performance Benchmark Suite");
        Console.WriteLine("        Phase 1 Optimization Testing");
        Console.WriteLine("═══════════════════════════════════════════════════════════════");
        Console.ResetColor();
        Console.WriteLine();

        // Проверяем доступность nLogViewer.Tester
        var testerPath = GetTesterPath();
        if (!File.Exists(testerPath))
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine($"Error: nLogViewer.Tester not found at {testerPath}");
            Console.WriteLine("Please build the solution first: dotnet build");
            Console.ResetColor();
            return 1;
        }

        // Создаем директорию для результатов
        var resultsDir = Path.Combine(Directory.GetCurrentDirectory(), "benchmark-results");
        Directory.CreateDirectory(resultsDir);
        Console.WriteLine($"Results directory: {resultsDir}");
        Console.WriteLine();

        var tests = new[]
        {
            new BenchmarkTest
            {
                Name = "[1/4] Test 10k entries",
                GenerateArgs = "benchmark generate test-10k.log 10000",
                CompareArgs = "benchmark compare test-10k.log --output results-10k.json"
            },
            new BenchmarkTest
            {
                Name = "[2/4] Test 100k entries",
                GenerateArgs = "benchmark generate test-100k.log 100000",
                CompareArgs = "benchmark compare test-100k.log --output results-100k.json"
            },
            new BenchmarkTest
            {
                Name = "[3/4] Test 1M entries (this may take a while)",
                GenerateArgs = "benchmark generate test-1m.log 1000000",
                CompareArgs = "benchmark compare test-1m.log --output results-1m.json"
            },
            new BenchmarkTest
            {
                Name = "[4/4] Test multiline entries",
                GenerateArgs = "benchmark generate test-multiline.log 100000 --multiline",
                CompareArgs = "benchmark compare test-multiline.log --output results-multiline.json"
            }
        };

        int totalTests = tests.Length;
        int passedTests = 0;
        int failedTests = 0;

        foreach (var test in tests)
        {
            if (await RunTest(test, testerPath, resultsDir))
            {
                passedTests++;
            }
            else
            {
                failedTests++;
            }
        }

        // Итоговый отчет
        Console.WriteLine();
        Console.ForegroundColor = ConsoleColor.Cyan;
        Console.WriteLine("═══════════════════════════════════════════════════════════════");
        Console.WriteLine("                    BENCHMARK SUITE SUMMARY");
        Console.WriteLine("═══════════════════════════════════════════════════════════════");
        Console.ResetColor();
        Console.WriteLine();
        Console.WriteLine($"Total tests:    {totalTests}");
        Console.ForegroundColor = ConsoleColor.Green;
        Console.WriteLine($"Passed:         {passedTests}");
        Console.ResetColor();

        if (failedTests > 0)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine($"Failed:         {failedTests}");
            Console.ResetColor();
        }
        else
        {
            Console.WriteLine($"Failed:         {failedTests}");
        }
        Console.WriteLine();

        if (failedTests == 0)
        {
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine("✓ All tests completed successfully!");
            Console.ResetColor();
            Console.WriteLine();
            Console.WriteLine($"Results saved in: {resultsDir}");
            Console.WriteLine();
            Console.WriteLine("JSON files:");
            foreach (var file in Directory.GetFiles(resultsDir, "results-*.json"))
            {
                Console.WriteLine($"  - {Path.GetFileName(file)}");
            }
            Console.WriteLine();
            Console.WriteLine("Log files:");
            foreach (var file in Directory.GetFiles(resultsDir, "test-*.log"))
            {
                Console.WriteLine($"  - {Path.GetFileName(file)}");
            }
            Console.WriteLine();
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine("Next steps:");
            Console.ResetColor();
            Console.WriteLine("1. Review the results in JSON files");
            Console.WriteLine("2. Verify that Memory Used < 20 MB for all tests");
            Console.WriteLine("3. Verify that Throughput > 40,000 entries/sec");
            Console.WriteLine("4. Commit the results to git");
            Console.WriteLine();
            Console.WriteLine("Press any key to exit...");
            Console.ReadKey();
            return 0;
        }
        else
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine("✗ Some tests failed. Please review the errors above.");
            Console.ResetColor();
            Console.WriteLine();
            Console.WriteLine("Common issues:");
            Console.WriteLine("- Missing .NET SDK or WPF workload");
            Console.WriteLine("- Run: dotnet workload install wpf");
            Console.WriteLine();
            Console.WriteLine("Press any key to exit...");
            Console.ReadKey();
            return 1;
        }
    }

    static async Task<bool> RunTest(BenchmarkTest test, string testerPath, string resultsDir)
    {
        Console.WriteLine();
        Console.ForegroundColor = ConsoleColor.Yellow;
        Console.WriteLine("━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━");
        Console.WriteLine($"Running: {test.Name}");
        Console.WriteLine("━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━");
        Console.ResetColor();

        try
        {
            // Генерация файла
            if (!await RunTesterCommand(testerPath, test.GenerateArgs, resultsDir))
            {
                throw new Exception("Generate failed");
            }

            // Запуск теста
            if (!await RunTesterCommand(testerPath, test.CompareArgs, resultsDir))
            {
                throw new Exception("Compare failed");
            }

            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine($"✓ {test.Name} completed successfully");
            Console.ResetColor();
            return true;
        }
        catch (Exception ex)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine($"✗ {test.Name} failed: {ex.Message}");
            Console.ResetColor();
            return false;
        }
    }

    static async Task<bool> RunTesterCommand(string testerPath, string args, string workingDir)
    {
        Console.ForegroundColor = ConsoleColor.Gray;
        Console.WriteLine($"Executing: dotnet {testerPath} {args}");
        Console.ResetColor();

        var startInfo = new ProcessStartInfo
        {
            FileName = "dotnet",
            Arguments = $"\"{testerPath}\" {args}",
            WorkingDirectory = workingDir,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };

        using var process = new Process { StartInfo = startInfo };

        process.OutputDataReceived += (sender, e) =>
        {
            if (!string.IsNullOrEmpty(e.Data))
            {
                Console.WriteLine(e.Data);
            }
        };

        process.ErrorDataReceived += (sender, e) =>
        {
            if (!string.IsNullOrEmpty(e.Data))
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine(e.Data);
                Console.ResetColor();
            }
        };

        process.Start();
        process.BeginOutputReadLine();
        process.BeginErrorReadLine();
        await process.WaitForExitAsync();

        return process.ExitCode == 0;
    }

    static string GetTesterPath()
    {
        // Пробуем найти скомпилированный nLogViewer.Tester.dll
        var baseDir = AppContext.BaseDirectory;
        var configuration = baseDir.Contains("Debug") ? "Debug" : "Release";

        var possiblePaths = new[]
        {
            Path.Combine(baseDir, "..", "..", "..", "..", "nLogViewer.Tester", "bin", configuration, "net9.0-windows", "nLogViewer.Tester.dll"),
            Path.Combine(baseDir, "..", "..", "..", "..", "nLogViewer.Tester", "bin", "Debug", "net9.0-windows", "nLogViewer.Tester.dll"),
            Path.Combine(baseDir, "..", "..", "..", "..", "nLogViewer.Tester", "bin", "Release", "net9.0-windows", "nLogViewer.Tester.dll")
        };

        foreach (var path in possiblePaths)
        {
            var fullPath = Path.GetFullPath(path);
            if (File.Exists(fullPath))
            {
                return fullPath;
            }
        }

        // Если не нашли, возвращаем путь по умолчанию
        return Path.GetFullPath(possiblePaths[0]);
    }
}

class BenchmarkTest
{
    public string Name { get; set; } = "";
    public string GenerateArgs { get; set; } = "";
    public string CompareArgs { get; set; } = "";
}
