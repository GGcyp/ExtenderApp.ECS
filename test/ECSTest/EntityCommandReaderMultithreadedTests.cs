using ECSTest.Components;
using ExtenderApp.ECS;
using ExtenderApp.ECS.Accessors;
using ExtenderApp.ECS.Entities;
using ExtenderApp.ECS.Queries;
using Xunit;

namespace ECSTest;

/// <summary>
/// 校验多线程向 <see cref="EntityCommandBuffer"/> 追加命令后，由 <see cref="World.PlaybackRecordedCommands"/>（内部 <c>EntityCommandReader.ReadCommands</c>）在主线程单次回放的正确性。
/// </summary>
public sealed class EntityCommandReaderMultithreadedTests
{
    private const int ThreadCount = 8;
    private const int IterationsPerThread = 128;

    /// <summary>
    /// 单线程：虚拟实体连续两条 Add（位置 + 速度），回放后实体数与数据与预期一致。
    /// </summary>
    [Fact]
    public void Playback_VirtualEntity_TwoAddsSequential_Baseline()
    {
        using var world = new World();
        var buf = world.CommandBuffer;
        const int n = 80;
        for (int i = 0; i < n; i++)
        {
            var v = buf.CreateEntity();
            buf.AddComponent(v, new TestPosition { X = i, Y = -1 });
            buf.AddComponent(v, new TestVelocity { Dx = 7, Dy = 8 });
        }

        world.PlaybackRecordedCommands();

        Assert.Equal(n, world.Query<TestPosition, TestVelocity>().Count);
        float sumX = 0;
        foreach (EntityQueryRow<TestPosition, TestVelocity> row in world.Query<TestPosition, TestVelocity>())
        {
            row.Deconstruct(out RefRW<TestPosition> p, out RefRW<TestVelocity> vel);
            sumX += p.Value.X;
            Assert.Equal(-1f, p.Value.Y);
            Assert.Equal(7f, vel.Value.Dx);
            Assert.Equal(8f, vel.Value.Dy);
        }

        Assert.Equal(n * (n - 1) / 2f, sumX);
    }

    /// <summary>
    /// 单线程：主线程创建仅含 <see cref="TestPosition"/> 的真实实体，再通过缓冲逐条 Add <see cref="TestVelocity"/>，回放后与实体一一对应。
    /// </summary>
    [Fact]
    public void Playback_RealEntity_BufferAddVelocitySequential_Baseline()
    {
        using var world = new World();
        var buf = world.CommandBuffer;
        const int n = 100;
        var entities = new Entity[n];
        for (int i = 0; i < n; i++)
            entities[i] = world.CreateEntity(new TestPosition { X = i, Y = i });

        for (int i = 0; i < n; i++)
            buf.AddComponent(entities[i], new TestVelocity { Dx = i, Dy = -i });

        world.PlaybackRecordedCommands();

        for (int i = 0; i < n; i++)
        {
            var v = world.GetComponent<TestVelocity>(entities[i]);
            Assert.Equal(i, v.Dx);
            Assert.Equal(-i, v.Dy);
        }
    }

    /// <summary>
    /// 多线程：对「同一虚拟实体」的创建与两条 Add 必须作为连续命令写入缓冲；否则回放切换实体时会提前 Flush，导致掩码与数据错位。
    /// 此处用细粒度锁保证每条虚拟实体上的命令块不被其它线程拆开；不同实体之间仍由多线程交错产生。
    /// </summary>
    [Fact]
    public void Playback_VirtualEntity_ParallelWriters_SlotsAndPayloadsConsistent()
    {
        using var world = new World();
        var buf = world.CommandBuffer;
        int total = ThreadCount * IterationsPerThread;
        var virtualEntityWriteLock = new object();

        Parallel.For(0, ThreadCount, t =>
        {
            for (int i = 0; i < IterationsPerThread; i++)
            {
                int slot = t * IterationsPerThread + i;
                lock (virtualEntityWriteLock)
                {
                    var v = buf.CreateEntity();
                    buf.AddComponent(v, new TestPosition { X = slot, Y = t });
                    buf.AddComponent(v, new TestVelocity { Dx = t, Dy = i });
                }
            }
        });

        world.PlaybackRecordedCommands();

        Assert.Equal(total, world.Query<TestPosition, TestVelocity>().Count);

        var slotHits = new int[total];
        var entities = new HashSet<Entity>();

        foreach (EntityQueryRow<TestPosition, TestVelocity> row in world.Query<TestPosition, TestVelocity>())
        {
            row.Deconstruct(out RefRW<TestPosition> p, out RefRW<TestVelocity> vel, out Entity entity);
            Assert.True(entities.Add(entity), "查询结果中出现重复实体行。");

            int t = (int)p.Value.Y;
            int i = (int)vel.Value.Dy;
            int slot = (int)p.Value.X;

            Assert.Equal(t, (int)vel.Value.Dx);
            Assert.Equal(t * IterationsPerThread + i, slot);
            Assert.InRange(slot, 0, total - 1);
            slotHits[slot]++;
        }

        Assert.Equal(total, entities.Count);
        Assert.All(slotHits, c => Assert.Equal(1, c));
    }

    /// <summary>
    /// 多线程无锁：仅校验回放后双组件实体数量（不校验载荷）；用于暴露「完全无同步的多条命令 per 虚拟实体」是否与当前回放模型兼容。
    /// </summary>
    [Fact]
    public void Playback_VirtualEntity_ParallelWriters_NoLock_EntityCountOnly()
    {
        using var world = new World();
        var buf = world.CommandBuffer;
        int total = ThreadCount * IterationsPerThread;

        Parallel.For(0, ThreadCount, t =>
        {
            for (int i = 0; i < IterationsPerThread; i++)
            {
                var v = buf.CreateEntity();
                buf.AddComponent(v, new TestPosition { X = t, Y = i });
                buf.AddComponent(v, new TestVelocity { Dx = t, Dy = i });
            }
        });

        world.PlaybackRecordedCommands();

        Assert.Equal(total, world.Query<TestPosition, TestVelocity>().Count);
    }

    /// <summary>
    /// 多线程无锁：主线程先创建大量仅含位置的实体，并行向缓冲写入单条 Add 速度；回放后每个实体上的速度与下标一致。
    /// </summary>
    [Fact]
    public void Playback_RealEntity_ParallelBufferAdds_MatchesPerEntityPayload()
    {
        using var world = new World();
        var buf = world.CommandBuffer;
        int n = ThreadCount * IterationsPerThread;
        var entities = new Entity[n];
        for (int i = 0; i < n; i++)
            entities[i] = world.CreateEntity(new TestPosition { X = i, Y = 0 });

        Parallel.For(0, n, i =>
            buf.AddComponent(entities[i], new TestVelocity { Dx = i, Dy = i * 2 }));

        world.PlaybackRecordedCommands();

        for (int i = 0; i < n; i++)
        {
            var v = world.GetComponent<TestVelocity>(entities[i]);
            Assert.Equal(i, v.Dx);
            Assert.Equal(i * 2f, v.Dy);
        }
    }
}
