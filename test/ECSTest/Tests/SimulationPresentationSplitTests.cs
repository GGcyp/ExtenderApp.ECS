using ExtenderApp.ECS;
using ECSTest.WorldTests;
using Xunit;

namespace ECSTest.Tests;

/// <summary>
/// <see cref="World.UpdateSimulation"/> / <see cref="World.UpdatePresentation"/> 与整帧 <see cref="World.Update"/> 的调度语义。
/// </summary>
public sealed class SimulationPresentationSplitTests : EcsTestContext
{
    /// <summary>
    /// 多次模拟步只跑非渲染组；单次呈现步只跑渲染组；呈现步不额外递增 FrameIndex。
    /// </summary>
    [Fact]
    public void World_多次模拟后单次呈现_调用次数与帧序号符合约定()
    {
        SplitScheduleSimulationCounterSystem.Reset();
        SplitSchedulePresentationCounterSystem.Reset();

        const float simDt = 0.02f;
        const float presDt = 1f / 60f;
        const int simSteps = 3;

        using var world = new World("Test_SplitSchedule");
        world.AddDefaultFrameSystem<SplitScheduleSimulationCounterSystem>();
        world.AddRenderingFrameSystem<SplitSchedulePresentationCounterSystem>();
        world.InitializeSystems();
        world.StartSystems();

        for (int i = 0; i < simSteps; i++)
            world.UpdateSimulation(simDt);

        Assert.Equal(simSteps, SplitScheduleSimulationCounterSystem.UpdateCount);
        Assert.Equal(0, SplitSchedulePresentationCounterSystem.UpdateCount);
        Assert.Equal(2u, SplitScheduleSimulationCounterSystem.LastSeenFrameIndex);

        world.UpdatePresentation(presDt);

        Assert.Equal(simSteps, SplitScheduleSimulationCounterSystem.UpdateCount);
        Assert.Equal(1, SplitSchedulePresentationCounterSystem.UpdateCount);
        Assert.Equal(3u, SplitSchedulePresentationCounterSystem.LastSeenFrameIndex);
    }

    /// <summary>
    /// 整帧 <see cref="World.Update"/> 仍按组顺序各执行一次，与「一次模拟 + 一次呈现」的计数一致。
    /// </summary>
    [Fact]
    public void World_整帧Update_默认组与渲染组各执行一次()
    {
        SplitScheduleSimulationCounterSystem.Reset();
        SplitSchedulePresentationCounterSystem.Reset();

        const float dt = 1f / 60f;

        using var world = new World("Test_FullUpdate");
        world.AddDefaultFrameSystem<SplitScheduleSimulationCounterSystem>();
        world.AddRenderingFrameSystem<SplitSchedulePresentationCounterSystem>();
        world.InitializeSystems();
        world.StartSystems();

        world.Update(dt);

        Assert.Equal(1, SplitScheduleSimulationCounterSystem.UpdateCount);
        Assert.Equal(1, SplitSchedulePresentationCounterSystem.UpdateCount);
        Assert.Equal(0u, SplitScheduleSimulationCounterSystem.LastSeenFrameIndex);
        Assert.Equal(0u, SplitSchedulePresentationCounterSystem.LastSeenFrameIndex);
    }

    /// <summary>
    /// 一次 <see cref="World.UpdateSimulation"/> 加一次 <see cref="World.UpdatePresentation"/> 与一次整帧 Update 对各组调用次数相同。
    /// </summary>
    [Fact]
    public void World_一次模拟加一次呈现_与整帧Update_各组调用次数一致()
    {
        const float dt = 1f / 60f;

        SplitScheduleSimulationCounterSystem.Reset();
        SplitSchedulePresentationCounterSystem.Reset();
        using (var world = new World("Test_SplitPair"))
        {
            world.AddDefaultFrameSystem<SplitScheduleSimulationCounterSystem>();
            world.AddRenderingFrameSystem<SplitSchedulePresentationCounterSystem>();
            world.InitializeSystems();
            world.StartSystems();
            world.UpdateSimulation(dt);
            world.UpdatePresentation(dt);
        }

        int simSplit = SplitScheduleSimulationCounterSystem.UpdateCount;
        int presSplit = SplitSchedulePresentationCounterSystem.UpdateCount;

        SplitScheduleSimulationCounterSystem.Reset();
        SplitSchedulePresentationCounterSystem.Reset();
        using (var world = new World("Test_Full"))
        {
            world.AddDefaultFrameSystem<SplitScheduleSimulationCounterSystem>();
            world.AddRenderingFrameSystem<SplitSchedulePresentationCounterSystem>();
            world.InitializeSystems();
            world.StartSystems();
            world.Update(dt);
        }

        Assert.Equal(simSplit, SplitScheduleSimulationCounterSystem.UpdateCount);
        Assert.Equal(presSplit, SplitSchedulePresentationCounterSystem.UpdateCount);
        Assert.Equal(1, simSplit);
        Assert.Equal(1, presSplit);
    }
}
