using System.Threading;
using ECSTest.Components;
using ExtenderApp.ECS;
using ExtenderApp.ECS.Abstract;
using ExtenderApp.ECS.Accessors;
using ExtenderApp.ECS.Queries;
using ExtenderApp.ECS.Systems;

namespace ECSTest.WorldTests;

/// <summary>
/// 多组件查询/并行作业遍历的期望与累加（2～5 个组件 arity 各一套字段）。
/// <see cref="SpawnMultiComponentPair"/> 为 N=2；更大 N 用 <see cref="SpawnMultiComponentAlternating"/>，期望由 <see cref="ExpectedT2For"/> 等给出。
/// </summary>
internal static class MultiComponentMetrics
{
    public const int ExpectedEntityCount = 2;
    public const double FloatTolerance = 1e-3;
    public const long SumScale = 10_000L;

    /// <summary>sum(pos.X + vel.Vx) = 3 + 30（N=2）</summary>
    public const float ExpectedT2 = 33f;

    /// <summary>上式 + sum(health) = 33 + 101（N=2）</summary>
    public const float ExpectedT3 = 134f;

    /// <summary>上式 + sum(mana) = 134 + 12（N=2）</summary>
    public const float ExpectedT4 = 146f;

    /// <summary>上式 + sum(rotation.Value) = 146 + 2（N=2）</summary>
    public const float ExpectedT5 = 148f;

    /// <summary>与 <see cref="SpawnMultiComponentAlternating"/> 一致：偶数下标为原先实体 A，奇数下标为实体 B。</summary>
    public static void GetEvenOddCounts(int n, out int countA, out int countB)
    {
        countA = (n + 1) / 2;
        countB = n / 2;
    }

    /// <summary>闭式期望（双精度，避免大 N 时 float 累加超过 ~2^24 后无法精确表示总和）。</summary>
    public static double ExpectedT2For(int n)
    {
        GetEvenOddCounts(n, out var a, out var b);
        return 3d * a + 30d * b;
    }

    public static double ExpectedT3For(int n)
    {
        GetEvenOddCounts(n, out var a, out var b);
        return 103d * a + 31d * b;
    }

    public static double ExpectedT4For(int n)
    {
        GetEvenOddCounts(n, out var a, out var b);
        return 108d * a + 38d * b;
    }

    public static double ExpectedT5For(int n)
    {
        GetEvenOddCounts(n, out var a, out var b);
        return 108.5d * a + 39.5d * b;
    }

    public static int T2MainCount;
    public static double T2MainSum;
    public static int T3MainCount;
    public static double T3MainSum;
    public static int T4MainCount;
    public static double T4MainSum;
    public static int T5MainCount;
    public static double T5MainSum;

    private static int _t2PC;
    private static long _t2PS;
    private static int _t3PC;
    private static long _t3PS;
    private static int _t4PC;
    private static long _t4PS;
    private static int _t5PC;
    private static long _t5PS;

    public static int T2ParallelCount => Volatile.Read(ref _t2PC);
    public static float T2ParallelSum => Volatile.Read(ref _t2PS) / (float)SumScale;
    public static int T3ParallelCount => Volatile.Read(ref _t3PC);
    public static float T3ParallelSum => Volatile.Read(ref _t3PS) / (float)SumScale;
    public static int T4ParallelCount => Volatile.Read(ref _t4PC);
    public static float T4ParallelSum => Volatile.Read(ref _t4PS) / (float)SumScale;
    public static int T5ParallelCount => Volatile.Read(ref _t5PC);
    public static float T5ParallelSum => Volatile.Read(ref _t5PS) / (float)SumScale;

    public static void ResetMain()
    {
        T2MainCount = T3MainCount = T4MainCount = T5MainCount = 0;
        T2MainSum = T3MainSum = T4MainSum = T5MainSum = 0d;
    }

    public static void ResetParallel()
    {
        Interlocked.Exchange(ref _t2PC, 0);
        Interlocked.Exchange(ref _t2PS, 0);
        Interlocked.Exchange(ref _t3PC, 0);
        Interlocked.Exchange(ref _t3PS, 0);
        Interlocked.Exchange(ref _t4PC, 0);
        Interlocked.Exchange(ref _t4PS, 0);
        Interlocked.Exchange(ref _t5PC, 0);
        Interlocked.Exchange(ref _t5PS, 0);
    }

    public static void ResetAll()
    {
        ResetMain();
        ResetParallel();
    }

    public static void AddParallelT2(float v)
    {
        Interlocked.Increment(ref _t2PC);
        Interlocked.Add(ref _t2PS, (long)(v * SumScale));
    }

    public static void AddParallelT3(float v)
    {
        Interlocked.Increment(ref _t3PC);
        Interlocked.Add(ref _t3PS, (long)(v * SumScale));
    }

