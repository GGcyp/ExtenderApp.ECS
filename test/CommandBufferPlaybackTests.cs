using ECSTest.Components;
using ExtenderApp.ECS;
using Xunit;

namespace ECSTest;

/// <summary>
/// 校验命令缓冲写入与主线程回放后实体状态一致。
/// </summary>
public sealed class CommandBufferPlaybackTests
{
    [Fact]
    public void PlaybackRecordedCommands_DestroyEntity_Applies()
    {
        using var world = new World();
        var e = world.CreateEntity(new TestPosition());
        Assert.True(world.EManager.IsAlive(e));

        world.CommandBuffer.DestroyEntity(e);
        Assert.True(world.EManager.IsAlive(e));

        world.PlaybackRecordedCommands();
        Assert.False(world.EManager.IsAlive(e));
    }
}
