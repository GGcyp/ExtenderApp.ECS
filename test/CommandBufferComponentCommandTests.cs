using ECSTest.Components;
using ExtenderApp.ECS;
using Xunit;

namespace ECSTest;

/// <summary>
/// 命令缓冲中的组件类命令经 <see cref="World.PlaybackRecordedCommands"/> 回放后的行为。
/// </summary>
public sealed class CommandBufferComponentCommandTests
{
    [Fact]
    public void Playback_AddComponentWithData_AddsArchetypeColumn()
    {
        using var world = new World();
        var e = world.CreateEntity(new TestPosition { X = 1, Y = 2 });
        Assert.Equal(1, world.Query<TestPosition>().Count);
        Assert.Equal(0, world.Query<TestPosition, TestVelocity>().Count);

        world.CommandBuffer.AddComponent(e, new TestVelocity { Dx = 3, Dy = 4 });
        world.PlaybackRecordedCommands();

        Assert.Equal(1, world.Query<TestPosition, TestVelocity>().Count);
        var v = world.GetComponent<TestVelocity>(e);
        Assert.Equal(3f, v.Dx);
        Assert.Equal(4f, v.Dy);
    }

    [Fact]
    public void Playback_SetComponent_OverwritesExistingColumn()
    {
        using var world = new World();
        var e = world.CreateEntity(new TestPosition { X = 0, Y = 0 });
        world.CommandBuffer.SetComponent(e, new TestPosition { X = 9, Y = 8 });
        world.PlaybackRecordedCommands();

        var p = world.GetComponent<TestPosition>(e);
        Assert.Equal(9f, p.X);
        Assert.Equal(8f, p.Y);
    }

    [Fact]
    public void Playback_SetComponent_WhenMigratingArchetype_WritesNewColumn()
    {
        using var world = new World();
        var e = world.CreateEntity(new TestPosition { X = 1, Y = 2 });
        world.CommandBuffer.SetComponent(e, new TestVelocity { Dx = 5, Dy = 6 });
        world.PlaybackRecordedCommands();

        Assert.Equal(1, world.Query<TestPosition, TestVelocity>().Count);
        var v = world.GetComponent<TestVelocity>(e);
        Assert.Equal(5f, v.Dx);
        Assert.Equal(6f, v.Dy);
        var p = world.GetComponent<TestPosition>(e);
        Assert.Equal(1f, p.X);
    }

    [Fact]
    public void Playback_RemoveComponent_WhenResultingMaskEmpty_StripsEntityFromQuery()
    {
        using var world = new World();
        var e = world.CreateEntity(new TestPosition { X = 1, Y = 1 });

        world.CommandBuffer.RemoveComponent<TestPosition>(e);
        world.PlaybackRecordedCommands();

        Assert.Equal(0, world.Query<TestPosition>().Count);
    }

    [Fact]
    public void Playback_RemoveComponent_FromTwoComponentEntity_LeavesRemainingColumn()
    {
        using var world = new World();
        var e = world.CreateEntity(
            new TestPosition { X = 1, Y = 1 },
            new TestVelocity { Dx = 1, Dy = 0 });

        world.CommandBuffer.RemoveComponent<TestVelocity>(e);
        world.PlaybackRecordedCommands();

        Assert.Equal(0, world.Query<TestPosition, TestVelocity>().Count);
        Assert.Equal(1, world.Query<TestPosition>().Count);
        _ = world.GetComponent<TestPosition>(e);
    }

    [Fact]
    public void Playback_AddComponentWithoutPayload_UsesDefaultStruct()
    {
        using var world = new World();
        var e = world.CreateEntity(new TestPosition());
        world.CommandBuffer.AddComponent<TestVelocity>(e);
        world.PlaybackRecordedCommands();

        var v = world.GetComponent<TestVelocity>(e);
        Assert.Equal(0f, v.Dx);
        Assert.Equal(0f, v.Dy);
    }
}
