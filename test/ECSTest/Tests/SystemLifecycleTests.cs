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
    public void System_LifecycleCounts_AreAsExpected()
    {
        WorldSuite.TestSystemLifecycle();
    }

    /// <summary>
    /// 仅调用 FixedUpdate 时仍能完成生命周期引导。
    /// </summary>
    [Fact]
    public void System_FixedUpdate_BootstrapsLifecycle()
    {
        WorldSuite.TestSystemLifecycleBootstrapFromFixedUpdate();
    }

    /// <summary>
    /// 在已有 Update 之后注册的系统仍能收到 OnCreate / OnStart。
    /// </summary>
    [Fact]
    public void System_LateRegistration_ReceivesLifecycle()
    {
        WorldSuite.TestLateRegisteredSystemGetsLifecycle();
    }

    /// <summary>
    /// 未知自定义组名时抛出 <see cref="ArgumentException"/>。
    /// </summary>
    [Fact]
    public void System_UnknownCustomGroupName_Throws()
    {
        WorldSuite.TestUnknownSystemGroupThrows();
    }

    /// <summary>
    /// Lightweight 预设下并行作业被关闭。
    /// </summary>
    [Fact]
    public void World_Lightweight_DisablesParallelJobs()
    {
        WorldSuite.TestWorldLightweightOptions();
    }

    /// <summary>
    /// 自定义系统组上的系统正常参与 Update。
    /// </summary>
    [Fact]
    public void System_CustomGroup_IsSchedulable()
    {
        WorldSuite.TestCustomSystemGroup();
    }
}
