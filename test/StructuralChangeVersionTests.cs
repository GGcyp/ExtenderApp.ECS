using ECSTest.Components;
using ExtenderApp.ECS;
using Xunit;

namespace ECSTest;

/// <summary>
/// 覆盖世界级结构变更版本（StructureVersion）：
/// 实体创建/销毁、组件增删迁移递增；纯组件数据写入不递增；无效操作不递增；命令回放递增。
/// 约定：版本单调递增，消费者只做「变了没变」比较，不依赖具体步长。
/// </summary>
public sealed class StructuralChangeVersionTests
{
    private static ulong Version(World world) => world.SVManager.StructureVersion;

    [Fact]
    public void CreateEntity_WithComponents_Increments()
    {
        using var world = new World();
        ulong before = Version(world);

        world.CreateEntity(new TestPosition());

        Assert.Equal(before + 1, Version(world));
    }

    [Fact]
    public void CreateEntity_WithoutComponents_DoesNotIncrement()
    {
        using var world = new World();
        ulong before = Version(world);

        world.CreateEntity();

        // 空实体不经过 Archetype，不属于结构版本覆盖范围
        Assert.Equal(before, Version(world));
    }

    [Fact]
    public void BatchCreateEntity_IncrementsByCount()
    {
        using var world = new World();
        var archetype = world.AManager.GetOrCreateArchetype(new ComponentMask(ComponentType.Create<TestPosition>()));
        Span<Entity> entities = stackalloc Entity[4];

        ulong before = Version(world);
        world.EManager.CreateEntity(entities, archetype);

        Assert.Equal(before + 4, Version(world));
    }

    [Fact]
    public void DestroyEntity_Increments()
    {
        using var world = new World();
        var entity = world.CreateEntity(new TestPosition());
        ulong before = Version(world);

        world.DestroyEntity(entity);

        Assert.Equal(before + 1, Version(world));
    }

    [Fact]
    public void DestroyEntity_Invalid_DoesNotIncrement()
    {
        using var world = new World();
        world.CreateEntity(new TestPosition());
        ulong before = Version(world);

        world.DestroyEntity(Entity.Empty);

        Assert.Equal(before, Version(world));
    }

    [Fact]
    public void AddComponent_Migration_IncrementsOnce()
    {
        using var world = new World();
        var entity = world.CreateEntity(new TestPosition());
        ulong before = Version(world);

        world.AddComponent<TestVelocity>(entity);

        // 一次迁移 = 1 次递增（目标槽位分配计一次，源侧移除不重复计）
        Assert.Equal(before + 1, Version(world));
    }

    [Fact]
    public void RemoveComponent_Migration_Increments()
    {
        using var world = new World();
        var entity = world.CreateEntity(new TestPosition(), new TestVelocity());
        ulong before = Version(world);

        world.RemoveComponent<TestVelocity>(entity);

        Assert.Equal(before + 1, Version(world));
    }

    [Fact]
    public void RemoveComponent_ToEmpty_Increments()
    {
        using var world = new World();
        var entity = world.CreateEntity(new TestPosition());
        ulong before = Version(world);

        world.RemoveComponent<TestPosition>(entity);

        Assert.Equal(before + 1, Version(world));
    }

    [Fact]
    public void SetComponent_DoesNotIncrement()
    {
        using var world = new World();
        var entity = world.CreateEntity(new TestPosition { X = 0 });
        ulong before = Version(world);

        world.SetComponent(entity, new TestPosition { X = 5 });

        Assert.Equal(before, Version(world));
    }

    [Fact]
    public void Playback_DestroyEntity_Increments()
    {
        using var world = new World();
        var entity = world.CreateEntity(new TestPosition());
        ulong before = Version(world);

        world.CommandBuffer.DestroyEntity(entity);
        Assert.Equal(before, Version(world)); // 回放前不递增

        world.PlaybackRecordedCommands();

        Assert.Equal(before + 1, Version(world));
    }

    [Fact]
    public void Playback_AddComponent_Increments()
    {
        using var world = new World();
        var entity = world.CreateEntity(new TestPosition());
        ulong before = Version(world);

        world.CommandBuffer.AddComponent<TestVelocity>(entity);
        world.PlaybackRecordedCommands();

        Assert.Equal(before + 1, Version(world));
    }
}
