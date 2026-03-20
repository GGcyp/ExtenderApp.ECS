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
            sum += p.X + v.Vx;
        }
    }
}
