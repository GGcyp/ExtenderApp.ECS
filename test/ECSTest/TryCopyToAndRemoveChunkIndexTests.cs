using ECSTest.Components;
using ExtenderApp.ECS;
using ExtenderApp.ECS.Components;
using Xunit;

namespace ECSTest;

/// <summary>
/// 针对 <see cref="ExtenderApp.ECS.Archetypes.ArchetypeChunkManager.TryCopyToAndRemove"/> 路径上
/// 可能出现的 <see cref="ArgumentOutOfRangeException"/>（列块列表索引越界）进行复现尝试与辅助定位。
/// </summary>
/// <remarks>
/// 结论摘要（当前仓库版本下的调查）：
/// <list type="bullet">
/// <item><description>
/// 实体段下标 <c>chunkIndex</c> 来自 <see cref="ExtenderApp.ECS.Archetypes.ArchetypeEntitySegmentInfoList.TryFindLocalIndexForGlobalIndex"/>，
/// 与各列 <see cref="ExtenderApp.ECS.Archetypes.ArchetypeChunkList"/> 下标一一对应；各列拥有独立的块列表，
/// 因此「处理完第一列后 RemoveEmptyChunks 缩短列表导致第二列越界」的推断<strong>不成立</strong>。
/// </description></item>
/// <item><description>
/// 「第二实体段 + 双数据列 + 迁移」场景下直接调用 <see cref="ExtenderApp.ECS.Archetype.TryCopyToAndRemove"/> 可完整成功返回，
/// 说明此类简单场景不足以触发 <c>List&lt;T&gt;[index]</c> 越界。
/// </description></item>
/// <item><description>
/// 若运行时出现越界，更可疑的是：<strong>某一列</strong>上 <c>chunkList.Count &lt;= chunkIndex</c>（实体段数与块列表长度不同步），
/// 或 <strong>目标列</strong> <c>newChunkList.Count &lt;= newChunkIndex</c>；应怀疑
/// <see cref="ExtenderApp.ECS.Archetypes.ArchetypeChunkManager.AddEntity"/>、
/// <see cref="ExtenderApp.ECS.Archetypes.ArchetypeChunkManager.RemoveEmptyChunks"/>、
/// 与实体段收缩逻辑之间的不变量被破坏，而非列与列之间的交叉影响。
/// </description></item>
/// </list>
/// </remarks>
public sealed class TryCopyToAndRemoveChunkIndexTests
{
    /// <summary>目标原型上预先存在的同签名实体数量（填满首段 16，使孤立实体迁入时目标侧易出现第二实体段）。</summary>
    private const int PrefillSameSignatureEntityCount = 16;

    /// <summary>随机迁移压力测试的总步数。</summary>
    private const int StressIterationCount = 8000;
    /// <summary>
    /// 校验：对掩码中每个存在块列表的列，列块数量应大于实体段下标 <paramref name="chunkIndex"/>。
    /// </summary>
    private static void AssertChunkListsCoverChunkIndex(Archetype archetype, int chunkIndex, string context)
    {
        foreach (var ct in archetype.ComponentMask)
        {
            if (!archetype.ComponentMask.TryGetEncodedPosition(ct, out var colIdx))
                continue;

            if (TryGetNonEmptyChunkListCount(archetype, colIdx, ct, out var count))
            {
                Assert.True(
                    count > chunkIndex,
                    $"{context}: Entities 段数={archetype.Entities.Count}, chunkIndex={chunkIndex}；列 {colIdx} ({ct}) chunkList.Count={count}。");
            }
        }
    }

    /// <summary>
    /// 解析非空组件列的块列表元素个数（与生产端 TryGetChunkListForColumn 一致）。
    /// </summary>
    private static bool TryGetNonEmptyChunkListCount(
        Archetype archetype,
        int columnIndex,
        ComponentType ct,
        out int count)
    {
        count = 0;
        var type = ct.TypeInstance;
        if (type == typeof(TestPosition) && archetype.TryGetChunkList<TestPosition>(columnIndex, out var listP))
        {
            count = listP.Count;
            return true;
        }

        if (type == typeof(TestVelocity) && archetype.TryGetChunkList<TestVelocity>(columnIndex, out var listV))
        {
            count = listV.Count;
            return true;
        }

        if (type == typeof(TestHealth) && archetype.TryGetChunkList<TestHealth>(columnIndex, out var listH))
        {
            count = listH.Count;
            return true;
        }

        if (type == typeof(TestTag) && archetype.TryGetChunkList<TestTag>(columnIndex, out var listT))
        {
            count = listT.Count;
            return true;
        }

        return false;
    }

