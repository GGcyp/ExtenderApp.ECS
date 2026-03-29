using System.Threading;
using ECSTest.Components;
using ExtenderApp.ECS.Abstract;
using ExtenderApp.ECS.Accessors;
using ExtenderApp.ECS.Queries;
using ExtenderApp.ECS.Systems;

namespace ECSTest.WorldTests;

/// <summary>
/// 查询遍历测试的期望与并行累加状态（与断言、控制台输出共用同一套数值）。
/// </summary>
internal static class QueryIterationMetrics
{
    public const int ExpectedEntityCount = 2;
    public const float ExpectedSumX = 11f;
    public const double FloatTolerance = 1e-4;

    public const long SumXScale = 10_000L;

    public static int MainThreadCount;
    public static float MainThreadSumX;

    private static int _parallelCount;
    private static long _parallelSumXScaled;

    public static int ParallelCount => Volatile.Read(ref _parallelCount);

    public static float ParallelSumX => Volatile.Read(ref _parallelSumXScaled) / SumXScale;

    public static void ResetMainThreadOnly()
    {
        MainThreadCount = 0;
        MainThreadSumX = 0f;
    }

    public static void ResetParallelOnly()
    {
        Interlocked.Exchange(ref _parallelCount, 0);
        Interlocked.Exchange(ref _parallelSumXScaled, 0);
    }

    public static void ResetAll()
    {
        ResetMainThreadOnly();
        ResetParallelOnly();
    }

    public static void AddParallelRow(float x)
    {
        Interlocked.Increment(ref _parallelCount);
        Interlocked.Add(ref _parallelSumXScaled, (long)(x * SumXScale));
    }
}

/// <summary>
/// 每帧最先执行：清空并行遍历累加器，便于同一 World 内多帧 Update 后仍按「末帧」断言并行结果。
/// </summary>
public struct ResetParallelAggregationSystem : ISystem
{
    public void OnCreate(ref SystemCreateContext createContext)
    {
    }

    public void OnStart()
    {
    }

    public void OnUpdate(ref SystemUpdateContext updateContext)
    {
        QueryIterationMetrics.ResetParallelOnly();
    }

    public void OnStop()
    {
    }

    public void OnDestroy()
    {
    }
}

/// <summary>
/// 主线程 ISystem：在 OnUpdate 中遍历 Query&lt;Position&gt; 并写入指标。
/// </summary>
public struct MainThreadPositionQuerySystem : ISystem
{
    public void OnCreate(ref SystemCreateContext createContext)
    {
    }

    public void OnStart()
    {
    }

    public void OnUpdate(ref SystemUpdateContext updateContext)
    {
        var query = updateContext.Query<Position>();
        int count = 0;
        float sumX = 0f;
        foreach (var row in query)
        {
            row.DeconstructRefs(out RefRW<Position> pos);
            count++;
            sumX += pos.Value.X;
        }

        QueryIterationMetrics.MainThreadCount = count;
        QueryIterationMetrics.MainThreadSumX = sumX;
    }

    public void OnStop()
    {
    }

    public void OnDestroy()
    {
    }
}

/// <summary>
/// 主线程 ISystem：通过 AddParallelSystem 调度 Position 的并行遍历作业。
/// </summary>
public struct ScheduleParallelPositionQuerySystem : ISystem
{
    public void OnCreate(ref SystemCreateContext createContext)
    {
    }

    public void OnStart()
    {
    }

    public void OnUpdate(ref SystemUpdateContext updateContext)
    {
        updateContext.AddParallelSystem<PositionParallelAccumulator, Position>();
    }

    public void OnStop()
    {
    }

    public void OnDestroy()
    {
    }
}

/// <summary>
/// 并行系统：在工作线程中遍历 JobEntityQuery&lt;Position&gt; 并原子累加指标。
/// </summary>
public struct PositionParallelAccumulator : IParallelSystem<Position>
{
    public void OnUpdate(JobEntityQuery<Position> queryResult, ref SystemUpdateContext updateContext)
    {
        foreach (var row in queryResult)
        {
            row.DeconstructRefs(out RefRW<Position> pos);
            QueryIterationMetrics.AddParallelRow(pos.Value.X);
        }
    }
}
