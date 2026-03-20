using System;
using BenchmarkDotNet.Running;

public static class BenchmarksRunner
{
    public static void Run(string[] args)
    {
        var config = new QuickConfig();

        // allow enabling memory diagnoser via env var - not added dynamically here to keep API simple
        var enableMem = Environment.GetEnvironmentVariable("ECSTEST_BENCHMARK_MEM") == "1";
        if (enableMem)
        {
            Console.WriteLine("ECSTEST_BENCHMARK_MEM=1 requested, but dynamic MemoryDiagnoser addition is not supported in quick runner. Run with full config if needed.");
        }

        // args 可以包含要运行的基准类名，用逗号分隔
        var filter = args != null && args.Length > 0 ? args[0] : null;

        if (string.IsNullOrEmpty(filter))
        {
            RunBenchmarkWithLogging(typeof(CreateSetGetBenchmarks), config);
            RunBenchmarkWithLogging(typeof(TransferBenchmarks), config);
            return;
        }

        var names = filter.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        foreach (var name in names)
        {
            if (name.Equals("CreateSetGet", StringComparison.OrdinalIgnoreCase))
                RunBenchmarkWithLogging(typeof(CreateSetGetBenchmarks), config);
            else if (name.Equals("Transfer", StringComparison.OrdinalIgnoreCase))
                RunBenchmarkWithLogging(typeof(TransferBenchmarks), config);
            else
                Console.WriteLine($"未知基准：{name}");
        }
    }

    private static void RunBenchmarkWithLogging(Type benchmarkType, QuickConfig config)
    {
        Console.WriteLine($"[BenchmarksRunner] Preparing to run benchmark: {benchmarkType.Name}");
        try
        {
            var summary = BenchmarkRunner.Run(benchmarkType, config);
            Console.WriteLine($"[BenchmarksRunner] Finished benchmark: {benchmarkType.Name}");
            Console.WriteLine($"[BenchmarksRunner] BenchmarkDotNet artifacts are available in the output folder (check ./BenchmarkDotNet.Artifacts/results).");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[BenchmarksRunner] Benchmark {benchmarkType.Name} failed: {ex.Message}");
        }
    }
}
