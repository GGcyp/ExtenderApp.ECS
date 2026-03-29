using Xunit;
using WorldSuite = ECSTest.WorldTests.WorldTests;

namespace ECSTest.Tests;

/// <summary>
/// 系统生命周期、组与 World 选项。
/// </summary>
public sealed class SystemLifecycleTests : EcsTestContext
{
    /// <summary>
    /// 系统 OnCreate / OnStart / OnUpdate / OnStop / OnDestroy 计数符合预期。
    /// </summary>
    [Fact]
    public void System_生命周期计数符合预期()
    {
        WorldSuite.TestSystemLifecycle();
    }

    /// <summary>
    /// 仅调用 FixedUpdate 时仍能完成生命周期引导。
    /// </summary>
    [Fact]
    public void System_FixedUpdate_可引导生命周期()
    {
        WorldSuite.TestSystemLifecycleBootstrapFromFixedUpdate();
    }

    /// <summary>
    /// 在已有 Update 之后注册的系统仍能收到 OnCreate / OnStart。
    /// </summary>
    [Fact]
    public void System_延迟注册仍获得生命周期()
    {
        WorldSuite.TestLateRegisteredSystemGetsLifecycle();
    }

    /// <summary>
    /// 未知自定义组名时抛出 <see cref="ArgumentException"/>。
    /// </summary>
    [Fact]
    public void System_未知自定义组名抛异常()
    {
        WorldSuite.TestUnknownSystemGroupThrows();
    }

    /// <summary>
    /// Lightweight 预设下并行作业被关闭。
    /// </summary>
    [Fact]
    public void World_Lightweight_关闭并行作业()
    {
        WorldSuite.TestWorldLightweightOptions();
    }

    /// <summary>
    /// 自定义系统组上的系统正常参与 Update。
    /// </summary>
    [Fact]
    public void System_自定义组可调度()
    {
        WorldSuite.TestCustomSystemGroup();
    }
}
