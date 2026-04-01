using System.Diagnostics;
using ECSTest.Components;
using ECSTest.Tests;
using ExtenderApp.ECS;
using ExtenderApp.ECS.Queries;

namespace ECSTest.CustomRuns;

/// <summary>
/// 命令缓冲并发、CurrentWorld 直调、按查询销毁与共享组件。
/// </summary>
public static partial class CustomRunner
{
    /// <summary>
    /// 多线程向同一 <see cref="World.CommandBuffer"/> 写入命令后的回放验证。
    /// </summary>
    public static void RunCommandBufferConcurrencyTest(int opsPerWriter)
    {
        Console.WriteLine($"=== CommandBuffer Concurrency Test: writers=8, opsPerWriter={opsPerWriter} ===");
        var sw = Stopwatch.StartNew();
        CustomRunnerCommandsVerification.VerifyCommandBufferConcurrency(opsPerWriter);
        sw.Stop();
        Console.WriteLine($"Verification OK, elapsed={sw.ElapsedMilliseconds} ms");
        Console.WriteLine("========================================");
    }

    /// <summary>
    /// 不经命令缓冲、直接在当前 World 上执行大量组件操作的冒烟测试。
    /// </summary>
    public static void RunWorldDirectExecutionTest(int opsPerWriter)
    {
        Console.WriteLine($"=== CurrentWorld Direct Execution opsPerWriter={opsPerWriter} ===");
        var sw = Stopwatch.StartNew();
        CustomRunnerCommandsVerification.VerifyWorldDirectExecution(opsPerWriter);
        sw.Stop();
        Console.WriteLine($"Verification OK, elapsed={sw.ElapsedMilliseconds} ms");
        Console.WriteLine("========================================");
    }

    /// <summary>
    /// <see cref="World.DestroyEntitiesForQuery"/> 与隐式 <see cref="EntityQuery"/> 转换。
    /// </summary>
    public static void RunDestroyEntitiesForQueryTest()
    {
        Console.WriteLine("=== CustomRunner: DestroyEntitiesForQuery Test ===");
        CustomRunnerCommandsVerification.VerifyDestroyEntitiesForQuery();
        Console.WriteLine("DestroyEntitiesForQuery: OK");
        Console.WriteLine("========================================");
    }

    /// <summary>
    /// 共享组件的增删改查路径。
    /// </summary>
    public static void RunSharedComponentCrudTest()
    {
        Console.WriteLine("=== CustomRunner: SharedComponent CRUD Test ===");
        var sw = Stopwatch.StartNew();
        CustomRunnerCommandsVerification.VerifySharedComponentCrud();
        sw.Stop();
        Console.WriteLine($"Elapsed: {sw.ElapsedMilliseconds} ms");
        Console.WriteLine("========================================");
    }

    /// <summary>
    /// 托管引用类型组件的创建与查询（性能由使用方评估）。
    /// </summary>
    public static void RunManagedUseDataTest()
    {
        Console.WriteLine("=== CustomRunner: Managed UseData Test ===");
        var sw = Stopwatch.StartNew();
        CustomRunnerCommandsVerification.VerifyManagedUseData();
        sw.Stop();
        Console.WriteLine($"OK, elapsed={sw.ElapsedMilliseconds} ms");
        Console.WriteLine("========================================");
    }

    /// <summary>
    /// 批量创建带掩码实体后逐条销毁的耗时输出。
    /// </summary>
    public static void RunDestroyEntitiesTest()
    {
        Console.WriteLine("=== CustomRunner: DestroyEntities batch ===");
        var sw = Stopwatch.StartNew();
        CustomRunnerCommandsVerification.VerifyDestroyEntitiesBatch();
        sw.Stop();
        Console.WriteLine($"OK, elapsed={sw.ElapsedMilliseconds} ms");
        Console.WriteLine("========================================");
    }

    /// <summary>
    /// 在 ECSTest.App 中执行托管堆（class 引用类型）组件测试。
    /// 说明：ECSTest.App 不会自动枚举 xUnit，用该入口可在菜单里直跑并打印每个用例的通过/失败与异常信息。
    /// </summary>
    public static void RunManagedHeapClassComponentTests()
    {
        Console.WriteLine("=== CustomRunner: ManagedHeap Class Component Tests ===");

        var suite = new ManagedHeapClassComponentTests();

        int passed = 0;
        int failed = 0;

        Run(nameof(ManagedHeapClassComponentTests.ManagedClassComponentCreateAndGetKeepsSameReference),
            suite.ManagedClassComponentCreateAndGetKeepsSameReference);

        Run(nameof(ManagedHeapClassComponentTests.ManagedClassComponentSetComponentReplacesReference),
            suite.ManagedClassComponentSetComponentReplacesReference);

        Run(nameof(ManagedHeapClassComponentTests.ManagedClassComponentQueryAllowsReplacingReference),
            suite.ManagedClassComponentQueryAllowsReplacingReference);

        Run(nameof(ManagedHeapClassComponentTests.ManagedClassComponentWithStructArchetypeWorksCorrectly),
            suite.ManagedClassComponentWithStructArchetypeWorksCorrectly);

        Console.WriteLine($"Summary: passed={passed}, failed={failed}");
        Console.WriteLine("========================================");
        return;

        void Run(string name, Action test)
        {
            Console.WriteLine();
            Console.WriteLine($"--- {name} ---");
            try
            {
                test();
                passed++;
                Console.WriteLine($"Result: PASS ({name})");
            }
            catch (Exception ex)
            {
                failed++;
                Console.WriteLine($"Result: FAIL ({name})");
                Console.WriteLine(ex);
            }
        }
    }

    /// <summary>
    /// 在 ECSTest.App 中执行组件增删写入影响验证测试。
    /// 说明：ECSTest.App 不会自动枚举 xUnit，用该入口可在菜单里直跑并打印每个用例的通过/失败与异常信息。
    /// </summary>
    public static void RunComponentMutationTests()
    {
        Console.WriteLine("=== CustomRunner: Component Mutation Tests ===");

        // 组件增删测试用例实例。
        var suite = new ComponentMutationTests();

        // 通过用例计数。
        int passed = 0;
        // 失败用例计数。
        int failed = 0;

        RunTest(nameof(ComponentMutationTests.World_AddRemoveComponent_DoesNotAffectExistingValues),
            suite.World_AddRemoveComponent_DoesNotAffectExistingValues);

        Console.WriteLine($"Summary: passed={passed}, failed={failed}");
        Console.WriteLine("========================================");
        return;

        /// <summary>
        /// 执行测试并输出结果。
        /// </summary>
        void RunTest(string name, Action test)
        {
            Console.WriteLine();
            Console.WriteLine($"--- {name} ---");
            try
            {
                test();
                passed++;
                Console.WriteLine($"Result: PASS ({name})");
            }
            catch (Exception ex)
            {
                failed++;
                Console.WriteLine($"Result: FAIL ({name})");
                Console.WriteLine(ex);
            }
        }
    }
}