    /// <summary>
    /// 基线：第 17 个实体落在第二实体段（chunkIndex=1），两列块列表长度均为 2；
    /// 直接调用 TryCopyToAndRemove 应成功且不抛越界（用于否定「跨列缩短列表」类假设）。
    /// </summary>
    [Fact]
    public void DirectTryCopyToAndRemove_SeventeenthEntity_TwoDataColumns_CompletesSuccessfully()
    {
        using var world = new World();
        Entity last = default;
        for (var i = 0; i < 17; i++)
        {
            last = world.CreateEntity(
                new TestPosition { X = i, Y = i },
                new TestVelocity { Dx = 1, Dy = 1 });
        }

        Assert.True(world.EManager.TryGetArchetype(last, out var oldA, out var oldIx));
        Assert.NotNull(oldA);
        Assert.True(oldA!.Entities.TryFindLocalIndexForGlobalIndex(oldIx, out _, out var chunkIndex));
        Assert.Equal(1, chunkIndex);

        AssertChunkListsCoverChunkIndex(oldA, chunkIndex, nameof(DirectTryCopyToAndRemove_SeventeenthEntity_TwoDataColumns_CompletesSuccessfully));

        var newMask = oldA.ComponentMask;
        newMask.Add(ComponentType.Create<TestHealth>());
        var newA = world.AManager.GetOrCreateArchetype(newMask);
        var newIx = newA.AddEntity(last);

        var ok = oldA.TryCopyToAndRemove(oldIx, newA, newIx, out _);
        Assert.True(ok);
    }

    /// <summary>
    /// 目标原型已存在且首段已满时，再迁入「仅两组件」的孤立实体：目标侧易出现第二实体段（newChunkIndex=1）。
    /// </summary>
    [Fact]
    public void AddComponent_LonelyEntity_TargetArchetypeAlreadyHasFullFirstSegment_DoesNotThrowArgumentOutOfRange()
    {
        using var world = new World();
        for (var i = 0; i < PrefillSameSignatureEntityCount; i++)
        {
            world.CreateEntity(
                new TestPosition { X = i, Y = i },
                new TestVelocity { Dx = 0, Dy = 0 },
                new TestTag { Value = i });
        }

        var lone = world.CreateEntity(
            new TestPosition { X = 100, Y = 100 },
            new TestVelocity { Dx = 1, Dy = 1 });

        Assert.True(world.EManager.TryGetArchetype(lone, out var oldA, out var oldIx));
        Assert.NotNull(oldA);
        Assert.True(oldA!.Entities.TryFindLocalIndexForGlobalIndex(oldIx, out _, out var oldChunk));
        AssertChunkListsCoverChunkIndex(oldA, oldChunk, "迁移前-源原型");

        world.AddComponent(lone, new TestTag { Value = 42 });

        Assert.True(world.EManager.TryGetArchetype(lone, out var newA, out var newIx));
        Assert.NotNull(newA);
        Assert.True(newA!.Entities.TryFindLocalIndexForGlobalIndex(newIx, out _, out var newChunk));
        AssertChunkListsCoverChunkIndex(newA, newChunk, "迁移后-目标原型");

        var tag = world.GetComponent<TestTag>(lone);
        Assert.Equal(42, tag.Value);
    }

