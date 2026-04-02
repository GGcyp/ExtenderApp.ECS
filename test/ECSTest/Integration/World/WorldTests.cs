using System.Diagnostics;
using ECSTest.Components;
using ExtenderApp.ECS;
using ExtenderApp.ECS.Accessors;
using Xunit;

namespace ECSTest.WorldTests;

/// <summary>
/// World 相关的独立测试集合（供 CI 或手动调用）。
/// </summary>
public static class WorldTests
{
    public static bool EnableConsoleMetrics { get; set; } = true;

    /// <summary>
    /// 多组件直接 Query / 主线程 / 并行测试共用的实体数量。默认 2；可通过环境变量 <c>ECSTEST_MULTI_COMPONENT_N</c>、命令行 <c>worldtests N</c> 或交互菜单设置。
    /// </summary>
    public static int MultiComponentStressEntityCount { get; set; } = 2;

    /// <summary>多组件测试实体数上限（防 OOM）。</summary>
    public const int MultiComponentEntityCountHardCap = 500_000;

    private static void MetricLine(string line)
    {
        if (EnableConsoleMetrics)
            Console.WriteLine(line);
    }

    private static void RunTimed(string name, Action action)
    {
        var sw = Stopwatch.StartNew();
        try
        {
            action();
        }
        finally
        {
            sw.Stop();
            if (EnableConsoleMetrics)
                Console.WriteLine($"[{name}] 用时: {sw.Elapsed.TotalMilliseconds:F3} ms");
        }
    }

    /// <summary>
    /// 与业务循环一致：先初始化再启动，再调用 Update / FixedUpdate。
    /// </summary>
    private static void InitializeAndStart(World world)
    {
        world.InitializeSystems();
        world.StartSystems();
    }

    /// <summary>
    /// 高实体压力测试规模：环境变量 <c>ECSTEST_HIGH_ENTITY_N</c> 可覆盖（正整数）；过大时封顶以防 OOM。
    /// </summary>
    private static int GetHighEntityCount()
    {
        const int defaultN = 25_000;
        const int cap = 2_000_000;
        var raw = Environment.GetEnvironmentVariable("ECSTEST_HIGH_ENTITY_N");
        if (string.IsNullOrWhiteSpace(raw) || !int.TryParse(raw.Trim(), out var n) || n <= 0)
            return defaultN;

        return Math.Min(n, cap);
    }

    /// <summary>
    /// 多组件测试实体数（正整数，封顶 <see cref="MultiComponentEntityCountHardCap"/>）。
    /// </summary>
    public static int GetMultiComponentEntityCount()
    {
        int n = MultiComponentStressEntityCount;
        if (n < 1)
            n = 1;
        return Math.Min(n, MultiComponentEntityCountHardCap);
    }

    /// <summary>并行路径结果为 float + 缩放整数，大 N 时略放宽；主线程与直接 Query 用 double，容差见 <see cref="MainThreadMultiComponentSumTolerance"/>。</summary>
    private static double MultiComponentParallelSumTolerance()
    {
        int n = GetMultiComponentEntityCount();
        return Math.Max(MultiComponentMetrics.FloatTolerance, n * 2e-4);
    }

    private const double MainThreadMultiComponentSumTolerance = 1e-3;

    public static void RunAll()
    {
        var total = Stopwatch.StartNew();
        MetricLine("======== WorldTests ========");
        MetricLine(
            $"  多组件测试 N={GetMultiComponentEntityCount()}（ECSTEST_MULTI_COMPONENT_N / worldtests N；封顶 {MultiComponentEntityCountHardCap}）；");

        RunTimed(nameof(TestWorldUpdate), TestWorldUpdate);
        RunTimed(nameof(TestWorldFixedUpdate), TestWorldFixedUpdate);
        RunTimed(nameof(TestSystemLifecycle), TestSystemLifecycle);
        RunTimed(nameof(TestSystemLifecycleBootstrapFromFixedUpdate), TestSystemLifecycleBootstrapFromFixedUpdate);
        RunTimed(nameof(TestLateRegisteredSystemGetsLifecycle), TestLateRegisteredSystemGetsLifecycle);
        RunTimed(nameof(TestUnknownSystemGroupThrows), TestUnknownSystemGroupThrows);
        RunTimed(nameof(TestWorldLightweightOptions), TestWorldLightweightOptions);
        RunTimed(nameof(TestCustomSystemGroup), TestCustomSystemGroup);
        RunTimed(nameof(TestEntityQueryIteration), TestEntityQueryIteration);
        RunTimed(nameof(TestEntityQueryIterationMainThreadSystem), TestEntityQueryIterationMainThreadSystem);
        RunTimed(nameof(TestEntityQueryIterationParallelSystem), TestEntityQueryIterationParallelSystem);
        RunTimed(nameof(TestMultiComponentEntityQueryDirect), TestMultiComponentEntityQueryDirect);
        RunTimed(nameof(TestMultiComponentIterationMainThread), TestMultiComponentIterationMainThread);
        RunTimed(nameof(TestMultiComponentIterationParallel), TestMultiComponentIterationParallel);
        RunTimed(nameof(TestHighEntityMultiSystemPipeline), TestHighEntityMultiSystemPipeline);

        total.Stop();
        MetricLine($"-------- 合计用时: {total.Elapsed.TotalMilliseconds:F3} ms --------");
    }

