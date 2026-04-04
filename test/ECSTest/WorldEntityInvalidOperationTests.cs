using ECSTest.Components;
using ExtenderApp.ECS;
using Xunit;

namespace ECSTest;

/// <summary>
/// 已销毁实体上调用组件 API 时的稳定失败语义（钉死当前实现，便于回归）。
/// 解析 Archetype 失败时由 <c>EntityComponentOperation.TryGetArchetype</c> 抛出，文案为「无法获取实体组件原型」。
/// </summary>
public sealed class WorldEntityInvalidOperationTests
{
    [Fact]
    public void GetComponent_AfterDestroy_ThrowsInvalidOperation()
    {
        using var world = new World();
        var e = world.CreateEntity(new TestPosition());
        world.DestroyEntity(e);

        var ex = Assert.Throws<InvalidOperationException>(() => world.GetComponent<TestPosition>(e));
        Assert.StartsWith("无法获取实体组件原型", ex.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void SetComponent_AfterDestroy_ThrowsInvalidOperation()
    {
        using var world = new World();
        var e = world.CreateEntity(new TestPosition());
        world.DestroyEntity(e);

        var ex = Assert.Throws<InvalidOperationException>(() =>
            world.SetComponent(e, new TestPosition { X = 1, Y = 1 }));
        Assert.StartsWith("无法获取实体组件原型", ex.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void AddComponent_AfterDestroy_ThrowsInvalidOperation()
    {
        using var world = new World();
        var e = world.CreateEntity(new TestPosition());
        world.DestroyEntity(e);

        var ex = Assert.Throws<InvalidOperationException>(() =>
            world.AddComponent(e, new TestVelocity()));
        Assert.StartsWith("无法获取实体组件原型", ex.Message, StringComparison.Ordinal);
    }
}