    /// <summary>
    /// 命令缓冲回放路径：与 <see cref="AddComponent_LonelyEntity_TargetArchetypeAlreadyHasFullFirstSegment_DoesNotThrowArgumentOutOfRange"/> 相同结构。
    /// </summary>
    [Fact]
    public void Playback_AddComponent_LonelyEntity_TargetArchetypeFullFirstSegment_DoesNotThrowArgumentOutOfRange()
    {
        using var world = new World();
        for (var i = 0; i < PrefillSameSignatureEntityCount; i++)
        {
            world.CreateEntity(
                new TestPosition { X = i, Y = i },
                new TestVelocity { Dx = 0, Dy = 0 },
                new TestTag { Value = i });
        }

        var lone = world.CreateEntity(
            new TestPosition { X = 100, Y = 100 },
            new TestVelocity { Dx = 1, Dy = 1 });

        world.CommandBuffer.AddComponent(lone, new TestTag { Value = 7 });
        world.PlaybackRecordedCommands();

        Assert.True(world.EManager.TryGetArchetype(lone, out var arch, out var gix));
        Assert.NotNull(arch);
        Assert.True(arch!.Entities.TryFindLocalIndexForGlobalIndex(gix, out _, out var ck));
        AssertChunkListsCoverChunkIndex(arch, ck, "回放后");

        Assert.Equal(7, world.GetComponent<TestTag>(lone).Value);
    }

    /// <summary>
    /// 随机迁移/回放压力：若破坏「chunkIndex &lt; chunkList.Count」不变量则断言失败；
    /// 若抛出 <see cref="ArgumentOutOfRangeException"/> 则测试失败并保留异常信息。
    /// </summary>
    /// <remarks>
    /// 每步行为（按随机分支）：
    /// <list type="bullet">
    /// <item><description>创建实体：仅含 <see cref="TestPosition"/> + <see cref="TestVelocity"/>。</description></item>
    /// <item><description>添加 <see cref="TestTag"/>：经 <see cref="World.CommandBuffer"/> 或 <see cref="World.AddComponent{T}(Entity, T)"/>，并调用 <see cref="World.PlaybackRecordedCommands"/> 回放未决命令。</description></item>
    /// <item><description>移除 Tag：<see cref="World.RemoveComponent{T}(Entity)"/> 仅去掉 Tag，实体迁回「仅 P+V」原型，<strong>Position/Velocity 仍保留</strong>；若当前掩码本无 Tag，API 约定为忽略（不报错）。</description></item>
    /// <item><description>销毁：实体从世界中移除。</description></item>
    /// </list>
    /// 因此「删 Tag 之后」预期仍是 P+V 都在，不是「旧原型里错误残留 Tag 列」；本测试每步对存活实体校验列块列表与实体段下标一致，用于捕捉迁移/回收路径上的不同步。
    /// </remarks>
    [Fact]
    public void Stress_RandomMigrations_VerifyChunkListCoversEntitySegmentIndex_OrThrow()
    {
        var rng = new Random(20260412);

        using var world = new World();
        var entities = new List<Entity>();

        for (var step = 0; step < StressIterationCount; step++)
        {
            var roll = rng.Next(100);
            if (roll < 40 || entities.Count == 0)
            {
                var e = world.CreateEntity(
                    new TestPosition { X = step, Y = roll },
                    new TestVelocity { Dx = 1, Dy = 1 });
                entities.Add(e);
            }
            else if (roll < 70)
            {
                var e = entities[rng.Next(entities.Count)];
                if (!world.EManager.IsAlive(e))
                    continue;

                if (rng.Next(2) == 0)
                    world.CommandBuffer.AddComponent(e, new TestTag { Value = step });
                else
                    world.AddComponent(e, new TestTag { Value = step });

                world.PlaybackRecordedCommands();
            }
            else if (roll < 85)
            {
                var e = entities[rng.Next(entities.Count)];
                if (world.EManager.IsAlive(e))
                    world.RemoveComponent<TestTag>(e);
            }
            else
            {
                var e = entities[rng.Next(entities.Count)];
                if (world.EManager.IsAlive(e))
                    world.DestroyEntity(e);
            }

            foreach (var e in entities)
            {
                if (!world.EManager.IsAlive(e))
                    continue;

                if (!world.EManager.TryGetArchetype(e, out var arch, out var gix) || arch == null)
                    continue;

                if (!arch.Entities.TryFindLocalIndexForGlobalIndex(gix, out _, out var chunkIndex))
                    continue;

                AssertChunkListsCoverChunkIndex(arch, chunkIndex, $"step={step}, entity={e}");
            }
        }
    }
}
