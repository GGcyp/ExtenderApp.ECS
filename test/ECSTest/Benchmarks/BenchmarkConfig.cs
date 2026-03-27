using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Columns;
using BenchmarkDotNet.Exporters;
using BenchmarkDotNet.Jobs;
using BenchmarkDotNet.Loggers;

public class QuickConfig : ManualConfig
{
    public QuickConfig()
    {
        // Minimal job: single launch, few warmup & target iterations
        AddJob(Job.Default
            .WithLaunchCount(1)
            .WithWarmupCount(1)
            .WithIterationCount(3)
        );

        AddLogger(ConsoleLogger.Default);
        AddExporter(MarkdownExporter.GitHub);
        AddColumnProvider(DefaultColumnProviders.Instance);
    }
}