    public static void TestWorldUpdate()
    {
        const int frames = 10;
        const float dt = 1.0f / 60f;

        using var world = new World("TestWorld_Update");
        InitializeAndStart(world);

        for (int i = 0; i < frames; i++)
            world.Update(dt);

        MetricLine($"  Update 次数: {frames}, 单帧 delta: {dt:G9} s, 累计模拟时间: {frames * dt:G9} s");
    }

    public static void TestWorldFixedUpdate()
    {
        const int steps = 5;
        const float fixedDt = 0.02f;

        using var world = new World("TestWorld_FixedUpdate");
        InitializeAndStart(world);

        for (int i = 0; i < steps; i++)
            world.FixedUpdate(fixedDt);

        MetricLine($"  FixedUpdate 次数: {steps}, 步长: {fixedDt:G9} s, 累计模拟时间: {steps * fixedDt:G9} s");
    }

    public static void TestSystemLifecycle()
    {
        LifecycleTestSystem.Reset();

        using (var world = new World("TestWorld_SystemLifecycle"))
        {
            world.AddDefaultFrameSystem<LifecycleTestSystem>();
            InitializeAndStart(world);

            Assert.Equal(1, LifecycleTestSystem.CreateCount);
            Assert.Equal(1, LifecycleTestSystem.StartCount);
            Assert.Equal(0, LifecycleTestSystem.UpdateCount);

            world.Update(1.0f / 60f);
            Assert.Equal(1, LifecycleTestSystem.UpdateCount);

            world.Update(1.0f / 60f);
            Assert.Equal(2, LifecycleTestSystem.UpdateCount);
            Assert.Equal(1, LifecycleTestSystem.CreateCount);
            Assert.Equal(1, LifecycleTestSystem.StartCount);
        }

        Assert.Equal(1, LifecycleTestSystem.StopCount);
        Assert.Equal(1, LifecycleTestSystem.DestroyCount);

        MetricLine(
            $"  生命周期计数 — OnCreate: {LifecycleTestSystem.CreateCount}, OnStart: {LifecycleTestSystem.StartCount}, " +
            $"OnUpdate: {LifecycleTestSystem.UpdateCount}, OnStop: {LifecycleTestSystem.StopCount}, OnDestroy: {LifecycleTestSystem.DestroyCount}");
    }

    public static void TestSystemLifecycleBootstrapFromFixedUpdate()
    {
        LifecycleTestSystem.Reset();

        using (var world = new World("TestWorld_BootstrapFixed"))
        {
            world.AddRenderingFrameSystem<LifecycleTestSystem>();
            InitializeAndStart(world);
            world.FixedUpdate(0.02f);

            Assert.Equal(1, LifecycleTestSystem.CreateCount);
            Assert.Equal(1, LifecycleTestSystem.StartCount);
        }

        Assert.Equal(1, LifecycleTestSystem.StopCount);
        Assert.Equal(1, LifecycleTestSystem.DestroyCount);

        MetricLine(
            $"  FixedUpdate 引导后计数 — OnCreate: {LifecycleTestSystem.CreateCount}, OnStart: {LifecycleTestSystem.StartCount}, " +
            $"OnStop: {LifecycleTestSystem.StopCount}, OnDestroy: {LifecycleTestSystem.DestroyCount}");
    }

