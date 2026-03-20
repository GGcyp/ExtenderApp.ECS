using System;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Order;
using ExtenderApp.ECS;
using ECSTest.Components;
using System.Collections.Generic;

[Orderer(SummaryOrderPolicy.FastestToSlowest)]
public class TransferBenchmarks
{
    [Params(1000, 10000, 100000)]
    public int N;

    private World world;
    private List<Entity> entitiesA;
    private List<Entity> entitiesB;

    [GlobalSetup]
    public void Setup()
    {
        world = new World();
        entitiesA = new List<Entity>(N);
        entitiesB = new List<Entity>(N);

        for (int i = 0; i < N; i++)
        {
            var a = world.CreateEntity();
            var b = world.CreateEntity();
            entitiesA.Add(a);
            entitiesB.Add(b);
            world.AddComponent(a, new Position { X = i, Y = -i });
            world.AddComponent(b, new Velocity { Vx = i * 0.1f, Vy = i * 0.2f });
        }
    }

    [GlobalCleanup]
    public void Cleanup()
    {
        world.Dispose();
    }

    [Benchmark]
    public void TransferPosition()
    {
        for (int i = 0; i < N; i++)
        {
            var p = world.GetComponent<Position>(entitiesA[i]);
            world.RemoveComponent<Position>(entitiesA[i]);
            world.AddComponent(entitiesB[i], p);
        }
    }
}
