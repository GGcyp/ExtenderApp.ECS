using System;
using System.Diagnostics;
using ExtenderApp.ECS;

public static class Program
{
    public static void Main(string[] args)
    {
        // 支持通过环境变量或命令行参数选择要运行的基准或自定义运行
        // 示例： dotnet run --project test/ECSTest CreateSetGet
        string filterArg = args != null && args.Length > 0 ? args[0] : Environment.GetEnvironmentVariable("ECSTEST_BENCHMARKS");

        if (!string.IsNullOrEmpty(filterArg))
        {
            BenchmarksRunner.Run(new string[] { filterArg });
            return;
        }

        while (true)
        {
            Console.Clear();
            Console.WriteLine("========================================");
            Console.WriteLine("  ExtenderApp.ECS 测试套件");
            Console.WriteLine("========================================");
            Console.WriteLine();
            Console.WriteLine("请选择要运行的测试或基准:");
            Console.WriteLine("  1. Create/Add/Get 基准 (BenchmarkDotNet)");
            Console.WriteLine("  2. Transfer 基准 (BenchmarkDotNet)");
            Console.WriteLine("  3. 自定义快速运行：Create/Add/Get (CustomRunner)");
            Console.WriteLine("  4. 自定义快速运行：Transfer (CustomRunner)");
            Console.WriteLine("  5. 自定义快速运行：Multi CRUD (CustomRunner)");
            Console.WriteLine("  6. 自定义快速运行：Bulk AddEntity/RemoveAt (CustomRunner)");
            Console.WriteLine("  7. 自定义快速运行：异常行为 (CustomRunner)");
            Console.WriteLine("  8. 自定义快速运行：批量创建实体 (CustomRunner)");
            Console.WriteLine("  9. 自定义对照：超大组件影响对比 (CustomRunner)");
            Console.WriteLine("  10. 自定义快速运行：EntityComponentLookup/Operation (CustomRunner)");
            Console.WriteLine("  11. 自定义示例：RelationPair (CustomRunner)");
            Console.WriteLine("  12. 自定义快速运行：EntityQuery Build 测试 (CustomRunner)");
            Console.WriteLine("  13. 命令缓冲并发写入测试 (CustomRunner)");
            Console.WriteLine("  14. 仅 CurrentWorld 直接执行测试 (CustomRunner)");
            Console.WriteLine("  15. DestroyEntitiesForQuery 测试 (CustomRunner)");
            Console.WriteLine("  16. 共享组件增删改查测试 (CustomRunner)");
            Console.WriteLine("  17. 托管对象 UseData 存取测试 (CustomRunner)");
            Console.WriteLine("  q. 退出");
            Console.WriteLine();
            Console.Write("请输入选项 (1-17 或 q): ");

            var choice = Console.ReadLine()?.Trim();
            if (string.IsNullOrEmpty(choice)) continue;

            if (choice.Equals("q", StringComparison.OrdinalIgnoreCase)) break;

            switch (choice)
            {
                case "1":
                    BenchmarksRunner.Run(new string[] { "CreateSetGet" });
                    break;

                case "2":
                    BenchmarksRunner.Run(new string[] { "Transfer" });
                    break;

                case "3":
                    RunCustomWithPrompt(CustomRunner.RunCreateSetGet, 10000);
                    break;

                case "4":
                    RunCustomWithPrompt(CustomRunner.RunTransfer, 10000);
                    break;

                case "5":
                    RunCustomWithPrompt(CustomRunner.RunMultipleEntitiesCRUD, 2000);
                    break;

                case "6":
                    RunCustomWithPrompt(CustomRunner.RunBulkAddRemove, 1000);
                    break;

                case "7":
                    CustomRunner.RunExceptionBehavior();
                    break;

                case "8":
                    RunCustomWithPrompt(CustomRunner.RunBatchCreate, 10000);
                    break;

                case "9":
                    RunCustomWithPrompt(CustomRunner.RunHugeComponentComparison, 200000);
                    break;

                case "10":
                    RunCustomWithPrompt(CustomRunner.RunEntityComponentApiTest, 50000);
                    break;

                case "11":
                    CustomRunner.RunRelationPairCase(0);
                    break;

                case "12":
                    CustomRunner.RunEntityQueryBuildTest();
                    break;

                case "13":
                    // 询问每个写线程的操作数量
                    Console.Write("输入每个写线程的操作次数（默认 1000）：");
                    var s = Console.ReadLine();
                    int n = 1000;
                    if (!string.IsNullOrWhiteSpace(s) && int.TryParse(s.Trim(), out var parsed) && parsed > 0)
                        n = parsed;
                    CustomRunner.RunCommandBufferConcurrencyTest(n);
                    break;

                case "14":
                    Console.Write("输入每个写线程的操作次数（默认 1000）：");
                    var s2 = Console.ReadLine();
                    int n2 = 1000;
                    if (!string.IsNullOrWhiteSpace(s2) && int.TryParse(s2.Trim(), out var parsed2) && parsed2 > 0)
                        n2 = parsed2;
                    CustomRunner.RunWorldDirectExecutionTest(n2);
                    break;

                case "15":
                    CustomRunner.RunDestroyEntitiesForQueryTest();
                    break;

                case "16":
                    CustomRunner.RunSharedComponentCrudTest();
                    break;

                case "17":
                    CustomRunner.RunManagedUseDataTest();
                    break;

                default:
                    Console.WriteLine("无效选项。");
                    break;
            }

            Console.WriteLine();
            Console.WriteLine("按回车返回菜单...");
            Console.ReadLine();
        }
    }

    private static void RunCustomWithPrompt(Action<int> action, int defaultN)
    {
        Console.Write($"输入测试规模 N（默认 {defaultN}，回车使用默认）：");
        var s = Console.ReadLine();
        int n = defaultN;
        if (!string.IsNullOrWhiteSpace(s) && int.TryParse(s.Trim(), out var parsed) && parsed > 0)
            n = parsed;
        else
        {
            // 尝试读取环境变量
            var env = Environment.GetEnvironmentVariable("ECSTEST_PERF_N");
            if (!string.IsNullOrEmpty(env) && int.TryParse(env, out var envN) && envN > 0)
                n = envN;
        }

        action(n);
    }
}