    public static void TestLateRegisteredSystemGetsLifecycle()
    {
        LifecycleTestSystem.Reset();

        using var world = new World("TestWorld_LateSystem");
        InitializeAndStart(world);
        world.Update(1.0f / 60f);
        Assert.Equal(0, LifecycleTestSystem.CreateCount);

        world.AddDefaultFrameSystem<LifecycleTestSystem>();
        Assert.Equal(1, LifecycleTestSystem.CreateCount);
        Assert.Equal(1, LifecycleTestSystem.StartCount);

        world.Update(1.0f / 60f);
        Assert.Equal(1, LifecycleTestSystem.UpdateCount);

        MetricLine(
            $"  延迟注册后 — OnCreate: {LifecycleTestSystem.CreateCount}, OnStart: {LifecycleTestSystem.StartCount}, OnUpdate: {LifecycleTestSystem.UpdateCount}");
    }

    public static void TestUnknownSystemGroupThrows()
    {
        using var world = new World("TestWorld_BadGroup");
        var ex = Assert.Throws<ArgumentException>(() =>
            world.AddFrameSystemToCustomGroup<LifecycleTestSystem>("NoSuchGroup"));
        MetricLine($"  已捕获 ArgumentException（组名错误）: {ex.ParamName ?? "(null)"}");
    }

    public static void TestWorldLightweightOptions()
    {
        using var world = new World("Test_Lightweight", WorldOptions.Lightweight);
        Assert.Equal(WorldParallelJobsMode.Disabled, world.Options.ParallelJobs);

        InitializeAndStart(world);
        world.Update(1f / 60f);
        MetricLine($"  WorldOptions.Lightweight: ParallelJobs={world.Options.ParallelJobs}");
    }

    /// <summary>
    /// 自定义组仅通过 AddCustomSystemGroup + AddFrameSystemToCustomGroup 使用。
    /// </summary>
    public static void TestCustomSystemGroup()
    {
        LifecycleTestSystem.Reset();
        using var world = new World("TestWorld_CustomGroup");
        world.AddCustomSystemGroup("Physics");
        world.AddFrameSystemToCustomGroup<LifecycleTestSystem>("Physics");
        InitializeAndStart(world);
        world.Update(1f / 60f);
        Assert.Equal(1, LifecycleTestSystem.UpdateCount);
        MetricLine($"  自定义组 Physics 上 OnUpdate 次数: {LifecycleTestSystem.UpdateCount}");
    }

    /// <summary>
    /// 在测试代码中直接 foreach Query&lt;Position&gt;：实体数为 2，X 之和为 11；控制台打印期望与遍历结果（与断言一致）。
    /// </summary>
    public static void TestEntityQueryIteration()
    {
        using var world = new World("TestWorld_QueryIteration");
        world.CreateEntity(new Position { X = 1f, Y = 2f });
        world.CreateEntity(new Position { X = 10f, Y = 20f });
        InitializeAndStart(world);

        var query = world.Query<Position>();
        Assert.Equal(QueryIterationMetrics.ExpectedEntityCount, query.Count);

        float sumX = 0f;
        foreach (var row in query)
        {
            row.Deconstruct(out RefRW<Position> pos);
            sumX += pos.Value.X;
        }

        Assert.True(
            Math.Abs(QueryIterationMetrics.ExpectedSumX - sumX) <= QueryIterationMetrics.FloatTolerance,
            $"直接 foreach 累加 Position.X：期望 {QueryIterationMetrics.ExpectedSumX}，实际 {sumX}");

        MetricLine(
            $"  直接 foreach — 期望 EntityCount={QueryIterationMetrics.ExpectedEntityCount}, 遍历得到 EntityCount={query.Count}; " +
            $"期望 SumX={QueryIterationMetrics.ExpectedSumX}, 遍历得到 SumX={sumX}");
    }

