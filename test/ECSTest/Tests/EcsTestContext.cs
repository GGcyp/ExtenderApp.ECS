using ExtenderApp.ECS.Threading;
using WorldSuite = ECSTest.WorldTests.WorldTests;

namespace ECSTest.Tests;

/// <summary>
/// 为依赖静态指标或控制台开关的集成测试提供统一默认配置。
/// </summary>
public abstract class EcsTestContext
{
    /// <summary>
    /// 将当前执行线程视为 ECS 主线程，并关闭控制台指标、设定多组件默认实体数。
    /// </summary>
    protected EcsTestContext()
    {
        MainThreadDetector.ForceSetCurrentThreadAsMain();
        WorldSuite.EnableConsoleMetrics = false;
        WorldSuite.MultiComponentStressEntityCount = 2;
    }
}
