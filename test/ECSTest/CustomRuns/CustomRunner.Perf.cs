using System.Diagnostics;
using ECSTest.Components;
using ExtenderApp.ECS;

namespace ECSTest.CustomRuns;

/// <summary>
/// 自定义快速运行器：Create/Transfer/CRUD/批量与巨型组件对照（性能向）。
/// </summary>
public static partial class CustomRunner
{
    /// <summary>
    /// Create / Set / Get 多组件路径的快速跑法（无逐条热路径打印）。
    /// </summary>
    public static void RunCreateSetGet(int N)
    {
        Console.WriteLine($"=== CustomRunner: Create/Add/Get (N={N}) ===");
        using var world = new World();

        var mask = new ComponentMask();
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
        mask.Add<PlayerTag>();

        var sw = new Stopwatch();
        var entities = new List<Entity>(N);
        double sum = 0;

        sw.Restart();
        for (int i = 0; i < N; i++) entities.Add(world.CreateEntity(mask));
        sw.Stop();
        Console.WriteLine($"Create: {N} entities in {sw.ElapsedMilliseconds} ms");

        sw.Restart();
        for (int i = 0; i < N; i++)
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
        sw.Stop();
        Console.WriteLine($"Add: {N * 10} component ops in {sw.ElapsedMilliseconds} ms");

        sw.Restart();
        for (int i = 0; i < N; i++)
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
        sw.Stop();
        Console.WriteLine($"Get: {N * 10} component ops in {sw.ElapsedMilliseconds} ms");

        Console.WriteLine($"Checksum: {sum}");
        Console.WriteLine("========================================");
    }

    /// <summary>
    /// 组件在实体间迁移（Transfer）路径的快速跑法。
    /// </summary>
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