    /// <summary>
    /// 通过主线程 <see cref="ISystem" />（ <see cref="MainThreadPositionQuerySystem" />）遍历 Query&lt;Position&gt;：期望 2 个实体、X 之和 11。
    /// </summary>
    public static void TestEntityQueryIterationMainThreadSystem()
    {
        QueryIterationMetrics.ResetMainThreadOnly();

        using var world = new World("TestWorld_QueryIteration_MainThreadSystem");
        world.CreateEntity(new Position { X = 1f, Y = 2f });
        world.CreateEntity(new Position { X = 10f, Y = 20f });
        world.AddDefaultFrameSystem<MainThreadPositionQuerySystem>();
        InitializeAndStart(world);

        world.Update(1f / 60f);

        Assert.Equal(QueryIterationMetrics.ExpectedEntityCount, QueryIterationMetrics.MainThreadCount);

        Assert.True(
            Math.Abs(QueryIterationMetrics.ExpectedSumX - QueryIterationMetrics.MainThreadSumX) <= QueryIterationMetrics.FloatTolerance,
            $"主线程 ISystem 累加 Position.X：期望 {QueryIterationMetrics.ExpectedSumX}，实际 {QueryIterationMetrics.MainThreadSumX}");

        MetricLine(
            $"  主线程 ISystem — 期望 EntityCount={QueryIterationMetrics.ExpectedEntityCount}, 遍历得到 EntityCount={QueryIterationMetrics.MainThreadCount}; " +
            $"期望 SumX={QueryIterationMetrics.ExpectedSumX}, 遍历得到 SumX={QueryIterationMetrics.MainThreadSumX}");
    }

    /// <summary>
    /// 通过 <see cref="ScheduleParallelPositionQuerySystem" /> 调度 <see cref="IParallelSystem{Position}" /> 遍历：期望 2 个实体、X 之和 11（帧末等待并行作业完成后断言）。
    /// </summary>
    public static void TestEntityQueryIterationParallelSystem()
    {
        QueryIterationMetrics.ResetParallelOnly();

        using var world = new World("TestWorld_QueryIteration_ParallelSystem", WorldOptions.Default);
        world.CreateEntity(new Position { X = 1f, Y = 2f });
        world.CreateEntity(new Position { X = 10f, Y = 20f });
        world.AddDefaultFrameSystem<ScheduleParallelPositionQuerySystem>();
        InitializeAndStart(world);

        world.Update(1f / 60f);

        Assert.Equal(QueryIterationMetrics.ExpectedEntityCount, QueryIterationMetrics.ParallelCount);

        Assert.True(
            Math.Abs(QueryIterationMetrics.ExpectedSumX - QueryIterationMetrics.ParallelSumX) <= QueryIterationMetrics.FloatTolerance,
            $"并行累加 Position.X：期望 {QueryIterationMetrics.ExpectedSumX}，实际 {QueryIterationMetrics.ParallelSumX}");

        MetricLine(
            $"  并行 IParallelSystem — 期望 EntityCount={QueryIterationMetrics.ExpectedEntityCount}, 遍历得到 EntityCount={QueryIterationMetrics.ParallelCount}; " +
            $"期望 SumX={QueryIterationMetrics.ExpectedSumX}, 遍历得到 SumX={QueryIterationMetrics.ParallelSumX}");
    }

    /// <summary>
    /// 在测试代码中直接 <c>foreach</c> <see cref="World.Query{T1,T2,T3,T4,T5}"/>（五组件）：实体数 N，组合标量之和与 <see cref="MultiComponentMetrics.ExpectedT5For"/> 一致。
    /// </summary>
    public static void TestMultiComponentEntityQueryDirect()
    {
        int n = GetMultiComponentEntityCount();
        using var world = new World("TestWorld_MultiComponent_Direct");
        MultiComponentTestWorlds.SpawnMultiComponentAlternating(world, n);
        InitializeAndStart(world);

        var query = world.Query<Position, Velocity, Health, Mana, Rotation>();
        Assert.Equal(n, query.Count);

        double sum = 0d;
        foreach (var row in query)
        {
            row.Deconstruct(out RefRW<Position> pos, out RefRW<Velocity> vel, out RefRW<Health> hp, out RefRW<Mana> mana, out RefRW<Rotation> rot);
            sum += pos.Value.X + vel.Value.Vx + hp.Value.Value + mana.Value.Value + rot.Value.Value;
        }

        double expected = MultiComponentMetrics.ExpectedT5For(n);
        Assert.True(
            Math.Abs(expected - sum) <= MainThreadMultiComponentSumTolerance,
            $"五组件直接遍历组合标量之和：期望 {expected}，实际 {sum}，容差 {MainThreadMultiComponentSumTolerance}");

        MetricLine(
            $"  五组件直接 Query — N={n}, EntityCount={query.Count}, Sum={sum} (期望 {expected})");
    }

