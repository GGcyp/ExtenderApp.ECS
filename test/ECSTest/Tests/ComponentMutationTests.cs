using ECSTest.Components;
using ExtenderApp.ECS;
using Xunit;

namespace ECSTest.Tests;

/// <summary>
/// 组件增删写入对既有组件值影响的验证。
/// </summary>
public sealed class ComponentMutationTests : EcsTestContext
{
    /// <summary>
    /// 验证新增或移除组件不会影响已有组件值。
    /// </summary>
    [Fact]
    public void World_AddRemoveComponent_DoesNotAffectExistingValues()
    {
        // 测试用世界实例。
        using var world = new World("TestWorld_ComponentMutation");
        // 初始位置组件值。
        var initialPosition = new Position { X = 1f, Y = 2f };
        // 新建实体句柄。
        var entity = world.CreateEntity(initialPosition);
        // 写入的速度组件值。
        var velocity = new Velocity { Vx = 3f, Vy = 4f };

        world.AddComponent(entity, velocity);
        world.AddComponent<Health>(entity);
        world.AddComponent<PlayerTag>(entity);

        // 添加后读取的位置组件值。
        var positionAfterAdd = world.GetComponent<Position>(entity);
        Assert.Equal(initialPosition.X, positionAfterAdd.X);
        Assert.Equal(initialPosition.Y, positionAfterAdd.Y);

        world.RemoveComponent<Velocity>(entity);

        // 移除后读取的位置组件值。
        var positionAfterRemove = world.GetComponent<Position>(entity);
        Assert.Equal(initialPosition.X, positionAfterRemove.X);
        Assert.Equal(initialPosition.Y, positionAfterRemove.Y);
    }
}
