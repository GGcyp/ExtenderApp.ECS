using ECSTest.Components;
using ExtenderApp.ECS;
using ExtenderApp.ECS.Accessors;
using ExtenderApp.ECS.Queries;
using Xunit;

namespace ECSTest;

/// <summary>
/// 覆盖实体查询的计数、遍历与 <see cref="EntityQueryBuilder"/> 过滤条件。
/// </summary>
public sealed class WorldQueryTests
{
    [Fact]
    public void Query_Count_MatchesCreatedEntities()
    {
        using var world = new World();
        world.CreateEntity(new TestPosition { X = 0, Y = 0 });
        world.CreateEntity(new TestPosition { X = 1, Y = 1 });
        world.CreateEntity(new TestVelocity()); // 不含 Position

        var q = world.Query<TestPosition>();
        Assert.Equal(2, q.Count);
    }

    [Fact]
    public void Foreach_Query_ModifiesComponentViaRefRw()
    {
        using var world = new World();
        world.CreateEntity(new TestPosition { X = 1, Y = 2 });

        foreach (EntityQueryRow<TestPosition> row in world.Query<TestPosition>())
        {
            row.Deconstruct(out RefRW<TestPosition> pos);
            pos.Value.X += 5;
        }

        // 再取实体：单实体场景直接重建查询遍历取第一行
        foreach (EntityQueryRow<TestPosition> row in world.Query<TestPosition>())
        {
            row.Deconstruct(out RefRW<TestPosition> pos);
            Assert.Equal(6f, pos.Value.X);
            Assert.Equal(2f, pos.Value.Y);
        }
    }

    [Fact]
    public void QueryBuilder_WithNone_ExcludesArchetype()
    {
        using var world = new World();
        world.CreateEntity(new TestPosition { X = 0, Y = 0 });
        world.CreateEntity(
            new TestPosition { X = 1, Y = 1 },
            new TestVelocity { Dx = 0, Dy = 0 });

        EntityQuery<TestPosition> q = world.QueryBuilder()
            .WithAll<TestPosition>()
            .WithNone<TestVelocity>()
            .Build<TestPosition>();

        Assert.Equal(1, q.Count);
    }

    [Fact]
    public void DestroyEntitiesForQuery_RemovesMatches()
    {
        using var world = new World();
        world.CreateEntity(new TestPosition());
        world.CreateEntity(new TestPosition());
        var q = world.Query<TestPosition>();
        Assert.Equal(2, q.Count);

        world.DestroyEntitiesForQuery(q);

        Assert.Equal(0, world.Query<TestPosition>().Count);
    }
}
