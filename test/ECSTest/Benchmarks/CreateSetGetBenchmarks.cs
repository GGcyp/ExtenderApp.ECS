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

    private World world;
    private ComponentMask mask;
    private List<Entity> entities;

    [GlobalSetup]
    public void Setup()
    {
        world = new World();
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
        entities = new List<Entity>(N);
    }

    [GlobalCleanup]
    public void Cleanup()
    {
        world.Dispose();
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
    public void SetComponents()
    {
        if (entities.Count == 0) CreateEntities();
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
    public void GetComponents()
    {
        if (entities.Count == 0) CreateEntities();
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
    }
}