    /// <summary>
    /// 主线程 <see cref="ISystem"/> 分别遍历 Query 的 2～5 组件重载；各 arity 独立 World，断言计数与组合和。
    /// </summary>
    public static void TestMultiComponentIterationMainThread()
    {
        MultiComponentMetrics.ResetMain();
        int n = GetMultiComponentEntityCount();

        RunMainThreadArityT2(n);
        RunMainThreadArityT3(n);
        RunMainThreadArityT4(n);
        RunMainThreadArityT5(n);
    }

    private static void RunMainThreadArityT2(int n)
    {
        using var world = new World("TestWorld_Multi_Main_T2");
        MultiComponentTestWorlds.SpawnMultiComponentAlternating(world, n);
        world.AddDefaultFrameSystem<MainThreadMultiT2System>();
        InitializeAndStart(world);
        world.Update(1f / 60f);

        Assert.Equal(n, MultiComponentMetrics.T2MainCount);
        Assert.True(
            Math.Abs(MultiComponentMetrics.ExpectedT2For(n) - MultiComponentMetrics.T2MainSum) <= MainThreadMultiComponentSumTolerance,
            $"T2 主线程组合和：期望 {MultiComponentMetrics.ExpectedT2For(n)}，实际 {MultiComponentMetrics.T2MainSum}");
        MetricLine($"  主线程 T2 — N={n}, EntityCount={MultiComponentMetrics.T2MainCount}, Sum={MultiComponentMetrics.T2MainSum}");
    }

    private static void RunMainThreadArityT3(int n)
    {
        using var world = new World("TestWorld_Multi_Main_T3");
        MultiComponentTestWorlds.SpawnMultiComponentAlternating(world, n);
        world.AddDefaultFrameSystem<MainThreadMultiT3System>();
        InitializeAndStart(world);
        world.Update(1f / 60f);

        Assert.Equal(n, MultiComponentMetrics.T3MainCount);
        Assert.True(
            Math.Abs(MultiComponentMetrics.ExpectedT3For(n) - MultiComponentMetrics.T3MainSum) <= MainThreadMultiComponentSumTolerance,
            $"T3 主线程组合和：期望 {MultiComponentMetrics.ExpectedT3For(n)}，实际 {MultiComponentMetrics.T3MainSum}");
        MetricLine($"  主线程 T3 — N={n}, EntityCount={MultiComponentMetrics.T3MainCount}, Sum={MultiComponentMetrics.T3MainSum}");
    }

    private static void RunMainThreadArityT4(int n)
    {
        using var world = new World("TestWorld_Multi_Main_T4");
        MultiComponentTestWorlds.SpawnMultiComponentAlternating(world, n);
        world.AddDefaultFrameSystem<MainThreadMultiT4System>();
        InitializeAndStart(world);
        world.Update(1f / 60f);

        Assert.Equal(n, MultiComponentMetrics.T4MainCount);
        Assert.True(
            Math.Abs(MultiComponentMetrics.ExpectedT4For(n) - MultiComponentMetrics.T4MainSum) <= MainThreadMultiComponentSumTolerance,
            $"T4 主线程组合和：期望 {MultiComponentMetrics.ExpectedT4For(n)}，实际 {MultiComponentMetrics.T4MainSum}");
        MetricLine($"  主线程 T4 — N={n}, EntityCount={MultiComponentMetrics.T4MainCount}, Sum={MultiComponentMetrics.T4MainSum}");
    }

    private static void RunMainThreadArityT5(int n)
    {
        using var world = new World("TestWorld_Multi_Main_T5");
        MultiComponentTestWorlds.SpawnMultiComponentAlternating(world, n);
        world.AddDefaultFrameSystem<MainThreadMultiT5System>();
        InitializeAndStart(world);
        world.Update(1f / 60f);

        Assert.Equal(n, MultiComponentMetrics.T5MainCount);
        Assert.True(
            Math.Abs(MultiComponentMetrics.ExpectedT5For(n) - MultiComponentMetrics.T5MainSum) <= MainThreadMultiComponentSumTolerance,
            $"T5 主线程组合和：期望 {MultiComponentMetrics.ExpectedT5For(n)}，实际 {MultiComponentMetrics.T5MainSum}");
        MetricLine($"  主线程 T5 — N={n}, EntityCount={MultiComponentMetrics.T5MainCount}, Sum={MultiComponentMetrics.T5MainSum}");
    }

