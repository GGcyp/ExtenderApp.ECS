using ECSTest.Components;
using ExtenderApp.ECS;
using Xunit;

namespace ECSTest;

/// <summary>
/// 覆盖世界中共享组件的注册、读取与移除。
/// </summary>
public sealed class SharedComponentTests
{
    [Fact]
    public void SetSharedComponent_TryGet_RoundTrip()
    {
        using var world = new World();
        world.SetSharedComponent(new TestSharedConfig { Seed = 42 });

        Assert.True(world.TryGetSharedComponent<TestSharedConfig>(out var cfg));
        Assert.Equal(42, cfg.Seed);
    }

    [Fact]
    public void GetSharedComponent_WhenMissing_ThrowsKeyNotFound()
    {
        using var world = new World();
        Assert.Throws<KeyNotFoundException>(() => world.GetSharedComponent<TestSharedConfig>());
    }

    [Fact]
    public void TryAddSharedComponent_WhenExists_UpdatesValue()
    {
        using var world = new World();
        Assert.True(world.TryAddSharedComponent(new TestSharedConfig { Seed = 1 }));
        // 已存在时语义为覆盖写入（与内部 TrySet 一致），返回 true。
        Assert.True(world.TryAddSharedComponent(new TestSharedConfig { Seed = 2 }));
        Assert.Equal(2, world.GetSharedComponent<TestSharedConfig>().Seed);
    }

    [Fact]
    public void RemoveSharedComponent_WhenPresent_ReturnsTrue()
    {
        using var world = new World();
        world.SetSharedComponent(new TestSharedConfig { Seed = 7 });
        Assert.True(world.RemoveSharedComponent<TestSharedConfig>());
        Assert.False(world.HasSharedComponent<TestSharedConfig>());
    }
}
