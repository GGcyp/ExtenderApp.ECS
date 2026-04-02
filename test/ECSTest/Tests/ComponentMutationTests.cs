using ECSTest.Components;
using ExtenderApp.ECS;
using Xunit;

namespace ECSTest.Tests;

/// <summary>
/// 组件增删写入对已有值影响的验证。
/// </summary>
public sealed class ComponentMutationTests : EcsTestContext
{
    /// <summary>
    /// 验证新增和移除组件不会影响已有组件值。
    /// </summary>
    [Fact]
    public void World_AddRemoveComponent_DoesNotAffectExistingValues()
    {
        using var world = new World("TestWorld_ComponentMutation");
        var initialPosition = new Position { X = 1f, Y = 2f };
        var entity = world.CreateEntity(initialPosition);
        var velocity = new Velocity { Vx = 3f, Vy = 4f };

        world.AddComponent(entity, velocity);
        world.AddComponent<Health>(entity);
        world.AddComponent<PlayerTag>(entity);

        var positionAfterAdd = world.GetComponent<Position>(entity);
        Assert.Equal(initialPosition.X, positionAfterAdd.X);
        Assert.Equal(initialPosition.Y, positionAfterAdd.Y);

        world.RemoveComponent<Velocity>(entity);

        var positionAfterRemove = world.GetComponent<Position>(entity);
        Assert.Equal(initialPosition.X, positionAfterRemove.X);
        Assert.Equal(initialPosition.Y, positionAfterRemove.Y);
    }

    /// <summary>
    /// 验证移除中间组件后，其余组件仍按旧下标正确复制到新下标。
    /// </summary>
    [Fact]
    public void World_RemoveMiddleComponent_KeepsOtherComponentValuesCorrect()
    {
        using var world = new World("TestWorld_RemoveMiddleComponent");

        var entity = world.CreateEntity(
            new Position { X = 1f, Y = 2f },
            new Velocity { Vx = 3f, Vy = 4f },
            new Health { Value = 5 },
            new Mana { Value = 6 },
            new Rotation { Value = 7f });

        world.RemoveComponent<Health>(entity);

        var position = world.GetComponent<Position>(entity);
        var velocity = world.GetComponent<Velocity>(entity);
        var mana = world.GetComponent<Mana>(entity);
        var rotation = world.GetComponent<Rotation>(entity);

        Assert.Equal(1f, position.X);
        Assert.Equal(2f, position.Y);
        Assert.Equal(3f, velocity.Vx);
        Assert.Equal(4f, velocity.Vy);
        Assert.Equal(6, mana.Value);
        Assert.Equal(7f, rotation.Value);
        Assert.ThrowsAny<Exception>(() => world.GetComponent<Health>(entity));
    }
}
