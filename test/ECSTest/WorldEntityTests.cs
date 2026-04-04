using ECSTest.Components;
using ExtenderApp.ECS;
using Xunit;

namespace ECSTest;

/// <summary>
/// 覆盖 <see cref="World"/> 的实体创建、组件读写与销毁行为。
/// </summary>
public sealed class WorldEntityTests
{
    [Fact]
    public void CreateEntity_Empty_IsAlive()
    {
        using var world = new World();
        var e = world.CreateEntity();
        Assert.True(world.EManager.IsAlive(e));
    }

    [Fact]
    public void CreateEntity_WithSingleComponent_RoundTripsGetComponent()
    {
        using var world = new World();
        var p = new TestPosition { X = 1, Y = 2 };
        var e = world.CreateEntity(p);

        var read = world.GetComponent<TestPosition>(e);
        Assert.Equal(1f, read.X);
        Assert.Equal(2f, read.Y);
    }

    [Fact]
    public void SetComponent_UpdatesValue()
    {
        using var world = new World();
        var e = world.CreateEntity(new TestPosition { X = 0, Y = 0 });
        world.SetComponent(e, new TestPosition { X = 10, Y = 20 });

        var read = world.GetComponent<TestPosition>(e);
        Assert.Equal(10f, read.X);
        Assert.Equal(20f, read.Y);
    }

    [Fact]
    public void CreateEntity_TwoComponents_BothReadable()
    {
        using var world = new World();
        var e = world.CreateEntity(
            new TestPosition { X = 3, Y = 4 },
            new TestVelocity { Dx = 1, Dy = -1 });

        var p = world.GetComponent<TestPosition>(e);
        var v = world.GetComponent<TestVelocity>(e);
        Assert.Equal(3f, p.X);
        Assert.Equal(-1f, v.Dy);
    }

    [Fact]
    public void AddComponent_MigratesArchetype()
    {
        using var world = new World();
        var e = world.CreateEntity(new TestPosition { X = 1, Y = 1 });
        world.AddComponent(e, new TestVelocity { Dx = 2, Dy = 3 });

        Assert.True(world.EManager.IsAlive(e));
        var v = world.GetComponent<TestVelocity>(e);
        Assert.Equal(2f, v.Dx);
    }

    [Fact]
    public void RemoveComponent_DropsComponent()
    {
        using var world = new World();
        var e = world.CreateEntity(
            new TestPosition { X = 0, Y = 0 },
            new TestVelocity { Dx = 1, Dy = 0 });

        world.RemoveComponent<TestVelocity>(e);

        var q = world.Query<TestPosition>();
        Assert.Equal(1, q.Count);
        _ = world.GetComponent<TestPosition>(e);
    }

    [Fact]
    public void DestroyEntity_NoLongerAlive()
    {
        using var world = new World();
        var e = world.CreateEntity(new TestPosition());
        world.DestroyEntity(e);
        Assert.False(world.EManager.IsAlive(e));
    }

    [Fact]
    public void CreateEntity_EmptyMask_Throws()
    {
        using var world = new World();
        Assert.Throws<ArgumentNullException>(() => world.CreateEntity(ComponentMask.Empty));
    }
}
