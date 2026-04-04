using ExtenderApp.ECS;
using ExtenderApp.ECS.Abstract;
using ExtenderApp.ECS.Systems;
using Xunit;

namespace ECSTest;

/// <summary>
/// <see cref="World.Update(float)"/> 与模拟/呈现拆分路径下，时间与帧序号在 <see cref="SystemUpdateContext"/> 中的可见性。
/// </summary>
public sealed class WorldUpdateSplitTests
{
    private sealed class SimulationClockCaptureSystem : ISystem
    {
        internal static int Invocations;
        internal static ulong LastFrameIndex;
        internal static double LastTime;
        internal static float LastDeltaTime;

        public static void Reset()
        {
            Invocations = 0;
            LastFrameIndex = 0;
            LastTime = 0;
            LastDeltaTime = 0;
        }

        public void OnCreate(ref SystemCreateContext createContext)
        {
        }

        public void OnStart()
        {
        }

        public void OnUpdate(ref SystemUpdateContext updateContext)
        {
            Invocations++;
            LastFrameIndex = updateContext.FrameIndex;
            LastTime = updateContext.Time;
            LastDeltaTime = updateContext.DeltaTime;
        }

        public void OnStop()
        {
        }

        public void OnDestroy()
        {
        }
    }

    private sealed class PresentationClockCaptureSystem : ISystem
    {
        internal static int Invocations;
        internal static ulong LastFrameIndex;
        internal static double LastTime;
        internal static float LastDeltaTime;

        public static void Reset()
        {
            Invocations = 0;
            LastFrameIndex = 0;
            LastTime = 0;
            LastDeltaTime = 0;
        }

        public void OnCreate(ref SystemCreateContext createContext)
        {
        }

        public void OnStart()
        {
        }

        public void OnUpdate(ref SystemUpdateContext updateContext)
        {
            Invocations++;
            LastFrameIndex = updateContext.FrameIndex;
            LastTime = updateContext.Time;
            LastDeltaTime = updateContext.DeltaTime;
        }

        public void OnStop()
        {
        }

        public void OnDestroy()
        {
        }
    }

    [Fact]
    public void Update_SimulationAndPresentationSeeSameFrameAndTimeInOneTick()
    {
        SimulationClockCaptureSystem.Reset();
        PresentationClockCaptureSystem.Reset();

        using var world = new World();
        world.AddDefaultFrameSystem<SimulationClockCaptureSystem>();
        world.AddRenderingFrameSystem<PresentationClockCaptureSystem>();
        world.InitializeSystems();
        world.StartSystems();

        world.Update(0.1f);

        Assert.Equal(1, SimulationClockCaptureSystem.Invocations);
        Assert.Equal(1, PresentationClockCaptureSystem.Invocations);
        Assert.Equal(0ul, SimulationClockCaptureSystem.LastFrameIndex);
        Assert.Equal(0ul, PresentationClockCaptureSystem.LastFrameIndex);
        Assert.Equal(0d, SimulationClockCaptureSystem.LastTime);
        Assert.Equal(0d, PresentationClockCaptureSystem.LastTime);
        Assert.Equal(0.1f, SimulationClockCaptureSystem.LastDeltaTime);
        Assert.Equal(0.1f, PresentationClockCaptureSystem.LastDeltaTime);

        world.StopSystems();
        world.DestroySystems();
    }

    [Fact]
    public void UpdateSimulationThenPresentation_PresentationSeesAdvancedFrameWithoutAdvancingAgain()
    {
        SimulationClockCaptureSystem.Reset();
        PresentationClockCaptureSystem.Reset();

        using var world = new World();
        world.AddDefaultFrameSystem<SimulationClockCaptureSystem>();
        world.AddRenderingFrameSystem<PresentationClockCaptureSystem>();
        world.InitializeSystems();
        world.StartSystems();

        world.UpdateSimulation(0.1f);
        Assert.Equal(1, SimulationClockCaptureSystem.Invocations);
        Assert.Equal(0, PresentationClockCaptureSystem.Invocations);
        Assert.Equal(0ul, SimulationClockCaptureSystem.LastFrameIndex);
        Assert.Equal(0d, SimulationClockCaptureSystem.LastTime);
        Assert.Equal(0.1f, SimulationClockCaptureSystem.LastDeltaTime);

        world.UpdatePresentation(0.02f);
        Assert.Equal(1, SimulationClockCaptureSystem.Invocations);
        Assert.Equal(1, PresentationClockCaptureSystem.Invocations);
        Assert.Equal(1ul, PresentationClockCaptureSystem.LastFrameIndex);
        Assert.Equal(0.1, PresentationClockCaptureSystem.LastTime, precision: 5);
        Assert.Equal(0.02f, PresentationClockCaptureSystem.LastDeltaTime);

        world.UpdateSimulation(0.05f);
        Assert.Equal(2, SimulationClockCaptureSystem.Invocations);
        Assert.Equal(1ul, SimulationClockCaptureSystem.LastFrameIndex);
        Assert.Equal(0.1, SimulationClockCaptureSystem.LastTime, precision: 5);
        Assert.Equal(0.05f, SimulationClockCaptureSystem.LastDeltaTime);

        world.StopSystems();
        world.DestroySystems();
    }
}
