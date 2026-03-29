using System.Threading;
using ECSTest.Components;
using ExtenderApp.ECS;
using ExtenderApp.ECS.Queries;
using Xunit;

namespace ECSTest.CustomRuns;

/// <summary>
/// 命令缓冲、直调 World、按查询销毁与共享组件等路径的断言，供 xUnit 与 CustomRunner 共用。
/// </summary>
public static class CustomRunnerCommandsVerification
{
    /// <summary>
    /// 校验多线程向同一 <see cref="World.CommandBuffer"/> 写入后的回放结果。
    /// </summary>
    public static void VerifyCommandBufferConcurrency(int opsPerWriter)
    {
        using var world = new World();

        var buffer = world.CommandBuffer;

        int writers = 8;
        int ops = Math.Max(1, opsPerWriter);

        var smoke = world.CreateEntity();
        buffer.AddComponent(smoke, new Position { X = 1, Y = 2 });

        var precreated = new Entity[writers][];
        for (int w = 0; w < writers; w++)
        {
            precreated[w] = new Entity[ops];
            for (int i = 0; i < ops; i++)
                precreated[w][i] = world.CreateEntity();
        }

        var barrier = new Barrier(writers);
        var tasks = new Task[writers];

        for (int w = 0; w < writers; w++)
        {
            int writerIndex = w;
            tasks[w] = Task.Run(() =>
            {
                barrier.SignalAndWait();

                var localEntities = precreated[writerIndex];
                for (int i = 0; i < ops; i++)
                {
                    var e = localEntities[i];
                    buffer.AddComponent(e, new Position { X = i, Y = -i });
                    buffer.SetComponent(e, new Position { X = i + 1, Y = -i - 1 });
                    buffer.RemoveComponent<Position>(e);
                    buffer.DestroyEntity(e);
                }
            });
        }

        Task.WaitAll(tasks);

        world.InitializeSystems();
        world.StartSystems();
        world.Update(0f);

        int posCount = 0;
        foreach (var _ in world.Query<Position>())
            posCount++;

        var sp = world.GetComponent<Position>(smoke);
        Assert.Equal(1, posCount);
        Assert.Equal(1, sp.X);
        Assert.Equal(2, sp.Y);
    }

    /// <summary>
    /// 校验不经命令缓冲、直接在当前 World 上大量组件操作后的状态。
    /// </summary>
    public static void VerifyWorldDirectExecution(int opsPerWriter)
    {
        using var world = new World();

        int ops = Math.Max(1, opsPerWriter);

        var smoke = world.CreateEntity();
        world.AddComponent(smoke, new Position { X = 1, Y = 2 });

        for (int i = 0; i < ops; i++)
        {
            var e = world.CreateEntity();
            world.AddComponent(e, new Position { X = i, Y = -i });
            world.SetComponent(e, new Position { X = i + 1, Y = -i - 1 });
            world.AddComponent(e, new Velocity { Vx = i, Vy = -i });
            world.AddComponent(e, new Health { Value = i });
            world.AddComponent(e, new Rotation { Value = i });
            world.AddComponent(e, new Scale { Value = i });
            world.AddComponent<PlayerTag>(e);
            world.RemoveComponent<Position>(e);
            world.RemoveComponent<PlayerTag>(e);
            world.DestroyEntity(e);
        }

        int posCount = 0;
        foreach (var _ in world.Query<Position>())
            posCount++;

        var sp = world.GetComponent<Position>(smoke);
        Assert.Equal(1, posCount);
        Assert.Equal(1, sp.X);
        Assert.Equal(2, sp.Y);
    }

    /// <summary>
    /// 校验 <see cref="World.DestroyEntitiesForQuery"/> 与隐式 <see cref="EntityQuery"/> 转换。
    /// </summary>
    public static void VerifyDestroyEntitiesForQuery()
    {
        using var world = new World();

        int total = 1000;
        int withPos = 400;
        var entities = new List<Entity>(total);
        for (int i = 0; i < total; i++)
        {
            var e = world.CreateEntity();
            if (i < withPos)
                world.AddComponent(e, new Position { X = i, Y = i });
            entities.Add(e);
        }

        var typedQuery = world.Query<Position>();
        EntityQuery query = typedQuery;

        world.DestroyEntitiesForQuery(query);

        int posCount = 0;
        foreach (var _ in world.Query<Position>())
            posCount++;

        Assert.Equal(0, posCount);
    }

    /// <summary>
    /// 校验共享组件的增删改查路径。
    /// </summary>
    public static void VerifySharedComponentCrud()
    {
        using var world = new World();

        Assert.True(world.TryAddSharedComponent(new SharedSample { X = 10, Y = 20 }));

        // 与 SharedComponentManager.TryAdd 语义一致：已存在时 AddSharedComponent 会覆盖而非抛错。
        world.AddSharedComponent(new SharedSample { X = 1, Y = 2 });
        var afterAddDuplicate = world.GetSharedComponent<SharedSample>();
        Assert.Equal(1, afterAddDuplicate.X);
        Assert.Equal(2, afterAddDuplicate.Y);

        Assert.True(world.TryGetSharedComponent<SharedSample>(out var sample));
        Assert.Equal(1, sample.X);
        Assert.Equal(2, sample.Y);

        world.UpdateSharedComponent(new SharedSample { X = 100, Y = 200 });
        var updated = world.GetSharedComponent<SharedSample>();
        Assert.Equal(100, updated.X);
        Assert.Equal(200, updated.Y);

        Assert.True(world.TryUpdateSharedComponent(new SharedSample { X = 7, Y = 8 }));

        Assert.True(world.RemoveSharedComponent<SharedSample>());

        Assert.False(world.TryGetSharedComponent<SharedSample>(out _));
    }

    private struct SharedSample
    {
        public int X;
        public int Y;
    }

    /// <summary>
    /// 校验托管引用类型组件的创建与查询条数。
    /// </summary>
    public static void VerifyManagedUseData()
    {
        using var world = new World();

        const int n = 1000;
        for (int i = 0; i < n; i++)
            world.CreateEntity(new ManagedData { Name = i.ToString(), Level = i });

        int count = 0;
        foreach (var _ in world.Query<ManagedData>())
            count++;

        Assert.Equal(n, count);
    }

    private sealed class ManagedData
    {
        public string? Name { get; set; }
        public int Level { get; set; }
    }

    /// <summary>
    /// 校验批量创建带掩码实体后逐条销毁，世界最终无残留实体（通过计数推断）。
    /// </summary>
    public static void VerifyDestroyEntitiesBatch()
    {
        const int count = 2048;

        World world = new();
        List<Entity> entities = new(count);
        ComponentMask mask = new()
        {
            ComponentType.Create<Position>(),
            ComponentType.Create<Velocity>(),
            ComponentType.Create<PlayerTag>()
        };

        for (int i = 0; i < count; i++)
            entities.Add(world.CreateEntity(mask));

        foreach (var entity in entities)
            world.DestroyEntity(entity);

        Assert.Equal(0, world.Query<Position>().Count);
    }
}
