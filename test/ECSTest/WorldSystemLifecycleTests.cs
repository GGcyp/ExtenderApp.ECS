using ExtenderApp.ECS;
using ExtenderApp.ECS.Abstract;
using ExtenderApp.ECS.Systems;
using Xunit;

namespace ECSTest;

/// <summary>
/// 最小系统生命周期与 <see cref="World.Update(float)"/> 驱动路径。
/// </summary>
public sealed class WorldSystemLifecycleTests
{
    private sealed class CountingSystem : ISystem
    {
        /// <summary>供测试断言：在禁用并行集合下于每则测试开头清零。</summary>
        internal static int Updates;

        public void OnCreate(ref SystemCreateContext createContext)
        {
        }

        public void OnStart()
        {
        }

        public void OnUpdate(ref SystemUpdateContext updateContext)
        {
            Updates++;
        }

        public void OnStop()
        {
        }

        public void OnDestroy()
        {
        }
    }

    [Fact]
    public void Initialize_Start_Update_RunsOnUpdate()
    {
        CountingSystem.Updates = 0;
        using var world = new World();
        world.AddDefaultFrameSystem<CountingSystem>();
        world.InitializeSystems();
        world.StartSystems();

        world.Update(1f / 60f);

        Assert.Equal(1, CountingSystem.Updates);

        world.StopSystems();
        world.DestroySystems();
    }
}
