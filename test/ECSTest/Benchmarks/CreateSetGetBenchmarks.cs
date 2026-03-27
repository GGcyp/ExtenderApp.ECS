using System;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Order;
using ExtenderApp.ECS;
using ECSTest.Components;
using System.Collections.Generic;

[Orderer(SummaryOrderPolicy.FastestToSlowest)]
public class CreateSetGetBenchmarks
{
    [Params(1000, 10000, 100000)]
    public int N;

    private World world = null!;
    private ComponentMask mask;
    private List<Entity> entities = null!;
    private Entity[] batchEntities = null!;

    [GlobalSetup]
    public void Setup()
    {
        mask = new ComponentMask();
        mask.Add(ComponentType.Create<Position>());
        mask.Add(ComponentType.Create<Velocity>());
        mask.Add(ComponentType.Create<Health>());
        mask.Add(ComponentType.Create<Mana>());
        mask.Add(ComponentType.Create<Rotation>());
        mask.Add(ComponentType.Create<Scale>());
        mask.Add(ComponentType.Create<Acceleration>());
        mask.Add(ComponentType.Create<Team>());
        mask.Add(ComponentType.Create<State>());
        mask.Add(ComponentType.Create<HugePayload>());
        batchEntities = new Entity[N];
    }

    [IterationSetup(Target = nameof(CreateEntities))]
    public void IterationSetupForCreate()
        => RebuildWorld(createEntities: false, fillComponents: false, withPlayerTag: false);

    [IterationSetup(Target = nameof(BatchCreateEntities))]
    public void IterationSetupForBatchCreate()
        => RebuildWorld(createEntities: false, fillComponents: false, withPlayerTag: false);

    [IterationSetup(Targets = [nameof(SetComponents)])]
    public void IterationSetupForSet()
        => RebuildWorld(createEntities: true, fillComponents: false, withPlayerTag: false);

    [IterationSetup(Targets = [nameof(GetComponents), nameof(QueryPositionCount), nameof(QueryPositionVelocityCount), nameof(QueryPositionVelocityHealthCount), nameof(QueryPositionVelocityHealthManaCount), nameof(QueryPositionVelocityHealthManaRotationCount), nameof(QueryBuilderPositionCount)])]
    public void IterationSetupForReadAndQuery()
        => RebuildWorld(createEntities: true, fillComponents: true, withPlayerTag: false);

    [IterationSetup(Target = nameof(ConvertArchetypeAddPlayerTag))]
    public void IterationSetupForConvertAdd()
        => RebuildWorld(createEntities: true, fillComponents: true, withPlayerTag: false);

    [IterationSetup(Target = nameof(ConvertArchetypeRemovePlayerTag))]
    public void IterationSetupForConvertRemove()
        => RebuildWorld(createEntities: true, fillComponents: true, withPlayerTag: true);

    [IterationSetup(Targets = [nameof(DestroyEntities), nameof(BatchDestroyEntities)])]
    public void IterationSetupForDestroy()
        => RebuildWorld(createEntities: true, fillComponents: false, withPlayerTag: false, createWithMask: true);

    [IterationSetup(Target = nameof(ComponentCrudPosition))]
    public void IterationSetupForComponentCrud()
        => RebuildWorld(createEntities: true, fillComponents: false, withPlayerTag: false, createWithMask: false);

    [IterationCleanup]
    public void IterationCleanup()
    {
        world.Dispose();
        entities.Clear();
        Array.Clear(batchEntities, 0, batchEntities.Length);
    }

    [Benchmark]
    public void CreateEntities()
    {
        entities.Clear();
        for (int i = 0; i < N; i++)
        {
            entities.Add(world.CreateEntity(mask));
        }
    }

    [Benchmark]
    public void BatchCreateEntities()
    {
        world.CreateEntity(batchEntities.AsSpan());
    }

    [Benchmark]
    public void SetComponents()
    {
        for (int i = 0; i < entities.Count; i++)
        {
            world.SetComponent(entities[i], new Position { X = i, Y = -i });
            world.SetComponent(entities[i], new Velocity { Vx = i * 0.001f, Vy = i * 0.002f });
            world.SetComponent(entities[i], new Health { Value = i });
            world.SetComponent(entities[i], new Mana { Value = i * 2 });
            world.SetComponent(entities[i], new Rotation { Value = i * 0.01f });
            world.SetComponent(entities[i], new Scale { Value = 1f + i * 0.001f });
            world.SetComponent(entities[i], new Acceleration { X = i * 0.003f, Y = i * 0.004f });
            world.SetComponent(entities[i], new Team { Id = i % 8 });
            world.SetComponent(entities[i], new State { Value = i % 5 });
            world.SetComponent(entities[i], new HugePayload { Seed = i });
        }
    }

