using Xunit;
using WorldSuite = ECSTest.WorldTests.WorldTests;

namespace ECSTest.Tests;

/// <summary>
/// World 主循环 Update / FixedUpdate 路径。
/// </summary>
public sealed class WorldLoopTests : EcsTestContext
{
    /// <summary>
    /// 连续多帧 Update 不抛异常。
    /// </summary>
    [Fact]
    public void World_Update_多帧不抛异常()
    {
        WorldSuite.TestWorldUpdate();
    }

    /// <summary>
    /// 连续多步 FixedUpdate 不抛异常。
    /// </summary>
    [Fact]
    public void World_FixedUpdate_多步不抛异常()
    {
        WorldSuite.TestWorldFixedUpdate();
    }
}
