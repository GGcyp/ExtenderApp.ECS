using Xunit;
using WorldSuite = ECSTest.WorldTests.WorldTests;

namespace ECSTest.Tests;

/// <summary>
/// 多组件 Query 直接遍历、主线程 arity 2～5、并行 arity 2～5。
/// </summary>
public sealed class MultiComponentQueryTests : EcsTestContext
{
    /// <summary>
    /// 五组件直接 foreach 与期望组合和一致。
    /// </summary>
    [Fact]
    public void MultiComponent_DirectQuery_T5_Checksum()
    {
        WorldSuite.TestMultiComponentEntityQueryDirect();
    }

    /// <summary>
    /// 主线程系统 T2～T5 各 arity 独立 World 断言。
    /// </summary>
    [Fact]
    public void MultiComponent_MainThread_T2ToT5()
    {
        WorldSuite.TestMultiComponentIterationMainThread();
    }

    /// <summary>
    /// 并行系统 T2～T5 各 arity 独立 World 断言。
    /// </summary>
    [Fact]
    public void MultiComponent_Parallel_T2ToT5()
    {
        WorldSuite.TestMultiComponentIterationParallel();
    }

    /// <summary>
    /// 多组件实体数 N 取不同值时仍保持闭式期望（轻量规模）。
    /// </summary>
    [Theory]
    [InlineData(2)]
    [InlineData(32)]
    public void MultiComponent_N_Variation_RemainsConsistent(int n)
    {
        WorldSuite.MultiComponentStressEntityCount = n;
        WorldSuite.TestMultiComponentEntityQueryDirect();
        WorldSuite.TestMultiComponentIterationMainThread();
        WorldSuite.TestMultiComponentIterationParallel();
    }
}
