using ECSTest.CustomRuns;
using Xunit;

namespace ECSTest.Tests;

/// <summary>
/// CustomRunner 与命令缓冲等路径的断言覆盖（小规模）。
/// </summary>
public sealed class CustomRunnerVerificationTests : EcsTestContext
{
    /// <summary>
    /// EntityQuery 构建、筛选、委托与 foreach 解构。
    /// </summary>
    [Fact]
    public void Custom_EntityQueryBuild_等价于验证器()
    {
        EntityQueryBuildVerification.Verify();
    }

    /// <summary>
    /// RelationPair / RelationMask 基本语义。
    /// </summary>
    [Fact]
    public void Custom_RelationPair_语义()
    {
        RelationPairVerification.Verify();
    }

    /// <summary>
    /// 命令缓冲多线程写入后回放。
    /// </summary>
    [Fact]
    public void Custom_CommandBuffer_并发写入回放()
    {
        CustomRunnerCommandsVerification.VerifyCommandBufferConcurrency(32);
    }

    /// <summary>
    /// 主线程直接大量组件操作。
    /// </summary>
    [Fact]
    public void Custom_World_直调组件操作()
    {
        CustomRunnerCommandsVerification.VerifyWorldDirectExecution(64);
    }

    /// <summary>
    /// DestroyEntitiesForQuery 清空带 Position 的实体。
    /// </summary>
    [Fact]
    public void Custom_DestroyEntitiesForQuery()
    {
        CustomRunnerCommandsVerification.VerifyDestroyEntitiesForQuery();
    }

    /// <summary>
    /// 共享组件增删改查。
    /// </summary>
    [Fact]
    public void Custom_SharedComponent_CRUD()
    {
        CustomRunnerCommandsVerification.VerifySharedComponentCrud();
    }

    /// <summary>
    /// 托管组件批量创建与查询条数。
    /// </summary>
    [Fact]
    public void Custom_ManagedData_查询条数()
    {
        CustomRunnerCommandsVerification.VerifyManagedUseData();
    }

    /// <summary>
    /// 掩码批量创建后逐条销毁。
    /// </summary>
    [Fact]
    public void Custom_DestroyEntities_批量掩码()
    {
        CustomRunnerCommandsVerification.VerifyDestroyEntitiesBatch();
    }

    /// <summary>
    /// Create/Set/Get 多组件校验和。
    /// </summary>
    [Fact]
    public void Perf_CreateSetGet_校验和()
    {
        CustomRunnerPerfVerification.VerifyCreateSetGet(64);
    }

    /// <summary>
    /// 多实体 CRUD 随机抽样。
    /// </summary>
    [Fact]
    public void Perf_MultipleEntitiesCRUD_抽样()
    {
        CustomRunnerPerfVerification.VerifyMultipleEntitiesCrud(256);
    }

    /// <summary>
    /// 批量 Add 再 Remove 一半。
    /// </summary>
    [Fact]
    public void Perf_BulkAddRemove()
    {
        CustomRunnerPerfVerification.VerifyBulkAddRemove(256);
    }

    /// <summary>
    /// 批量 CreateEntity(Span) 与批量销毁。
    /// </summary>
    [Fact]
    public void Perf_BatchCreate()
    {
        CustomRunnerPerfVerification.VerifyBatchCreate(512);
    }

    /// <summary>
    /// 缺组件读取抛错、写入后可读。
    /// </summary>
    [Fact]
    public void Perf_ExceptionBehavior()
    {
        CustomRunnerPerfVerification.VerifyExceptionBehavior();
    }

    /// <summary>
    /// Lookup / Operation 与应力校验和。
    /// </summary>
    [Fact]
    public void Perf_EntityComponentApi()
    {
        CustomRunnerPerfVerification.VerifyEntityComponentApi(256);
    }

    /// <summary>
    /// 巨型组件与不含巨型组件的校验和可区分。
    /// </summary>
    [Fact]
    public void Perf_HugeComponent_对照小规模()
    {
        CustomRunnerPerfVerification.VerifyHugeComponentComparisonSmall(24);
    }
}
