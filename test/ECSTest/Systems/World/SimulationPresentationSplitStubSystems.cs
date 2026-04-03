using System.Threading;
using ExtenderApp.ECS.Abstract;
using ExtenderApp.ECS.Systems;

namespace ECSTest.WorldTests;

/// <summary>
/// 用于「模拟 / 呈现」拆分调度测试：默认每帧组上的 OnUpdate 计数。
/// </summary>
public struct SplitScheduleSimulationCounterSystem : ISystem
{
    private static int _updateCount;
    private static long _lastFrameIndex;

    public static int UpdateCount => Volatile.Read(ref _updateCount);

    /// <summary>
    /// 最近一次模拟步中见到的 <see cref="SystemUpdateContext.FrameIndex"/>。
    /// </summary>
    public static ulong LastSeenFrameIndex => (ulong)Volatile.Read(ref _lastFrameIndex);

    public static void Reset()
    {
        Interlocked.Exchange(ref _updateCount, 0);
        Interlocked.Exchange(ref _lastFrameIndex, 0);
    }

    public void OnCreate(ref SystemCreateContext createContext)
    {
    }

    public void OnStart()
    {
    }

    public void OnUpdate(ref SystemUpdateContext updateContext)
    {
        Interlocked.Increment(ref _updateCount);
        Interlocked.Exchange(ref _lastFrameIndex, (long)updateContext.FrameIndex);
    }

    public void OnStop()
    {
    }

    public void OnDestroy()
    {
    }
}

/// <summary>
/// 用于「模拟 / 呈现」拆分调度测试：渲染每帧组上的 OnUpdate 计数与最后一次见到的帧序号。
/// </summary>
public struct SplitSchedulePresentationCounterSystem : ISystem
{
    private static int _updateCount;
    private static long _lastFrameIndex;

    public static int UpdateCount => Volatile.Read(ref _updateCount);

    /// <summary>
    /// 最近一次呈现步中见到的 <see cref="SystemUpdateContext.FrameIndex"/>。
    /// </summary>
    public static ulong LastSeenFrameIndex => (ulong)Volatile.Read(ref _lastFrameIndex);

    public static void Reset()
    {
        Interlocked.Exchange(ref _updateCount, 0);
        Interlocked.Exchange(ref _lastFrameIndex, 0);
    }

    public void OnCreate(ref SystemCreateContext createContext)
    {
    }

    public void OnStart()
    {
    }

    public void OnUpdate(ref SystemUpdateContext updateContext)
    {
        Interlocked.Increment(ref _updateCount);
        Interlocked.Exchange(ref _lastFrameIndex, (long)updateContext.FrameIndex);
    }

    public void OnStop()
    {
    }

    public void OnDestroy()
    {
    }
}
