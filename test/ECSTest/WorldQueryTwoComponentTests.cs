using ECSTest.Components;
using ExtenderApp.ECS;
using ExtenderApp.ECS.Accessors;
using ExtenderApp.ECS.Queries;
using Xunit;

namespace ECSTest;

/// <summary>
/// 双组件 <see cref="EntityQuery{T1,T2}"/> 的计数与按行修改。
/// </summary>
public sealed class WorldQueryTwoComponentTests
{
    [Fact]
    public void Query_TwoComponents_CountAndMutation()
    {
        using var world = new World();
        world.CreateEntity(
            new TestPosition { X = 1, Y = 2 },
            new TestVelocity { Dx = 10, Dy = 20 });
        world.CreateEntity(
            new TestPosition { X = 3, Y = 4 },
            new TestVelocity { Dx = -1, Dy = 0 });

        var q = world.Query<TestPosition, TestVelocity>();
        Assert.Equal(2, q.Count);

        foreach (EntityQueryRow<TestPosition, TestVelocity> row in q)
        {
            row.Deconstruct(out RefRW<TestPosition> p, out RefRW<TestVelocity> v);
            p.Value.X += 100;
            v.Value.Dx += 1;
        }

        var sumX = 0f;
        var sumDx = 0f;
        foreach (EntityQueryRow<TestPosition, TestVelocity> row in world.Query<TestPosition, TestVelocity>())
        {
            row.Deconstruct(out RefRW<TestPosition> p, out RefRW<TestVelocity> v);
            sumX += p.Value.X;
            sumDx += v.Value.Dx;
        }

        Assert.Equal(1f + 100 + 3f + 100, sumX);
        Assert.Equal(11f + 0f, sumDx);
    }
}
