using ECSTest.Components;
using ExtenderApp.ECS;
using ExtenderApp.ECS.Accessors;
using ExtenderApp.ECS.Entities;
using ExtenderApp.ECS.Queries;
using Xunit;

namespace ECSTest.CustomRuns;

/// <summary>
/// 与 <see cref="CustomRunner.RunEntityQueryBuildTest"/> 等价的断言集合，供 xUnit 与控制台入口共用。
/// </summary>
public static class EntityQueryBuildVerification
{
    /// <summary>
    /// 校验 EntityQuery 构建、筛选、委托与 foreach 解构路径的正确性。
    /// </summary>
    public static void Verify()
    {
        using var world = new World();

        var entity = world.CreateEntity(new Position { X = 7, Y = -7 });

        var allPosition = world.Query<Position>();
        var withVelocity = world.QueryBuilder()
            .WithAll<Velocity>()
            .Build<Position>();
        var noVelocity = world.QueryBuilder()
            .WithNone<Velocity>()
            .Build<Position>();

        var p1 = ReadSinglePosition(allPosition, out var allCount1);
        var withCount1 = Count(withVelocity);
        var noneCount1 = Count(noVelocity);

        world.AddComponent(entity, new Velocity { Vx = 1, Vy = 2 });
        var p2 = ReadSinglePosition(allPosition, out var allCount2);
        var withCount2 = Count(withVelocity);
        var noneCount2 = Count(noVelocity);

        world.RemoveComponent<Velocity>(entity);
        var p3 = ReadSinglePosition(allPosition, out var allCount3);
        var withCount3 = Count(withVelocity);
        var noneCount3 = Count(noVelocity);

        Assert.True(p1.X == p2.X && p2.X == p3.X && p1.Y == p2.Y && p2.Y == p3.Y);
        Assert.True(allCount1 == 1 && allCount2 == 1 && allCount3 == 1);
        Assert.True(withCount1 == 0 && noneCount1 == 1
            && withCount2 == 1 && noneCount2 == 0
            && withCount3 == 0 && noneCount3 == 1);

        world.AddComponent(entity, new Velocity { Vx = 10, Vy = 20 });

        int delegateCount = 0;
        var moveQuery = world.Query<Position, Velocity>();
        moveQuery.Query((ref Position position, in Velocity velocity) =>
        {
            delegateCount++;
            position.X += velocity.Vx;
            position.Y += velocity.Vy;
        });

        var p4 = world.GetComponent<Position>(entity);
        Assert.Equal(1, delegateCount);
        Assert.Equal(17, p4.X);
        Assert.Equal(13, p4.Y);

        VerifyMultiComponentDelegate();
        VerifyForeachDeconstruct();
    }

    private static void VerifyMultiComponentDelegate()
    {
        using var world = new World();

        var entity = world.CreateEntity(
            new Position { X = 2, Y = 3 },
            new Velocity { Vx = 4, Vy = 5 },
            new Health { Value = 6 });

        _ = world.CreateEntity(
            new Position { X = 100, Y = 200 },
            new Velocity { Vx = 1, Vy = 1 });

        int count = 0;
        var query = world.Query<Health, Position, Velocity>();

        query.Query((in Velocity velocity, in Health health, ref Position position) =>
        {
            count++;
            position.X += velocity.Vx + health.Value;
            position.Y += velocity.Vy - health.Value;
        });

        var result = world.GetComponent<Position>(entity);
        Assert.Equal(1, count);
        Assert.Equal(12, result.X);
        Assert.Equal(2, result.Y);
    }

    private static void VerifyForeachDeconstruct()
    {
        using var world = new World();

        var entity = world.CreateEntity(
            new Position { X = 2, Y = 3 },
            new Velocity { Vx = 4, Vy = 5 },
            new Health { Value = 6 });

        _ = world.CreateEntity(
            new Position { X = 100, Y = 200 },
            new Velocity { Vx = 1, Vy = 1 });

        var query = world.Query<Velocity, Health, Position>();
        var query1 = world.Query<Velocity, Position>();

        foreach ((Velocity v, Position p) in query1)
        {
            _ = v;
            _ = p;
        }

        int count1 = 0;
        float checksum = 0;
        Entity visitedEntity = Entity.Empty;
        foreach ((Velocity velocity, Health health, Position position, Entity rowEntity) in query)
        {
            count1++;
            visitedEntity = rowEntity;
            checksum += velocity.Vx + velocity.Vy + health.Value + position.X + position.Y;
        }

        int count2 = 0;
        foreach (var row in query)
        {
            row.DeconstructRefs(out RefRW<Velocity> velocity, out RefRW<Health> health, out RefRW<Position> position, out Entity rowEntity);
            count2++;
            Assert.Equal(entity, rowEntity);
            velocity.Value = new Velocity
            {
                Vx = velocity.Value.Vx + health.Value.Value,
                Vy = velocity.Value.Vy + position.Value.X
            };
        }

        var updatedVelocity = world.GetComponent<Velocity>(entity);
        Assert.Equal(1, count1);
        Assert.Equal(1, count2);
        Assert.Equal(entity, visitedEntity);
        Assert.Equal(20f, checksum);
        Assert.Equal(10, updatedVelocity.Vx);
        Assert.Equal(7, updatedVelocity.Vy);
    }

    private static Position ReadSinglePosition(EntityQuery<Position> query, out int count)
    {
        count = 0;
        Position value = default;
        foreach (Position item in query)
        {
            count++;
            value = item;
        }

        return value;
    }

    private static int Count(EntityQuery<Position> query)
    {
        int count = 0;
        foreach (var _ in query)
            count++;
        return count;
    }
}
