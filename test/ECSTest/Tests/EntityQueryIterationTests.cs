using Xunit;
using WorldSuite = ECSTest.WorldTests.WorldTests;

namespace ECSTest.Tests;

/// <summary>
/// EntityQuery 直接遍历与主线程 / 并行系统路径。
/// </summary>
public sealed class EntityQueryIterationTests : EcsTestContext
{
    /// <summary>
    /// 测试代码中直接 foreach Query&lt;Position&gt; 的计数与 X 之和。
    /// </summary>
    [Fact]
    public void EntityQuery_直接遍历_Position_计数与和()
    {
        WorldSuite.TestEntityQueryIteration();
    }

    /// <summary>
    /// 主线程 ISystem 内遍历 Query&lt;Position&gt;。
    /// </summary>
    [Fact]
    public void EntityQuery_主线程系统遍历_Position()
    {
        WorldSuite.TestEntityQueryIterationMainThreadSystem();
    }

    /// <summary>
    /// 并行 IParallelSystem 遍历 Query&lt;Position&gt;。
    /// </summary>
    [Fact]
    public void EntityQuery_并行系统遍历_Position()
    {
        WorldSuite.TestEntityQueryIterationParallelSystem();
    }
}
