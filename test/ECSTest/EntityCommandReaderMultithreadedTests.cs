using ECSTest.Components;
using ExtenderApp.ECS;
using ExtenderApp.ECS.Accessors;
using ExtenderApp.ECS.Entities;
using ExtenderApp.ECS.Queries;
using Xunit;

namespace ECSTest;

/// <summary>
/// <see cref="ExtenderApp.ECS.Commands.EntityCommandReader"/> 回放相关测试：虚拟/真实实体、顺序与并行写入。
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
    /// 单线程真实实体：多阶段增删组件与缓冲写入后回放，校验各组件数据与多次原型迁移一致性。
    /// </summary>
    [Fact]
    public void Playback_RealEntity_BufferAddVelocitySequential_Baseline()
    {
        using var world = new World();
        const int n = 100;
        var entities = new Entity[n];
        for (int i = 0; i < n; i++)
        {
            entities[i] = world.CreateEntity(new TestPosition { X = i, Y = i });
            world.AddComponent(entities[i], new TestEcsContext { Name = $"Entity_{i}", Id = i });
            world.RemoveComponent<TestPosition>(entities[i]);
            world.AddComponent(entities[i], new TestAgentContext { AgentId = i });
            world.AddComponent(entities[i], new TestBufferElement { Value = i });
            world.AddComponent(entities[i], new TestTag { Value = i });
        }

        for (int i = 0; i < n; i++)
        {
            world.AddComponent(entities[i], new TestVelocity { Dx = i, Dy = -i });
            world.RemoveComponent<TestPosition>(entities[i]);
            world.AddComponent(entities[i], new TestAgentContext { AgentId = i * 2 });
        }

        world.PlaybackRecordedCommands();

        for (int i = 0; i < n; i++)
        {
            var v = world.GetComponent<TestVelocity>(entities[i]);
            Assert.Equal(i, v.Dx);
            Assert.Equal(-i, v.Dy);
            var c = world.GetComponent<TestEcsContext>(entities[i]);
            Assert.Equal($"Entity_{i}", c.Name);
            Assert.Equal(i, c.Id);
            var a = world.GetComponent<TestAgentContext>(entities[i]);
            Assert.Equal(i * 2, a.AgentId);
            var b = world.GetComponent<TestBufferElement>(entities[i]);
            Assert.Equal(i, b.Value);
        }
    }

    /// <summary>
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