    public static void AddParallelT4(float v)
    {
        Interlocked.Increment(ref _t4PC);
        Interlocked.Add(ref _t4PS, (long)(v * SumScale));
    }

    public static void AddParallelT5(float v)
    {
        Interlocked.Increment(ref _t5PC);
        Interlocked.Add(ref _t5PS, (long)(v * SumScale));
    }
}

/// <summary>
/// 创建两个 Archetype 相同的实体，含 Position、Velocity、Health、Mana、Rotation，数值与 <see cref="MultiComponentMetrics"/> 期望一致。
/// </summary>
internal static class MultiComponentTestWorlds
{
    public static void SpawnMultiComponentPair(World world)
    {
        world.CreateEntity(
            new Position { X = 1f, Y = 0f },
            new Velocity { Vx = 2f, Vy = 0f },
            new Health { Value = 100 },
            new Mana { Value = 5 },
            new Rotation { Value = 0.5f });

        world.CreateEntity(
            new Position { X = 10f, Y = 0f },
            new Velocity { Vx = 20f, Vy = 0f },
            new Health { Value = 1 },
            new Mana { Value = 7 },
            new Rotation { Value = 1.5f });
    }

    /// <summary>
    /// 生成 <paramref name="count"/> 个同原型实体：下标偶数为原 A、奇数为原 B，与 <see cref="ExpectedT5For"/> 等一致。
    /// </summary>
    public static void SpawnMultiComponentAlternating(World world, int count)
    {
        for (int i = 0; i < count; i++)
        {
            if ((i & 1) == 0)
            {
                world.CreateEntity(
                    new Position { X = 1f, Y = 0f },
                    new Velocity { Vx = 2f, Vy = 0f },
                    new Health { Value = 100 },
                    new Mana { Value = 5 },
                    new Rotation { Value = 0.5f });
            }
            else
            {
                world.CreateEntity(
                    new Position { X = 10f, Y = 0f },
                    new Velocity { Vx = 20f, Vy = 0f },
                    new Health { Value = 1 },
                    new Mana { Value = 7 },
                    new Rotation { Value = 1.5f });
            }
        }
    }
}

#region Main-thread ISystem (Query T2–T5)

public struct MainThreadMultiT2System : ISystem
{
    public void OnCreate(ref SystemCreateContext createContext) { }
    public void OnStart() { }
    public void OnUpdate(ref SystemUpdateContext updateContext)
    {
        var query = updateContext.Query<Position, Velocity>();
        int c = 0;
        double s = 0d;
        foreach (var row in query)
        {
            row.DeconstructRefs(out RefRW<Position> pos, out RefRW<Velocity> vel);
            c++;
            s += pos.Value.X + vel.Value.Vx;
        }

        MultiComponentMetrics.T2MainCount = c;
        MultiComponentMetrics.T2MainSum = s;
    }

    public void OnStop() { }
    public void OnDestroy() { }
}

public struct MainThreadMultiT3System : ISystem
{
    public void OnCreate(ref SystemCreateContext createContext) { }
    public void OnStart() { }
    public void OnUpdate(ref SystemUpdateContext updateContext)
    {
        var query = updateContext.Query<Position, Velocity, Health>();
        int c = 0;
        double s = 0d;
        foreach (var row in query)
        {
            row.DeconstructRefs(out RefRW<Position> pos, out RefRW<Velocity> vel, out RefRW<Health> hp);
            c++;
            s += pos.Value.X + vel.Value.Vx + hp.Value.Value;
        }

        MultiComponentMetrics.T3MainCount = c;
        MultiComponentMetrics.T3MainSum = s;
    }

    public void OnStop() { }
    public void OnDestroy() { }
}

public struct MainThreadMultiT4System : ISystem
{
    public void OnCreate(ref SystemCreateContext createContext) { }
    public void OnStart() { }
    public void OnUpdate(ref SystemUpdateContext updateContext)
    {
        var query = updateContext.Query<Position, Velocity, Health, Mana>();
        int c = 0;
        double s = 0d;
        foreach (var row in query)
        {
            row.DeconstructRefs(out RefRW<Position> pos, out RefRW<Velocity> vel, out RefRW<Health> hp, out RefRW<Mana> mana);
            c++;
            s += pos.Value.X + vel.Value.Vx + hp.Value.Value + mana.Value.Value;
        }

        MultiComponentMetrics.T4MainCount = c;
        MultiComponentMetrics.T4MainSum = s;
    }

    public void OnStop() { }
    public void OnDestroy() { }
}