    /// <summary>
    /// <see cref="IParallelSystem{T1}"/>～<see cref="IParallelSystem{T1,T2,T3,T4,T5}"/>：各 arity 独立 World（<see cref="WorldOptions.Default"/>），帧后原子累加与期望一致。
    /// </summary>
    public static void TestMultiComponentIterationParallel()
    {
        MultiComponentMetrics.ResetParallel();
        double tol = MultiComponentParallelSumTolerance();
        int n = GetMultiComponentEntityCount();

        RunParallelArityT2(n, tol);
        RunParallelArityT3(n, tol);
        RunParallelArityT4(n, tol);
        RunParallelArityT5(n, tol);
    }

    private static void RunParallelArityT2(int n, double tol)
    {
        using var world = new World("TestWorld_Multi_Parallel_T2", WorldOptions.Default);
        MultiComponentTestWorlds.SpawnMultiComponentAlternating(world, n);
        world.AddDefaultFrameSystem<ScheduleParallelMultiT2System>();
        InitializeAndStart(world);
        world.Update(1f / 60f);

        Assert.Equal(n, MultiComponentMetrics.T2ParallelCount);
        Assert.True(
            Math.Abs(MultiComponentMetrics.ExpectedT2For(n) - MultiComponentMetrics.T2ParallelSum) <= tol,
            $"T2 并行组合和：期望 {MultiComponentMetrics.ExpectedT2For(n)}，实际 {MultiComponentMetrics.T2ParallelSum}");
        MetricLine(
            $"  并行 T2 — N={n}, EntityCount={MultiComponentMetrics.T2ParallelCount}, Sum={MultiComponentMetrics.T2ParallelSum}");
    }

    private static void RunParallelArityT3(int n, double tol)
    {
        using var world = new World("TestWorld_Multi_Parallel_T3", WorldOptions.Default);
        MultiComponentTestWorlds.SpawnMultiComponentAlternating(world, n);
        world.AddDefaultFrameSystem<ScheduleParallelMultiT3System>();
        InitializeAndStart(world);
        world.Update(1f / 60f);

        Assert.Equal(n, MultiComponentMetrics.T3ParallelCount);
        Assert.True(
            Math.Abs(MultiComponentMetrics.ExpectedT3For(n) - MultiComponentMetrics.T3ParallelSum) <= tol,
            $"T3 并行组合和：期望 {MultiComponentMetrics.ExpectedT3For(n)}，实际 {MultiComponentMetrics.T3ParallelSum}");
        MetricLine(
            $"  并行 T3 — N={n}, EntityCount={MultiComponentMetrics.T3ParallelCount}, Sum={MultiComponentMetrics.T3ParallelSum}");
    }

    private static void RunParallelArityT4(int n, double tol)
    {
        using var world = new World("TestWorld_Multi_Parallel_T4", WorldOptions.Default);
        MultiComponentTestWorlds.SpawnMultiComponentAlternating(world, n);
        world.AddDefaultFrameSystem<ScheduleParallelMultiT4System>();
        InitializeAndStart(world);
        world.Update(1f / 60f);

        Assert.Equal(n, MultiComponentMetrics.T4ParallelCount);
        Assert.True(
            Math.Abs(MultiComponentMetrics.ExpectedT4For(n) - MultiComponentMetrics.T4ParallelSum) <= tol,
            $"T4 并行组合和：期望 {MultiComponentMetrics.ExpectedT4For(n)}，实际 {MultiComponentMetrics.T4ParallelSum}");
        MetricLine(
            $"  并行 T4 — N={n}, EntityCount={MultiComponentMetrics.T4ParallelCount}, Sum={MultiComponentMetrics.T4ParallelSum}");
    }

    private static void RunParallelArityT5(int n, double tol)
    {
        using var world = new World("TestWorld_Multi_Parallel_T5", WorldOptions.Default);
        MultiComponentTestWorlds.SpawnMultiComponentAlternating(world, n);
        world.AddDefaultFrameSystem<ScheduleParallelMultiT5System>();
        InitializeAndStart(world);
        world.Update(1f / 60f);

        Assert.Equal(n, MultiComponentMetrics.T5ParallelCount);
        Assert.True(
            Math.Abs(MultiComponentMetrics.ExpectedT5For(n) - MultiComponentMetrics.T5ParallelSum) <= tol,
            $"T5 并行组合和：期望 {MultiComponentMetrics.ExpectedT5For(n)}，实际 {MultiComponentMetrics.T5ParallelSum}");
        MetricLine(
            $"  并行 T5 — N={n}, EntityCount={MultiComponentMetrics.T5ParallelCount}, Sum={MultiComponentMetrics.T5ParallelSum}");
    }

