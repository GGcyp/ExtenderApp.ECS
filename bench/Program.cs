using System.Diagnostics;
using ExtenderApp.ECS;
using ExtenderApp.ECS.Entities;

namespace RelationBench;

// 每个场景独立的关系类型，避免不同组件场景的表互相污染查询结果
internal struct RelK1
{
}

internal struct RelK2
{
}

internal struct RelK4
{
}

internal struct RelK50
{
}

internal static class Program
{
    /// <summary>
    /// 1/2/4 组件场景的实体数。
    /// </summary>
    private const int NCommon = 1_000_000;

    /// <summary>
    /// 50 组件场景的实体数（列数多、迁移贵，实体数略降以控制单轮时长）。
    /// </summary>
    private const int NHeavy = 500_000;

    private static double Measure(Action action)
    {
        action(); // 预热
        double best = double.MaxValue;
        for (int r = 0; r < 5; r++)
        {
            var sw = Stopwatch.StartNew();
            action();
            sw.Stop();
            if (sw.Elapsed.TotalMilliseconds < best)
                best = sw.Elapsed.TotalMilliseconds;
        }
        return best;
    }

    private static void Report(string name, double ms, long ops) =>
        Console.WriteLine($"{name,-58} {ms,10:F2} ms   {ms * 1e6 / ops,8:F1} ns/op");

    private static void RunCase(string label, RelationType relType, int n, Func<World, int, Entity[]> createEntities)
    {
        using var world = new World();
        var entities = createEntities(world, n);
        var target = world.CreateEntity();
        var deadTarget = world.CreateEntity();

        Console.WriteLine($"--- {label} (N={n:N0}) ---");
        double ms;

        ms = Measure(() =>
        {
            for (int i = 0; i < n; i++)
                world.AddRelation(entities[i], relType, target);
        });
        Report("AddRelation 初始(类型位迁移)", ms, n);

        ms = Measure(() =>
        {
            for (int i = 0; i < n; i++)
                world.AddRelation(entities[i], relType, target);
        });
        Report("AddRelation 覆盖目标(原地写行)", ms, n);

        ms = Measure(() =>
        {
            for (int i = 0; i < n; i++)
                world.TryGetRelationTarget(entities[i], relType, out _);
        });
        Report("TryGetRelationTarget 读目标", ms, n);

        ms = Measure(() =>
        {
            for (int i = 0; i < n; i++)
                world.AddRelation(entities[i], relType, target);
            for (int i = 0; i < n; i++)
                world.RemoveRelation(entities[i], relType);
        });
        Report("AddRelation+RemoveRelation 往返迁移(2N 迁移)", ms, n * 2);

        // 恢复关系位供查询场景使用
        for (int i = 0; i < n; i++)
            world.AddRelation(entities[i], relType, target);

        ms = Measure(() =>
        {
            int count = 0;
            foreach (var e in world.GetEntitiesWithRelation(relType, target))
                count++;
            if (count != n) throw new InvalidOperationException($"count={count}");
        });
        Report("GetEntitiesWithRelation 全命中(行过滤)", ms, n);

        ms = Measure(() =>
        {
            int count = 0;
            foreach (var e in world.GetEntitiesWithRelation(relType, deadTarget))
                count++;
            if (count != 0) throw new InvalidOperationException($"count={count}");
        });
        Report("GetEntitiesWithRelation 零命中(行扫描)", ms, n);

        Console.WriteLine();
    }

    private static void Main()
    {
        Console.WriteLine("Release, 每轮 5 次取最小");
        Console.WriteLine();

        RunCase("1 组件 (C1)", RelationType.Create<RelK1>(), NCommon, static (world, n) =>
        {
            var a = new Entity[n];
            for (int i = 0; i < n; i++)
                a[i] = world.CreateEntity(new C1 { A = i });
            return a;
        });

        RunCase("2 组件 (C1+C2)", RelationType.Create<RelK2>(), NCommon, static (world, n) =>
        {
            var a = new Entity[n];
            for (int i = 0; i < n; i++)
                a[i] = world.CreateEntity(new C1 { A = i }, new C2 { A = i });
            return a;
        });

        RunCase("4 组件 (C1..C4)", RelationType.Create<RelK4>(), NCommon, static (world, n) =>
        {
            var a = new Entity[n];
            for (int i = 0; i < n; i++)
                a[i] = world.CreateEntity(
                    new C1 { A = i },
                    new C2 { A = i },
                    new C3 { A = i },
                    new C4 { A = i });
            return a;
        });

        RunCase("50 组件 (C1..C50)", RelationType.Create<RelK50>(), NHeavy, static (world, n) =>
        {
            var a = new Entity[n];
            for (int i = 0; i < n; i++)
                a[i] = world.CreateEntity(ComponentsGen.Mask50);
            return a;
        });

        // 对照：4 组件表上的组件读写与迁移（与关系操作同环境对比）
        Console.WriteLine($"--- 对照: 4 组件表上的组件操作 (N={NCommon:N0}) ---");
        using (var controlWorld = new World())
        {
            var a = new Entity[NCommon];
            for (int i = 0; i < NCommon; i++)
                a[i] = controlWorld.CreateEntity(
                    new C1 { A = i },
                    new C2 { A = i },
                    new C3 { A = i },
                    new C4 { A = i });

            double ms;

            ms = Measure(() =>
            {
                for (int i = 0; i < NCommon; i++)
                    controlWorld.SetComponent(a[i], new C1 { A = i });
            });
            Report("对照 SetComponent<C1> 原地写", ms, NCommon);

            ms = Measure(() =>
            {
                for (int i = 0; i < NCommon; i++)
                    controlWorld.GetComponent<C1>(a[i]);
            });
            Report("对照 GetComponent<C1> 读", ms, NCommon);

            ms = Measure(() =>
            {
                for (int i = 0; i < NCommon; i++)
                    controlWorld.AddComponent(a[i], new C5 { A = i });
                for (int i = 0; i < NCommon; i++)
                    controlWorld.RemoveComponent<C5>(a[i]);
            });
            Report("对照 Add+RemoveComponent 往返迁移(2N)", ms, NCommon * 2);

            Console.WriteLine();
        }

        Console.WriteLine("完成。");
    }
}
