using System;
using System.Diagnostics;
using ECSTest.WorldTests;

public static class Program
{
    public static void Main(string[] args)
    {
        if (args != null && args.Length > 0 &&
            string.Equals(args[0], "querytest", StringComparison.OrdinalIgnoreCase))
        {
            CustomRunner.RunEntityQueryBuildTest();
            return;
        }

        if (args != null && args.Length > 0 &&
            string.Equals(args[0], "worldtests", StringComparison.OrdinalIgnoreCase))
        {
            ApplyMultiComponentCountFromArgs(args, argIndex: 1);
            WorldTests.RunAll();
            return;
        }

        // 支持通过环境变量或命令行参数选择要运行的基准或自定义运行 示例： dotnet run --project test/ECSTest CreateSetGet
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
            Console.WriteLine("  18. DestroyEntities 测试 (CustomRunner)");
            Console.WriteLine("  19. World 全量测试 (WorldTests.RunAll，可输入多组件实体数 N，并行段会打印入队作业数)");
            Console.WriteLine("  q. 退出");
            Console.WriteLine();
            Console.Write("请输入选项 (1-19 或 q): ");

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

                case "18":
                    CustomRunner.RunDestroyEntitiesTest();
                    break;

                case "19":
                    Console.Write(
                        $"多组件遍历实体数 N（回车保留当前 {WorldTests.MultiComponentStressEntityCount}，或读 ECSTEST_MULTI_COMPONENT_N；封顶 {WorldTests.MultiComponentEntityCountHardCap}）：");
                    var s19 = Console.ReadLine();
                    if (!string.IsNullOrWhiteSpace(s19) && int.TryParse(s19.Trim(), out var p19) && p19 > 0)
                        WorldTests.MultiComponentStressEntityCount = Math.Min(p19, WorldTests.MultiComponentEntityCountHardCap);
                    else
                    {
                        var envN = Environment.GetEnvironmentVariable("ECSTEST_MULTI_COMPONENT_N");
                        if (!string.IsNullOrWhiteSpace(envN) && int.TryParse(envN.Trim(), out var e19) && e19 > 0)
                            WorldTests.MultiComponentStressEntityCount = Math.Min(e19, WorldTests.MultiComponentEntityCountHardCap);
                    }

                    WorldTests.RunAll();
                    Console.WriteLine("WorldTests.RunAll 已完成。");
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

    /// <summary>
    /// 解析多组件测试规模：优先 <paramref name="args"/>[argIndex]，否则环境变量 ECSTEST_MULTI_COMPONENT_N；写入 <see cref="WorldTests.MultiComponentStressEntityCount"/>。
    /// </summary>
    private static void ApplyMultiComponentCountFromArgs(string[] args, int argIndex)
    {
        if (args != null && args.Length > argIndex && int.TryParse(args[argIndex].Trim(), out var parsed) && parsed > 0)
        {
            WorldTests.MultiComponentStressEntityCount = Math.Min(parsed, WorldTests.MultiComponentEntityCountHardCap);
            return;
        }

        var env = Environment.GetEnvironmentVariable("ECSTEST_MULTI_COMPONENT_N");
        if (!string.IsNullOrWhiteSpace(env) && int.TryParse(env.Trim(), out var en) && en > 0)
            WorldTests.MultiComponentStressEntityCount = Math.Min(en, WorldTests.MultiComponentEntityCountHardCap);
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