using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Diagnosers;
using BenchmarkDotNet.Running;
using BenchmarkDotNet.Columns;
using BenchmarkDotNet.Exporters;
using BenchmarkDotNet.Jobs;

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

        AddExporter(MarkdownExporter.GitHub);
        AddColumnProvider(DefaultColumnProviders.Instance);

        // Do not add memory diagnoser by default (can be enabled via env variable)
    }
}