    [Benchmark]
    public double GetComponents()
    {
        double sum = 0;
        for (int i = 0; i < entities.Count; i++)
        {
            var p = world.GetComponent<Position>(entities[i]);
            var v = world.GetComponent<Velocity>(entities[i]);
            var h = world.GetComponent<Health>(entities[i]);
            var m = world.GetComponent<Mana>(entities[i]);
            var r = world.GetComponent<Rotation>(entities[i]);
            var s = world.GetComponent<Scale>(entities[i]);
            var a = world.GetComponent<Acceleration>(entities[i]);
            var t = world.GetComponent<Team>(entities[i]);
            var st = world.GetComponent<State>(entities[i]);
            var huge = world.GetComponent<HugePayload>(entities[i]);
            sum += p.X + v.Vx + h.Value + m.Value + r.Value + s.Value + a.X + t.Id + st.Value + huge.Seed;
        }

        return sum;
    }

    [Benchmark]
    public int QueryPositionCount()
    {
        int count = 0;
        foreach (var _ in world.Query<Position>())
        {
            count++;
        }

        return count;
    }

    [Benchmark]
    public int QueryPositionVelocityCount()
    {
        int count = 0;
        foreach ((Position _, Velocity __) in world.Query<Position, Velocity>())
        {
            count++;
        }

        return count;
    }

    [Benchmark]
    public int QueryPositionVelocityHealthCount()
    {
        int count = 0;
        foreach ((Position _, Velocity __, Health ___) in world.Query<Position, Velocity, Health>())
        {
            count++;
        }

        return count;
    }

    [Benchmark]
    public int QueryPositionVelocityHealthManaCount()
    {
        int count = 0;
        foreach ((Position _, Velocity __, Health ___, Mana ____) in world.Query<Position, Velocity, Health, Mana>())
        {
            count++;
        }

        return count;
    }

    [Benchmark]
    public int QueryPositionVelocityHealthManaRotationCount()
    {
        int count = 0;
        foreach ((Position _, Velocity __, Health ___, Mana ____, Rotation _____) in world.Query<Position, Velocity, Health, Mana, Rotation>())
        {
            count++;
        }

        return count;
    }

    [Benchmark]
    public int QueryBuilderPositionCount()
    {
        var query = world.QueryBuilder()
            .WithAll<Position>()
            .WithNone<PlayerTag>()
            .Build<Position>();

        int count = 0;
        foreach (var _ in query)
        {
            count++;
        }

        return count;
    }

    [Benchmark]
    public void ConvertArchetypeAddPlayerTag()
    {
        for (int i = 0; i < entities.Count; i++)
        {
            world.AddComponent<PlayerTag>(entities[i]);
        }
    }

    [Benchmark]
    public void ConvertArchetypeRemovePlayerTag()
    {
        for (int i = 0; i < entities.Count; i++)
        {
            world.RemoveComponent<PlayerTag>(entities[i]);
        }
    }

    [Benchmark]
    public void DestroyEntities()
    {
        // 倒序删除可避免尾部交换影响后续待删实体索引映射
        for (int i = 0; i < entities.Count; i++)
        {
            world.DestroyEntity(entities[i]);
        }
    }

    [Benchmark]
    public void BatchDestroyEntities()
    {
        if (entities.Count == 0)
            return;

        // 批量删除同样使用倒序输入，减少交换导致的索引失配风险
        Entity[] buffer = entities.ToArray();
        Array.Reverse(buffer);
        world.DestroyEntity(buffer.AsSpan());
    }

    [Benchmark]
    public double ComponentCrudPosition()
    {
        double sum = 0;
        for (int i = 0; i < entities.Count; i++)
        {
            Entity entity = entities[i];
            world.AddComponent(entity, new Position { X = i, Y = -i });
            world.SetComponent(entity, new Position { X = i + 1, Y = -i - 1 });
            var p = world.GetComponent<Position>(entity);
            sum += p.X + p.Y;
            world.RemoveComponent<Position>(entity);
        }

        return sum;
    }

    private void RebuildWorld(bool createEntities, bool fillComponents, bool withPlayerTag, bool createWithMask = true)
    {
        if (world != null)
        {
            world.Dispose();
        }

        world = new World();
        entities = new List<Entity>(N);

        if (!createEntities)
            return;

        for (int i = 0; i < N; i++)
        {
            entities.Add(createWithMask ? world.CreateEntity(mask) : world.CreateEntity());
        }

        if (fillComponents)
        {
            for (int i = 0; i < entities.Count; i++)
            {
                world.SetComponent(entities[i], new Position { X = i, Y = -i });
                world.SetComponent(entities[i], new Velocity { Vx = i * 0.001f, Vy = i * 0.002f });
                world.SetComponent(entities[i], new Health { Value = i });
                world.SetComponent(entities[i], new Mana { Value = i * 2 });
                world.SetComponent(entities[i], new Rotation { Value = i * 0.01f });
                world.SetComponent(entities[i], new Scale { Value = 1f + i * 0.001f });
                world.SetComponent(entities[i], new Acceleration { X = i * 0.003f, Y = i * 0.004f });
                world.SetComponent(entities[i], new Team { Id = i % 8 });
                world.SetComponent(entities[i], new State { Value = i % 5 });
                world.SetComponent(entities[i], new HugePayload { Seed = i });
            }
        }

        if (withPlayerTag)
        {
            for (int i = 0; i < entities.Count; i++)
            {
                world.AddComponent<PlayerTag>(entities[i]);
            }
        }
    }
}