public struct MainThreadMultiT5System : ISystem
{
    public void OnCreate(ref SystemCreateContext createContext) { }
    public void OnStart() { }
    public void OnUpdate(ref SystemUpdateContext updateContext)
    {
        var query = updateContext.Query<Position, Velocity, Health, Mana, Rotation>();
        int c = 0;
        double s = 0d;
        foreach (var row in query)
        {
            row.DeconstructRefs(out RefRW<Position> pos, out RefRW<Velocity> vel, out RefRW<Health> hp, out RefRW<Mana> mana, out RefRW<Rotation> rot);
            c++;
            s += pos.Value.X + vel.Value.Vx + hp.Value.Value + mana.Value.Value + rot.Value.Value;
        }

        MultiComponentMetrics.T5MainCount = c;
        MultiComponentMetrics.T5MainSum = s;
    }

    public void OnStop() { }
    public void OnDestroy() { }
}

#endregion

#region Parallel schedulers + IParallelSystem T2–T5

public struct ScheduleParallelMultiT2System : ISystem
{
    public void OnCreate(ref SystemCreateContext createContext) { }
    public void OnStart() { }
    public void OnUpdate(ref SystemUpdateContext updateContext)
        => updateContext.AddParallelSystem<MultiT2ParallelAccumulator, Position, Velocity>();
    public void OnStop() { }
    public void OnDestroy() { }
}

public struct MultiT2ParallelAccumulator : IParallelSystem<Position, Velocity>
{
    public void OnUpdate(JobEntityQuery<Position, Velocity> queryResult, ref SystemUpdateContext updateContext)
    {
        foreach (var row in queryResult)
        {
            row.DeconstructRefs(out RefRW<Position> pos, out RefRW<Velocity> vel);
            MultiComponentMetrics.AddParallelT2(pos.Value.X + vel.Value.Vx);
        }
    }
}

public struct ScheduleParallelMultiT3System : ISystem
{
    public void OnCreate(ref SystemCreateContext createContext) { }
    public void OnStart() { }
    public void OnUpdate(ref SystemUpdateContext updateContext)
        => updateContext.AddParallelSystem<MultiT3ParallelAccumulator, Position, Velocity, Health>();
    public void OnStop() { }
    public void OnDestroy() { }
}

public struct MultiT3ParallelAccumulator : IParallelSystem<Position, Velocity, Health>
{
    public void OnUpdate(JobEntityQuery<Position, Velocity, Health> queryResult, ref SystemUpdateContext updateContext)
    {
        foreach (var row in queryResult)
        {
            row.DeconstructRefs(out RefRW<Position> pos, out RefRW<Velocity> vel, out RefRW<Health> hp);
            MultiComponentMetrics.AddParallelT3(pos.Value.X + vel.Value.Vx + hp.Value.Value);
        }
    }
}

public struct ScheduleParallelMultiT4System : ISystem
{
    public void OnCreate(ref SystemCreateContext createContext) { }
    public void OnStart() { }
    public void OnUpdate(ref SystemUpdateContext updateContext)
        => updateContext.AddParallelSystem<MultiT4ParallelAccumulator, Position, Velocity, Health, Mana>();
    public void OnStop() { }
    public void OnDestroy() { }
}

public struct MultiT4ParallelAccumulator : IParallelSystem<Position, Velocity, Health, Mana>
{
    public void OnUpdate(JobEntityQuery<Position, Velocity, Health, Mana> queryResult, ref SystemUpdateContext updateContext)
    {
        foreach (var row in queryResult)
        {
            row.DeconstructRefs(out RefRW<Position> pos, out RefRW<Velocity> vel, out RefRW<Health> hp, out RefRW<Mana> mana);
            MultiComponentMetrics.AddParallelT4(pos.Value.X + vel.Value.Vx + hp.Value.Value + mana.Value.Value);
        }
    }
}

public struct ScheduleParallelMultiT5System : ISystem
{
    public void OnCreate(ref SystemCreateContext createContext) { }
    public void OnStart() { }
    public void OnUpdate(ref SystemUpdateContext updateContext)
        => updateContext.AddParallelSystem<MultiT5ParallelAccumulator, Position, Velocity, Health, Mana, Rotation>();
    public void OnStop() { }
    public void OnDestroy() { }
}

public struct MultiT5ParallelAccumulator : IParallelSystem<Position, Velocity, Health, Mana, Rotation>
{
    public void OnUpdate(JobEntityQuery<Position, Velocity, Health, Mana, Rotation> queryResult, ref SystemUpdateContext updateContext)
    {
        foreach (var row in queryResult)
        {
            row.DeconstructRefs(out RefRW<Position> pos, out RefRW<Velocity> vel, out RefRW<Health> hp, out RefRW<Mana> mana, out RefRW<Rotation> rot);
            MultiComponentMetrics.AddParallelT5(pos.Value.X + vel.Value.Vx + hp.Value.Value + mana.Value.Value + rot.Value.Value);
        }
    }
}

#endregion
