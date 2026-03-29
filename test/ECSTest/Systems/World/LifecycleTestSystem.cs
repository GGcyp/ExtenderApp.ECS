using System.Threading;
using ExtenderApp.ECS.Abstract;
using ExtenderApp.ECS.Systems;

namespace ECSTest.WorldTests;

/// <summary>
/// 用于验证 <see cref="ISystem"/> 生命周期调用次数的测试用系统（单线程测试下使用静态计数器）。
/// </summary>
public struct LifecycleTestSystem : ISystem
{
    private static int _createCount;
    private static int _startCount;
    private static int _updateCount;
    private static int _stopCount;
    private static int _destroyCount;

    public static int CreateCount => Volatile.Read(ref _createCount);
    public static int StartCount => Volatile.Read(ref _startCount);
    public static int UpdateCount => Volatile.Read(ref _updateCount);
    public static int StopCount => Volatile.Read(ref _stopCount);
    public static int DestroyCount => Volatile.Read(ref _destroyCount);

    public static void Reset()
    {
        Interlocked.Exchange(ref _createCount, 0);
        Interlocked.Exchange(ref _startCount, 0);
        Interlocked.Exchange(ref _updateCount, 0);
        Interlocked.Exchange(ref _stopCount, 0);
        Interlocked.Exchange(ref _destroyCount, 0);
    }

    public void OnCreate(ref SystemCreateContext createContext)
        => Interlocked.Increment(ref _createCount);

    public void OnStart()
        => Interlocked.Increment(ref _startCount);

    public void OnUpdate(ref SystemUpdateContext updateContext)
        => Interlocked.Increment(ref _updateCount);

    public void OnStop()
        => Interlocked.Increment(ref _stopCount);

    public void OnDestroy()
        => Interlocked.Increment(ref _destroyCount);
}
