using System;
using System.Collections.Generic;
using System.Diagnostics;
using ExtenderApp.ECS;
using ECSTest.Components;

// 自定义快速运行器：避免在热路径打印，返回简洁汇总
public static class CustomRunner
{
    // Create/Set/Get 快速运行（无中间输出）
    public static void RunCreateSetGet(int N)
    {
        Console.WriteLine($"=== CustomRunner: Create/Set/Get (N={N}) ===");
        using var world = new World();

        var mask = new ComponentMask();
        mask.Add(ComponentType.Create<Position>());
        mask.Add(ComponentType.Create<Velocity>());

        var sw = new Stopwatch();
        var entities = new List<Entity>(N);

        // Create
        sw.Restart();
        for (int i = 0; i < N; i++) entities.Add(world.CreateEntity(mask));
        sw.Stop();
        Console.WriteLine($"Create: {N} entities in {sw.ElapsedMilliseconds} ms");

        // Set
        sw.Restart();
        for (int i = 0; i < N; i++)
        {
            world.SetComponent(entities[i], new Position { X = i, Y = -i });
            world.SetComponent(entities[i], new Velocity { Vx = i * 0.001f, Vy = i * 0.002f });
        }
        sw.Stop();
        Console.WriteLine($"Set: {N * 2} component ops in {sw.ElapsedMilliseconds} ms");

        // Get
        sw.Restart();
        double sum = 0;
        for (int i = 0; i < N; i++)
        {
            var p = world.GetComponent<Position>(entities[i]);
            var v = world.GetComponent<Velocity>(entities[i]);
            sum += p.X + v.Vx;
        }
        sw.Stop();
        Console.WriteLine($"Get: {N * 2} component ops in {sw.ElapsedMilliseconds} ms");

        Console.WriteLine($"Checksum: {sum}");
        Console.WriteLine("========================================");
    }

    // Transfer 快速运行（无中间输出）
    public static void RunTransfer(int N)
    {
        Console.WriteLine($"=== CustomRunner: Transfer (N={N}) ===");
        using var world = new World();

        var entitiesA = new List<Entity>(N);
        var entitiesB = new List<Entity>(N);
        for (int i = 0; i < N; i++)
        {
            var a = world.CreateEntity();
            var b = world.CreateEntity();
            entitiesA.Add(a);
            entitiesB.Add(b);
            world.AddComponent(a, new Position { X = i, Y = -i });
            world.AddComponent(b, new Velocity { Vx = i * 0.1f, Vy = i * 0.2f });
        }

        var sw = Stopwatch.StartNew();
        for (int i = 0; i < N; i++)
        {
            var p = world.GetComponent<Position>(entitiesA[i]);
            world.RemoveComponent<Position>(entitiesA[i]);
            world.AddComponent(entitiesB[i], p);
        }
        sw.Stop();

        Console.WriteLine($"Transfer: {N} components in {sw.ElapsedMilliseconds} ms");

        // 样本校验
        bool ok = true;
        for (int i = 0; i < Math.Min(5, N); i++)
        {
            var rp = world.GetComponent<Position>(entitiesB[i]);
            if (rp.X != i || rp.Y != -i) { ok = false; break; }
        }
        Console.WriteLine($"Sample check: {(ok ? "OK" : "FAILED")}");
        Console.WriteLine("========================================");
    }

    // 新增：多实体 CRUD 快速运行
    public static void RunMultipleEntitiesCRUD(int N)
    {
        Console.WriteLine($"=== CustomRunner: MultipleEntitiesCRUD (N={N}) ===");
        using var world = new World();

        var mask = new ComponentMask();
        mask.Add(ComponentType.Create<Position>());
        mask.Add(ComponentType.Create<Velocity>());

        var entities = new List<Entity>(N);
        for (int i = 0; i < N; i++) entities.Add(world.CreateEntity(mask));

        var sw = Stopwatch.StartNew();
        for (int i = 0; i < N; i++)
        {
            world.SetComponent(entities[i], new Position { X = i + 0.5f, Y = -i - 0.25f });
            world.SetComponent(entities[i], new Velocity { Vx = i * 0.01f, Vy = i * 0.02f });
        }
        sw.Stop();
        Console.WriteLine($"Set for {N} entities in {sw.ElapsedMilliseconds} ms");

        // 抽样验证
        int samples = Math.Min(100, N);
        var rnd = new Random(42);
        bool ok = true;
        for (int k = 0; k < samples; k++)
        {
            int i = rnd.Next(N);
            var rp = world.GetComponent<Position>(entities[i]);
            var rv = world.GetComponent<Velocity>(entities[i]);
            if (rp.X != i + 0.5f || rp.Y != -i - 0.25f || rv.Vx != i * 0.01f || rv.Vy != i * 0.02f) { ok = false; break; }
        }
        Console.WriteLine($"Sample verification: {(ok ? "OK" : "FAILED")}");
        Console.WriteLine("========================================");
    }

    // 新增：批量添加/移除 快速运行
    public static void RunBulkAddRemove(int N)
    {
        Console.WriteLine($"=== CustomRunner: BulkAddRemove (N={N}) ===");
        using var world = new World();

        var entities = new List<Entity>(N);
        for (int i = 0; i < N; i++) entities.Add(world.CreateEntity());

        var sw = Stopwatch.StartNew();
        for (int i = 0; i < N; i++) world.AddComponent(entities[i], new Position { X = i, Y = i });
        sw.Stop();
        Console.WriteLine($"Add {N} Position components in {sw.ElapsedMilliseconds} ms");

        sw.Restart();
        for (int i = 0; i < N / 2; i++) world.RemoveComponent<Position>(entities[i]);
        sw.Stop();
        Console.WriteLine($"Remove {N / 2} Position components in {sw.ElapsedMilliseconds} ms");

        // 验证
        bool ok = true;
        for (int i = 0; i < N; i++)
        {
            try
            {
                var p = world.GetComponent<Position>(entities[i]);
                if (i < N / 2) { ok = false; break; }
            }
            catch
            {
                if (i >= N / 2) { ok = false; break; }
            }
        }
        Console.WriteLine($"Verification: {(ok ? "OK" : "FAILED")}");
        Console.WriteLine("========================================");
    }

    // 新增：异常行为快速运行
    public static void RunExceptionBehavior()
    {
        Console.WriteLine($"=== CustomRunner: ExceptionBehavior ===");
        using var world = new World();

        var e = world.CreateEntity();
        bool threw = false;
        try { var p = world.GetComponent<Position>(e); } catch { threw = true; }
        Console.WriteLine($"Read nonexistent component throws: {(threw ? "Yes" : "No")}");

        world.AddComponent(e, new Position { X = 1, Y = 2 });
        var rp = world.GetComponent<Position>(e);
        Console.WriteLine($"Add & Read: {(rp.X == 1 && rp.Y == 2 ? "OK" : "FAILED")}");
        Console.WriteLine("========================================");
    }

    public static int GetPerfCount(int defaultN)
    {
        var s = Environment.GetEnvironmentVariable("ECSTEST_PERF_N");
        if (!string.IsNullOrEmpty(s) && int.TryParse(s, out var val) && val > 0)
            return val;
        return defaultN;
    }

    public static void RunAllQuick()
    {
        var sizes = new int[] { 1000, 10000 };
        foreach (var n in sizes)
        {
            RunCreateSetGet(n);
            RunTransfer(n);
            RunMultipleEntitiesCRUD(n);
            RunBulkAddRemove(n);
        }
    }
}