    /// <summary>
    /// 高实体（默认 2.5 万，可由 ECSTEST_HIGH_ENTITY_N 调整）：IntegrateVelocity + 每实体重浮点 + 主线程汇总 + 并行汇总； 连续多帧 Update，校验末帧 SumX 与实体数；打印创建耗时与每帧平均耗时。
    /// </summary>
    public static void TestHighEntityMultiSystemPipeline()
    {
        int n = GetHighEntityCount();
        const int stressFrames = 3;
        const float dt = 1f / 60f;
        const float initialX = 1f;
        const float vx = 1f;
        float expectedSumAfterFrames = HighEntityStressMath.ExpectedUniformSumX(n, initialX, vx, dt * stressFrames);
        const float parallelTolerance = 0.25f;
        float mainVsParallelTolerance = Math.Max(16f, n * 4e-4f);

        QueryIterationMetrics.ResetAll();

        var swSpawn = Stopwatch.StartNew();
        using var world = new World("TestWorld_HighEntityPipeline", WorldOptions.Default);
        for (int i = 0; i < n; i++)
        {
            world.CreateEntity(
                new Position { X = initialX, Y = 0f },
                new Velocity { Vx = vx, Vy = 2f });
        }

        swSpawn.Stop();

        world.AddDefaultFrameSystem<ResetParallelAggregationSystem>();
        world.AddDefaultFrameSystem<IntegrateVelocitySystem>();
        world.AddDefaultFrameSystem<HeavyPositionTouchSystem>();
        world.AddDefaultFrameSystem<MainThreadPositionQuerySystem>();
        world.AddDefaultFrameSystem<ScheduleParallelPositionQuerySystem>();

        InitializeAndStart(world);

        var swUpdates = Stopwatch.StartNew();
        for (int f = 0; f < stressFrames; f++)
            world.Update(dt);

        swUpdates.Stop();

        var query = world.Query<Position>();
        Assert.Equal(n, query.Count);

        Assert.Equal(n, QueryIterationMetrics.MainThreadCount);

        Assert.Equal(n, QueryIterationMetrics.ParallelCount);

        Assert.True(
            Math.Abs(expectedSumAfterFrames - QueryIterationMetrics.ParallelSumX) <= parallelTolerance,
            $"并行聚合 SumX：期望 {expectedSumAfterFrames}，实际 {QueryIterationMetrics.ParallelSumX}，容差 {parallelTolerance}");

        Assert.True(
            Math.Abs(QueryIterationMetrics.ParallelSumX - QueryIterationMetrics.MainThreadSumX) <= mainVsParallelTolerance,
            $"主线程与并行 SumX 一致：并行 {QueryIterationMetrics.ParallelSumX}，主线程 {QueryIterationMetrics.MainThreadSumX}，容差 {mainVsParallelTolerance}");

        MetricLine(
            $"  高实体流水线 — N={n}, 系统数=5（ResetParallel→Integrate→Heavy→MainThreadSum→ParallelSum）, " +
            $"Heavy.InnerIterations={HeavyPositionTouchSystem.InnerIterations}, 连续帧={stressFrames}, dt={dt:G9} s");
        MetricLine(
            $"  创建实体: {swSpawn.Elapsed.TotalMilliseconds:F3} ms; {stressFrames} 帧 Update 合计: {swUpdates.Elapsed.TotalMilliseconds:F3} ms; " +
            $"平均每帧: {swUpdates.Elapsed.TotalMilliseconds / stressFrames:F3} ms");
        MetricLine(
            $"  期望 EntityCount={n}, 主线程/并行 EntityCount={QueryIterationMetrics.MainThreadCount}/{QueryIterationMetrics.ParallelCount}; " +
            $"期望 SumX={expectedSumAfterFrames:G9}, 主线程 SumX={QueryIterationMetrics.MainThreadSumX:G9}, 并行 SumX={QueryIterationMetrics.ParallelSumX:G9}");
    }
}