        bool ok = true;
        for (int i = 0; i < Math.Min(5, N); i++)
        {
            var rp = world.GetComponent<Position>(entitiesB[i]);
            if (rp.X != i || rp.Y != -i) { ok = false; break; }
        }
        Console.WriteLine($"Sample check: {(ok ? "OK" : "FAILED")}");
        Console.WriteLine("========================================");
    }

    /// <summary>
    /// 多实体 CRUD 快速跑法。
    /// </summary>
    public static void RunMultipleEntitiesCRUD(int N)
    {
        Console.WriteLine($"=== CustomRunner: MultipleEntitiesCRUD (N={N}) ===");
        using var world = new World();

        var mask = new ComponentMask();
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

        var entities = new List<Entity>(N);
        for (int i = 0; i < N; i++) entities.Add(world.CreateEntity(mask));

        var sw = Stopwatch.StartNew();
        for (int i = 0; i < N; i++)
        {
            world.SetComponent(entities[i], new Position { X = i + 0.5f, Y = -i - 0.25f });
            world.SetComponent(entities[i], new Velocity { Vx = i * 0.01f, Vy = i * 0.02f });
            world.SetComponent(entities[i], new Health { Value = i + 1 });
            world.SetComponent(entities[i], new Mana { Value = i + 2 });
            world.SetComponent(entities[i], new Rotation { Value = i * 0.05f });
            world.SetComponent(entities[i], new Scale { Value = 2f + i * 0.001f });
            world.SetComponent(entities[i], new Acceleration { X = i * 0.005f, Y = i * 0.006f });
            world.SetComponent(entities[i], new Team { Id = i % 4 });
            world.SetComponent(entities[i], new State { Value = i % 3 });
            world.SetComponent(entities[i], new HugePayload { Seed = i * 10 });
        }
        sw.Stop();
        Console.WriteLine($"Add for {N} entities in {sw.ElapsedMilliseconds} ms");

        int samples = Math.Min(100, N);
        var rnd = new Random(42);
        bool ok = true;
        for (int k = 0; k < samples; k++)
        {
            int i = rnd.Next(N);
            var rp = world.GetComponent<Position>(entities[i]);
            var rv = world.GetComponent<Velocity>(entities[i]);
            var rh = world.GetComponent<Health>(entities[i]);
            var rm = world.GetComponent<Mana>(entities[i]);
            var rr = world.GetComponent<Rotation>(entities[i]);
            var rs = world.GetComponent<Scale>(entities[i]);
            var ra = world.GetComponent<Acceleration>(entities[i]);
            var rt = world.GetComponent<Team>(entities[i]);
            var rst = world.GetComponent<State>(entities[i]);
            var rhp = world.GetComponent<HugePayload>(entities[i]);
            if (rp.X != i + 0.5f || rp.Y != -i - 0.25f ||
                rv.Vx != i * 0.01f || rv.Vy != i * 0.02f ||
                rh.Value != i + 1 || rm.Value != i + 2 ||
                rr.Value != i * 0.05f || rs.Value != 2f + i * 0.001f ||
                ra.X != i * 0.005f || ra.Y != i * 0.006f ||
                rt.Id != i % 4 || rst.Value != i % 3 ||
                rhp.Seed != i * 10)
            {
                ok = false;
                break;
            }
        }
        Console.WriteLine($"Sample verification: {(ok ? "OK" : "FAILED")}");
        Console.WriteLine("========================================");
    }

    /// <summary>
    /// 批量添加/移除单组件路径的快速跑法。
    /// </summary>
    public static void RunBulkAddRemove(int N)
    {
        Console.WriteLine($"=== CustomRunner: BulkAddRemove (N={N}) ===");
        using var world = new World();

        var entities = new List<Entity>(N);
        for (int i = 0; i < N; i++) entities.Add(world.CreateEntity());

        var sw = Stopwatch.StartNew();
        for (int i = 0; i < N; i++) world.AddComponent(entities[i], new Position { X = i, Y = i });
        sw.Stop();
        Console.WriteLine($"AddEntity {N} Position components in {sw.ElapsedMilliseconds} ms");

        sw.Restart();
        for (int i = 0; i < N / 2; i++) world.RemoveComponent<Position>(entities[i]);
        sw.Stop();
        Console.WriteLine($"RemoveAt {N / 2} Position components in {sw.ElapsedMilliseconds} ms");

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

    /// <summary>
    /// 异常路径（缺组件读取等）的冒烟输出。
    /// </summary>
    public static void RunExceptionBehavior()
    {
        Console.WriteLine("=== CustomRunner: ExceptionBehavior ===");
        using var world = new World();

        var e = world.CreateEntity();
        bool threw = false;
        try { _ = world.GetComponent<Position>(e); } catch { threw = true; }
        Console.WriteLine($"Read nonexistent component throws: {(threw ? "Yes" : "No")}");

        world.AddComponent(e, new Position { X = 1, Y = 2 });
        var rp = world.GetComponent<Position>(e);
        Console.WriteLine($"AddEntity & Read: {(rp.X == 1 && rp.Y == 2 ? "OK" : "FAILED")}");
        Console.WriteLine("========================================");
    }

    /// <summary>
    /// 批量 <see cref="World.CreateEntity(System.Span{ExtenderApp.ECS.Entity})"/> 与批量销毁。
    /// </summary>
    public static void RunBatchCreate(int N)
    {
        Console.WriteLine($"=== CustomRunner: BatchCreate (N={N}) ===");
        using var world = new World();

        var entities = new Entity[N];

        var sw = Stopwatch.StartNew();
        world.CreateEntity(entities.AsSpan());
        sw.Stop();

        int created = 0;
        for (int i = 0; i < entities.Length; i++)
        {
            if (!entities[i].IsEmpty) created++;
        }

        Console.WriteLine($"Batch Create: target={N}, created={created}, elapsed={sw.ElapsedMilliseconds} ms");

        var sample = Math.Min(10, entities.Length);
        bool sampleOk = true;
        for (int i = 0; i < sample; i++)
        {
            if (entities[i].IsEmpty)
            {
                sampleOk = false;
                break;
            }
        }
        Console.WriteLine($"Sample verification: {(sampleOk ? "OK" : "FAILED")}");

        var validEntities = new List<Entity>(created);
        for (int i = 0; i < entities.Length; i++)
        {
            if (!entities[i].IsEmpty) validEntities.Add(entities[i]);
        }

        sw.Restart();
        if (validEntities.Count > 0)
        {
            var destroyBuffer = validEntities.ToArray();
            world.DestroyEntity(destroyBuffer.AsSpan());
        }
        sw.Stop();

        Console.WriteLine($"Batch Destroy: count={validEntities.Count}, elapsed={sw.ElapsedMilliseconds} ms");
        Console.WriteLine("========================================");
    }

    /// <summary>
    /// 巨型组件与掩码 Create/Destroy 对照。
    /// </summary>
    public static void RunHugeComponentComparison(int N)
    {
        Console.WriteLine($"=== CustomRunner: HugeComponentComparison (N={N}) ===");

        Console.WriteLine("[1/4] CreateSetGet: 9 components");
        RunCreateSetGetScenario(N, includeHuge: false);

        Console.WriteLine("[2/4] CreateSetGet: 9 + HugePayload");
        RunCreateSetGetScenario(N, includeHuge: true);

        int createDestroyN = Math.Min(N, 100000);
        int createDestroyHugeN = Math.Min(createDestroyN, 20000);
        if (createDestroyN != N)
            Console.WriteLine($"CreateDestroy 对照使用较小规模以避免长时间阻塞：N={createDestroyN}");
        if (createDestroyHugeN != createDestroyN)
            Console.WriteLine($"含 HugePayload 的 CreateDestroy 进一步降规模：N={createDestroyHugeN}");

        Console.WriteLine("[3/4] CreateDestroy: 9 components");
        RunCreateDestroyWithMaskScenario(createDestroyN, includeHuge: false);

        Console.WriteLine("[4/4] CreateDestroy: 9 + HugePayload");
        RunCreateDestroyWithMaskScenario(createDestroyHugeN, includeHuge: true);

        Console.WriteLine("========================================");
    }

    private static void RunCreateSetGetScenario(int n, bool includeHuge)
    {
        using var world = new World();
        var mask = BuildMask(includeHuge);
        var entities = new List<Entity>(n);
        var sw = new Stopwatch();
        double checksum = 0;

        sw.Start();
        for (int i = 0; i < n; i++)
            entities.Add(world.CreateEntity(mask));
        sw.Stop();
        var createMs = sw.ElapsedMilliseconds;

        sw.Restart();
        for (int i = 0; i < n; i++)
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

            if (includeHuge)
                world.SetComponent(entities[i], new HugePayload { Seed = i });
        }
        sw.Stop();
        var setMs = sw.ElapsedMilliseconds;

        sw.Restart();
        for (int i = 0; i < n; i++)
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
            checksum += p.X + v.Vx + h.Value + m.Value + r.Value + s.Value + a.X + t.Id + st.Value;
            if (includeHuge)
            {
                var huge = world.GetComponent<HugePayload>(entities[i]);
                checksum += huge.Seed;
            }
        }
        sw.Stop();
        var getMs = sw.ElapsedMilliseconds;

        Console.WriteLine($"[CreateSetGet][{(includeHuge ? "9+Huge" : "9 components")}] Create={createMs} ms, Add={setMs} ms, Get={getMs} ms, Checksum={checksum}");
    }

    private static void RunCreateDestroyWithMaskScenario(int n, bool includeHuge)
    {
        using var world = new World();
        var mask = BuildMask(includeHuge);
        var entities = new Entity[n];

        const int chunkSize = 20000;
        var sw = Stopwatch.StartNew();
        for (int i = 0; i < n; i++)
        {
            entities[i] = world.CreateEntity(mask);
            if ((i + 1) % chunkSize == 0 || i + 1 == n)
                Console.WriteLine($"  Create progress [{(includeHuge ? "9+Huge" : "9 components")}]: {i + 1}/{n}");
        }
        sw.Stop();
        var createMs = sw.ElapsedMilliseconds;

        sw.Restart();
        int destroyed = 0;
        while (destroyed < n)
        {
            int len = Math.Min(chunkSize, n - destroyed);
            world.DestroyEntity(entities.AsSpan(destroyed, len));
            destroyed += len;
            Console.WriteLine($"  Destroy progress [{(includeHuge ? "9+Huge" : "9 components")}]: {destroyed}/{n}");
        }
        sw.Stop();
        var destroyMs = sw.ElapsedMilliseconds;

        Console.WriteLine($"[CreateDestroy][{(includeHuge ? "9+Huge" : "9 components")}] Create={createMs} ms, Destroy={destroyMs} ms");
    }

    private static ComponentMask BuildMask(bool includeHuge)
    {
        var mask = new ComponentMask();
        mask.Add(ComponentType.Create<Position>());
        mask.Add(ComponentType.Create<Velocity>());
        mask.Add(ComponentType.Create<Health>());
        mask.Add(ComponentType.Create<Mana>());
        mask.Add(ComponentType.Create<Rotation>());
        mask.Add(ComponentType.Create<Scale>());
        mask.Add(ComponentType.Create<Acceleration>());
        mask.Add(ComponentType.Create<Team>());
        mask.Add(ComponentType.Create<State>());
        mask.Add<PlayerTag>();
        if (includeHuge)
            mask.Add(ComponentType.Create<HugePayload>());
        return mask;
    }

    /// <summary>
    /// 从环境变量 <c>ECSTEST_PERF_N</c> 读取规模，默认返回 <paramref name="defaultN"/>。
    /// </summary>
    public static int GetPerfCount(int defaultN)
    {
        var s = Environment.GetEnvironmentVariable("ECSTEST_PERF_N");
        if (!string.IsNullOrEmpty(s) && int.TryParse(s, out var val) && val > 0)
            return val;
        return defaultN;
    }

    /// <summary>
    /// 串联多组快速跑法（用于本地一键冒烟）。
    /// </summary>
    public static void RunAllQuick()
    {
        var sizes = new int[] { 1000, 10000 };
        foreach (var n in sizes)
        {
            RunCreateSetGet(n);
            RunTransfer(n);
            RunMultipleEntitiesCRUD(n);
            RunBulkAddRemove(n);
            RunBatchCreate(n);
            RunHugeComponentComparison(n);
            RunEntityComponentApiTest(Math.Min(n, 5000));
        }

        RunEntityQueryBuildTest();
    }

    /// <summary>
    /// 实体组件 API（Lookup / Operation）行为与轻量压力。
    /// </summary>
    public static void RunEntityComponentApiTest(int N)
    {
        Console.WriteLine($"=== CustomRunner: EntityComponentLookup/Operation (N={N}) ===");
        using var world = new World();

        var sw = Stopwatch.StartNew();

        var e = world.CreateEntity();
        world.AddComponent(e, new Position { X = 1, Y = 2 });
        world.SetComponent(e, new Position { X = 3, Y = 4 });
        var p = world.GetComponent<Position>(e);
        bool opBasicOk = p.X == 3 && p.Y == 4;

        world.AddComponent(e, new Velocity { Vx = 7, Vy = 8 });
        var v = world.GetComponent<Velocity>(e);
        bool opAddOk = v.Vx == 7 && v.Vy == 8;

        world.RemoveComponent<Velocity>(e);
        bool opRemoveOk;
        try
        {
            _ = world.GetComponent<Velocity>(e);
            opRemoveOk = false;
        }
        catch
        {
            opRemoveOk = true;
        }

        bool lookupOk = false;
        bool lookupGetSetOk = false;
        bool lookupTryGetOk = false;

        try
        {
            var lookup = world.GetEntityComponentLookup(e);
            lookupOk = true;

            lookup.SetComponent(new Position { X = 11, Y = 22 });
            var lp = lookup.GetComponent<Position>();
            lookupGetSetOk = lp.X == 11 && lp.Y == 22;

            lookupTryGetOk = lookup.TryGetComponent<Position>(out var tp) && tp.X == 11 && tp.Y == 22;
        }
        catch
        {
            lookupOk = false;
        }

        var entities = new List<Entity>(N);
        var mask = new ComponentMask(ComponentType.Create<Position>());
        for (int i = 0; i < N; i++) entities.Add(world.CreateEntity(mask));
        for (int i = 0; i < N; i++) world.SetComponent(entities[i], new Position { X = i, Y = -i });

        double sum = 0;
        for (int i = 0; i < N; i++)
        {
            var rp = world.GetComponent<Position>(entities[i]);
            sum += rp.X * 3 + rp.Y;
        }

        sw.Stop();

        Console.WriteLine($"Operation.Basic: {(opBasicOk ? "OK" : "FAILED")}");
        Console.WriteLine($"Operation.Add/RemoveAt: {(opAddOk && opRemoveOk ? "OK" : "FAILED")}");
        Console.WriteLine($"Lookup.Create: {(lookupOk ? "OK" : "FAILED")}");
        Console.WriteLine($"Lookup.Get/Add: {(lookupGetSetOk ? "OK" : "FAILED")}");
        Console.WriteLine($"Lookup.TryGet: {(lookupTryGetOk ? "OK" : "FAILED")}");
        Console.WriteLine($"Operation.Stress checksum: {sum}");
        Console.WriteLine($"Elapsed: {sw.ElapsedMilliseconds} ms");
        Console.WriteLine("========================================");
    }
}
