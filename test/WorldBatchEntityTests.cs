using ECSTest.Components;
using ExtenderApp.ECS;
using Xunit;

namespace ECSTest;

/// <summary>
/// 批量创建与批量销毁实体与查询计数的一致性。
/// </summary>
public sealed class WorldBatchEntityTests
{
    [Fact]
    public void CreateEntity_SpanBatch_CreatesDistinctAliveEntities()
    {
        using var world = new World();
        Span<Entity> batch = stackalloc Entity[4];
        world.CreateEntity(batch);

        foreach (var e in batch)
        {
            Assert.True(world.EManager.IsAlive(e));
        }

        var distinct = new HashSet<Entity>();
        foreach (var e in batch)
            Assert.True(distinct.Add(e));
    }

    [Fact]
    public void CreateEntity_BatchPlusSingles_QueryCountMatches()
    {
        using var world = new World();
        Span<Entity> emptyBatch = stackalloc Entity[5];
        world.CreateEntity(emptyBatch);

        world.CreateEntity(new TestPosition { X = 1, Y = 0 });
        world.CreateEntity(new TestPosition { X = 2, Y = 0 });

        Assert.Equal(2, world.Query<TestPosition>().Count);
    }

    [Fact]
    public void DestroyEntity_ReadOnlySpan_RemovesAllFromQuery()
    {
        using var world = new World();
        var a = world.CreateEntity(new TestPosition());
        var b = world.CreateEntity(new TestPosition());
        var c = world.CreateEntity(new TestPosition());
        Assert.Equal(3, world.Query<TestPosition>().Count);

        ReadOnlySpan<Entity> toKill = stackalloc Entity[] { a, b, c };
        world.DestroyEntity(toKill);

        Assert.Equal(0, world.Query<TestPosition>().Count);
        Assert.False(world.EManager.IsAlive(a));
        Assert.False(world.EManager.IsAlive(b));
        Assert.False(world.EManager.IsAlive(c));
    }
}
