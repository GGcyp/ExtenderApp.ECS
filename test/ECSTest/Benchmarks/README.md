BenchmarkDotNet 基准说明

文件列表:
- CreateSetGetBenchmarks.cs: Create/Set/Get 三个基准
- TransferBenchmarks.cs: 组件转移基准
- BenchmarksRunner.cs: 辅助运行器，可通过命令行参数或环境变量 ECSTEST_BENCHMARKS 指定要运行的基准

用法示例:
- 通过命令行运行某个基准：
  dotnet run --project test/ECSTest CreateSetGet

- 通过环境变量运行多个基准：
  ECSTEST_BENCHMARKS=CreateSetGet,Transfer dotnet run --project test/ECSTest

- 默认在交互式模式下选择要运行的基准。

备注：BenchmarkDotNet 会生成详细报告并在第一次运行时做多次迭代与 warmup，以获得可靠数据。