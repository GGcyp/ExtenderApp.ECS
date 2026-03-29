using Xunit;
using WorldSuite = ECSTest.WorldTests.WorldTests;

namespace ECSTest.Tests;

/// <summary>
/// 高实体多系统流水线（默认约 2.5 万实体，可通过 ECSTEST_HIGH_ENTITY_N 调整）。
/// </summary>
[Trait("Category", "Stress")]
public sealed class HighEntityPipelineTests : EcsTestContext
{
    /// <summary>
    /// 高实体 Integrate + 重浮点 + 主线程与并行汇总一致。
    /// </summary>
    [Fact]
    public void World_高实体多系统流水线_末帧一致()
    {
        WorldSuite.TestHighEntityMultiSystemPipeline();
    }
}
