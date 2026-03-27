using System.Diagnostics;
using ECSTest.Components;
using ExtenderApp.ECS;
using ExtenderApp.ECS.Abstract;
using ExtenderApp.ECS.Accessors;
using ExtenderApp.ECS.Entities;
using ExtenderApp.ECS.Queries;

// 自定义快速运行器：避免在热路径打印，返回简洁汇总
public static class CustomRunner
{
    // Create/Add/Get 快速运行（无中间输出）
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

        try
        {
            // Create
            sw.Restart();
            for (int i = 0; i < N; i++) entities.Add(world.CreateEntity(mask));
            sw.Stop();
            Console.WriteLine($"Create: {N} entities in {sw.ElapsedMilliseconds} ms");

            // Add
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

            // Get
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
        }
        catch (Exception ex)
        {
            throw;
        }

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

        // 抽样验证
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
        Console.WriteLine($"AddEntity {N} Position components in {sw.ElapsedMilliseconds} ms");

        sw.Restart();
        for (int i = 0; i < N / 2; i++) world.RemoveComponent<Position>(entities[i]);
        sw.Stop();
        Console.WriteLine($"RemoveAt {N / 2} Position components in {sw.ElapsedMilliseconds} ms");

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
        Console.WriteLine($"AddEntity & Read: {(rp.X == 1 && rp.Y == 2 ? "OK" : "FAILED")}");
        Console.WriteLine("========================================");
    }

    // 新增：批量创建实体测试
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

    // 新增：巨型组件对比测试
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
        {
            Console.WriteLine($"CreateDestroy 对照使用较小规模以避免长时间阻塞：N={createDestroyN}");
        }
        if (createDestroyHugeN != createDestroyN)
        {
            Console.WriteLine($"含 HugePayload 的 CreateDestroy 进一步降规模：N={createDestroyHugeN}");
        }

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
        {
            entities.Add(world.CreateEntity(mask));
        }
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
            {
                world.SetComponent(entities[i], new HugePayload { Seed = i });
            }
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
            {
                Console.WriteLine($"  Create progress [{(includeHuge ? "9+Huge" : "9 components")}]: {i + 1}/{n}");
            }
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
        {
            mask.Add(ComponentType.Create<HugePayload>());
        }
        return mask;
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
            RunBatchCreate(n);
            RunHugeComponentComparison(n);
            RunEntityComponentApiTest(Math.Min(n, 5000));
        }

        RunEntityQueryBuildTest();
    }

    // 新增：实体组件 API 行为测试
    public static void RunEntityComponentApiTest(int N)
    {
        Console.WriteLine($"=== CustomRunner: EntityComponentLookup/Operation (N={N}) ===");
        using var world = new World();

        var sw = Stopwatch.StartNew();

        // Operation：增删改查行为验证
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

        // Lookup：从 CurrentWorld 直接获取
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

        // 小规模压力：Operation 路径
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

    // 新增：EntityQuery
    public static void RunEntityQueryBuildTest()
    {
        Console.WriteLine("=== CustomRunner: EntityQuery Single Data Test ===");
        using var world = new World();

        var entity = world.CreateEntity(new Position { X = 7, Y = -7 });

        var allPosition = world.Query<Position>();
        var withVelocity = world.QueryBuilder()
            .WithAll<Velocity>()
            .Build<Position>();
        var noVelocity = world.QueryBuilder()
            .WithNone<Velocity>()
            .Build<Position>();

        var p1 = ReadSinglePosition(allPosition, out var allCount1);
        var withCount1 = Count(withVelocity);
        var noneCount1 = Count(noVelocity);

        world.AddComponent(entity, new Velocity { Vx = 1, Vy = 2 });
        var p2 = ReadSinglePosition(allPosition, out var allCount2);
        var withCount2 = Count(withVelocity);
        var noneCount2 = Count(noVelocity);

        world.RemoveComponent<Velocity>(entity);
        var p3 = ReadSinglePosition(allPosition, out var allCount3);
        var withCount3 = Count(withVelocity);
        var noneCount3 = Count(noVelocity);

        bool valueStable = p1.X == p2.X && p2.X == p3.X && p1.Y == p2.Y && p2.Y == p3.Y;
        bool countOk = allCount1 == 1 && allCount2 == 1 && allCount3 == 1;
        bool filterOk = withCount1 == 0 && noneCount1 == 1
            && withCount2 == 1 && noneCount2 == 0
            && withCount3 == 0 && noneCount3 == 1;

        world.AddComponent(entity, new Velocity { Vx = 10, Vy = 20 });

        int delegateCount = 0;
        var moveQuery = world.Query<Position, Velocity>();
        moveQuery.Query((ref Position position, in Velocity velocity) =>
        {
            delegateCount++;
            position.X += velocity.Vx;
            position.Y += velocity.Vy;
        });

        var p4 = world.GetComponent<Position>(entity);
        bool delegateQueryOk = delegateCount == 1 && p4.X == 17 && p4.Y == 13;
        bool multiDelegateQueryOk = RunMultiComponentDelegateTest();
        bool foreachQueryOk = RunForeachDeconstructTest();

        bool ok = valueStable && countOk && filterOk && delegateQueryOk && multiDelegateQueryOk && foreachQueryOk;

        Console.WriteLine($"DelegateQuery: {(delegateQueryOk ? "OK" : "FAILED")}, Position=({p4.X},{p4.Y})");
        Console.WriteLine($"MultiDelegateQuery: {(multiDelegateQueryOk ? "OK" : "FAILED")}");
        Console.WriteLine($"ForeachDeconstructQuery: {(foreachQueryOk ? "OK" : "FAILED")}");
        Console.WriteLine($"Phase1: Position=({p1.X},{p1.Y}), All={allCount1}, WithVelocity={withCount1}, NoVelocity={noneCount1}");
        Console.WriteLine($"Phase2: Position=({p2.X},{p2.Y}), All={allCount2}, WithVelocity={withCount2}, NoVelocity={noneCount2}");
        Console.WriteLine($"Phase3: Position=({p3.X},{p3.Y}), All={allCount3}, WithVelocity={withCount3}, NoVelocity={noneCount3}");
        Console.WriteLine($"Result: {(ok ? "OK" : "FAILED")}");
        Console.WriteLine("========================================");

        static Position ReadSinglePosition(EntityQuery<Position> query, out int count)
        {
            count = 0;
            Position value = default;
            foreach (Position item in query)
            {
                count++;
                value = item;
            }
            return value;
        }

        static int Count(EntityQuery<Position> query)
        {
            int count = 0;
            foreach (var item in query)
            {
                count++;
            }
            return count;
        }

        static bool RunMultiComponentDelegateTest()
        {
            using var world = new World();

            var entity = world.CreateEntity();
            world.AddComponent(entity, new Position { X = 2, Y = 3 });
            world.AddComponent(entity, new Velocity { Vx = 4, Vy = 5 });
            world.AddComponent(entity, new Health { Value = 6 });

            var ignored = world.CreateEntity();
            world.AddComponent(ignored, new Position { X = 100, Y = 200 });
            world.AddComponent(ignored, new Velocity { Vx = 1, Vy = 1 });

            int count = 0;
            var query = world.Query<Health, Position, Velocity>();

            var directPosition = world.GetComponent<Position>(entity);
            var directVelocity = world.GetComponent<Velocity>(entity);
            var directHealth = world.GetComponent<Health>(entity);
            Console.WriteLine($"MultiDelegateDirectRead: Position=({directPosition.X},{directPosition.Y}), Velocity=({directVelocity.Vx},{directVelocity.Vy}), Health={directHealth.Value}");

            query.Query((in Velocity velocity, in Health health, ref Position position) =>
            {
                count++;
                position.X += velocity.Vx + health.Value;
                position.Y += velocity.Vy - health.Value;
            });

            var result = world.GetComponent<Position>(entity);
            Console.WriteLine($"MultiDelegateDetail: Count={count}, Result=({result.X},{result.Y})");
            return count == 1 && result.X == 12 && result.Y == 2;
        }

        static bool RunForeachDeconstructTest()
        {
            using var world = new World();

            var entity = world.CreateEntity();
            world.AddComponent(entity, new Position { X = 2, Y = 3 });
            world.AddComponent(entity, new Velocity { Vx = 4, Vy = 5 });
            world.AddComponent(entity, new Health { Value = 6 });

            var ignored = world.CreateEntity();
            world.AddComponent(ignored, new Position { X = 100, Y = 200 });
            world.AddComponent(ignored, new Velocity { Vx = 1, Vy = 1 });

            var query = world.Query<Velocity, Health, Position>();
            var query1 = world.Query<Velocity, Position>();

            foreach ((Velocity v, Position p) in query1)
            {
            }

            int count1 = 0;
            float checksum = 0;
            Entity visitedEntity = Entity.Empty;
            foreach ((Velocity velocity, Health health, Position position, Entity rowEntity) in query)
            {
                count1++;
                visitedEntity = rowEntity;
                checksum += velocity.Vx + velocity.Vy + health.Value + position.X + position.Y;
            }

            int count2 = 0;
            foreach ((RefRW<Velocity> velocity, Health health, Position position, Entity rowEntity) in query)
            {
                count2++;
                if (rowEntity != entity) return false;
                velocity.Value = new Velocity
                {
                    Vx = velocity.Value.Vx + health.Value,
                    Vy = velocity.Value.Vy + position.X
                };
            }

            var updatedVelocity = world.GetComponent<Velocity>(entity);
            Console.WriteLine($"ForeachDeconstructDetail: Count1={count1}, Count2={count2}, Entity={visitedEntity}, Checksum={checksum}, Velocity=({updatedVelocity.Vx},{updatedVelocity.Vy})");
            return count1 == 1
                && count2 == 1
                && visitedEntity == entity
                && checksum == 20
                && updatedVelocity.Vx == 10
                && updatedVelocity.Vy == 7;
        }
    }

    public static void RunRelationPairCase(int _)
    {
        Console.WriteLine("=== CustomRunner: RelationPair 示例 ===");
        using var world = new World();

        // 准备实体：child 作为“主体实体”，parentA/parentB 作为“目标实体”
        var child = world.CreateEntity();
        var parentA = world.CreateEntity();
        var parentB = world.CreateEntity();
        var target = world.CreateEntity();

        // 关系类型
        var childOf = RelationType.Create<ChildOf>();
        var targetRel = RelationType.Create<Target>();

        // 关系对（高位：关系类型，低位：目标ID）
        var childOfA = RelationPair.Create(childOf, parentA);
        var childOfB = RelationPair.Create(childOf, parentB);
        var targetPair = RelationPair.Create(targetRel, target);

        Console.WriteLine($"childOfA = {childOfA}, raw=0x{childOfA.Value:X16}");
        Console.WriteLine($"childOfB = {childOfB}, raw=0x{childOfB.Value:X16}");
        Console.WriteLine($"targetPair = {targetPair}, raw=0x{targetPair.Value:X16}");
        Console.WriteLine($"childOfA == childOfB ? {childOfA == childOfB}");

        // 关系掩码（只看关系类型）
        var relationMask = new RelationMask();
        relationMask.Add(childOf);
        relationMask.Add(targetRel);
        Console.WriteLine($"RelationMask.Has(ChildOf) = {relationMask.On(childOf)}");
        Console.WriteLine($"RelationMask.Has(Target) = {relationMask.On(targetRel)}");

        // 关系对与主体实体关联：主体实体 -> pair 列表
        // 这是一种最直观的关联方式（演示用）
        var ownerRelations = new Dictionary<Entity, List<RelationPair>>();
        ownerRelations[child] = new List<RelationPair> { childOfA, targetPair };

        Console.WriteLine($"Owner {child} relation count = {ownerRelations[child].Count}");

        // 修改 child 的 ChildOf 目标：parentA -> parentB
        ownerRelations[child][0] = childOfB;
        Console.WriteLine($"Owner {child} ChildOf targetId changed to {ownerRelations[child][0].TargetId}");

        Console.WriteLine("========================================");
    }

    // 新增：命令缓冲并发写入测试
    public static void RunCommandBufferConcurrencyTest(int opsPerWriter)
    {
        Console.WriteLine($"=== CommandBuffer Concurrency Test: writers=8, opsPerWriter={opsPerWriter} ===");
        using var world = new World();

        // 从 CurrentWorld 获取命令缓冲实例
        var buffer = world.CommandBuffer;

        int writers = 8;
        int ops = Math.Max(1, opsPerWriter);

        // 使用真实实体作为标记，便于在回放后验证数据正确性
        var smoke = world.CreateEntity();
        buffer.AddComponent(smoke, new Position { X = 1, Y = 2 });

        // 预先在主线程创建每个任务将要使用的真实实体集合，避免在工作线程上调用 CurrentWorld API
        var precreated = new Entity[writers][];
        for (int w = 0; w < writers; w++)
        {
            precreated[w] = new Entity[ops];
            for (int i = 0; i < ops; i++)
            {
                precreated[w][i] = world.CreateEntity();
            }
        }

        var barrier = new Barrier(writers);
        var tasks = new Task[writers];
        var sw = Stopwatch.StartNew();

        for (int w = 0; w < writers; w++)
        {
            int writerIndex = w; // capture
            tasks[w] = Task.Run(() =>
            {
                barrier.SignalAndWait();

                // Use pre-created real entities to avoid virtual entity mapping path
                var localEntities = precreated[writerIndex];
                for (int i = 0; i < ops; i++)
                {
                    var e = localEntities[i];
                    // record operations against the real entity
                    buffer.AddComponent(e, new Position { X = i, Y = -i });
                    buffer.SetComponent(e, new Position { X = i + 1, Y = -i - 1 });
                    buffer.RemoveComponent<Position>(e);
                    buffer.DestroyEntity(e);
                }
            });
        }

        Task.WaitAll(tasks);
        sw.Stop();

        long totalOps = (long)writers * ops * 5 + 1; // 每轮 5 条命令, +1 为 smoke 的 Add
        Console.WriteLine($"Command write finished: totalCommands={totalOps}, elapsed={sw.ElapsedMilliseconds} ms");

        // 验证：回放后，唯一存在 Position 的实体应为 smoke，且值应为 (1,2)
        bool ok = true;
        int posCount = 0;
        foreach (var p in world.Query<Position>())
        {
            posCount++;
        }

        try
        {
            var sp = world.GetComponent<Position>(smoke);
            if (sp.X != 1 || sp.Y != 2) ok = false;
        }
        catch
        {
            ok = false;
        }

        if (posCount != 1) ok = false;

        Console.WriteLine($"Verification: PositionCount={posCount}, SmokeValid={ok}");

        if (!ok)
        {
            Console.WriteLine("--- Diagnostic: listing up to 50 Position entities ---");
            try
            {
                int printed = 0;
                // first try to get entity with deconstruct form
                try
                {
                    foreach ((Position p, Entity e) in world.Query<Position>())
                    {
                        Console.WriteLine($"Entity={e}, Position=({p.X},{p.Y}){(e == smoke ? " [smoke]" : "")} ");
                        if (++printed >= 50) break;
                    }
                }
                catch
                {
                    // fallback: only values available
                    foreach (var p in world.Query<Position>())
                    {
                        Console.WriteLine($"Position value: {p}");
                        if (++printed >= 50) break;
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Diagnostic enumeration failed: " + ex.Message);
            }

            Console.WriteLine("--- End Diagnostic ---");
        }
    }

    // 新增：只使用 CurrentWorld API 的对照测试（不经过 CommandBuffer）
    public static void RunWorldDirectExecutionTest(int opsPerWriter)
    {
        Console.WriteLine($"=== CurrentWorld Direct Execution opsPerWriter={opsPerWriter} ===");
        using var world = new World();

        int ops = Math.Max(1, opsPerWriter);

        var smoke = world.CreateEntity();
        world.AddComponent(smoke, new Position { X = 1, Y = 2 });

        var sw = Stopwatch.StartNew();

        for (int i = 0; i < ops; i++)
        {
            var e = world.CreateEntity();
            world.AddComponent(e, new Position { X = i, Y = -i });
            world.SetComponent(e, new Position { X = i + 1, Y = -i - 1 });
            world.AddComponent(e, new Velocity { Vx = i, Vy = -i });
            world.AddComponent(e, new Health { Value = i });
            world.AddComponent(e, new Rotation { Value = i });
            world.AddComponent(e, new Scale { Value = i });
            world.RemoveComponent<Position>(e);
            world.DestroyEntity(e);
        }

        sw.Stop();

        long totalOps = (long)ops * 8 + 1;
        Console.WriteLine($"CurrentWorld direct execution finished: totalOps={totalOps}, elapsed={sw.ElapsedMilliseconds} ms");

        bool ok = true;
        int posCount = 0;
        foreach (var p in world.Query<Position>())
        {
            posCount++;
        }

        try
        {
            var sp = world.GetComponent<Position>(smoke);
            if (sp.X != 1 || sp.Y != 2) ok = false;
        }
        catch
        {
            ok = false;
        }

        if (posCount != 1) ok = false;

        Console.WriteLine($"Verification: PositionCount={posCount}, SmokeValid={ok}");
        Console.WriteLine("========================================");
    }

    // 新增：DestroyEntitiesForQuery 测试
    public static void RunDestroyEntitiesForQueryTest()
    {
        Console.WriteLine("=== CustomRunner: DestroyEntitiesForQuery Test ===");
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

        // 构造仅匹配包含 Position 的查询，然后隐式转换为仅实体的 EntityQuery
        var typedQuery = world.Query<Position>();
        EntityQuery query = typedQuery; // implicit conversion

        // 调用被测方法
        world.DestroyEntitiesForQuery(query);

        // 验证：回放后不应再存在任何 Position 实体
        int posCount = 0;
        foreach (var p in world.Query<Position>()) posCount++;

        bool ok = posCount == 0;
        Console.WriteLine($"DestroyEntitiesForQuery: initialWithPosition={withPos}, remainingPositionCount={posCount}, Result={(ok ? "OK" : "FAILED")}");
        Console.WriteLine("========================================");
    }

    // 新增：Shared Component 增删改查测试（替换原来的托管 UseData 测试）
    public static void RunSharedComponentCrudTest()
    {
        Console.WriteLine("=== CustomRunner: SharedComponent CRUD Test ===");
        using var world = new World();

        var sw = Stopwatch.StartNew();

        // 尝试 Add（不应存在)
        bool tryAddOk = world.TryAddSharedComponent(new SharedSample { X = 10, Y = 20 });

        // 再次 Add 应失败
        bool addFail = false;
        try
        {
            world.AddSharedComponent(new SharedSample { X = 1, Y = 2 });
            addFail = false;
        }
        catch
        {
            addFail = true;
        }

        // TryGet
        bool tryGetOk = world.TryGetSharedComponent<SharedSample>(out var sample);

        // Update
        world.UpdateSharedComponent(new SharedSample { X = 100, Y = 200 });
        var updated = world.GetSharedComponent<SharedSample>();

        // TryUpdate (succeeds)
        bool tryUpdateOk = world.TryUpdateSharedComponent(new SharedSample { X = 7, Y = 8 });

        // RemoveAt
        bool removed = world.RemoveSharedComponent<SharedSample>();

        // After remove TryGet should fail
        bool tryGetAfterRemove = world.TryGetSharedComponent<SharedSample>(out _);

        sw.Stop();

        Console.WriteLine($"TryAdd initial: {(tryAddOk ? "OK" : "FAILED")}");
        Console.WriteLine($"Add when exists throws: {(addFail ? "OK" : "FAILED")}");
        Console.WriteLine($"TryGet after add: {(tryGetOk ? "OK" : "FAILED")}, value=({sample.X},{sample.Y})");
        Console.WriteLine($"Get after update: ({updated.X},{updated.Y})");
        Console.WriteLine($"TryUpdate: {(tryUpdateOk ? "OK" : "FAILED")}");
        Console.WriteLine($"RemoveAt: {(removed ? "OK" : "FAILED")}");
        Console.WriteLine($"TryGet after remove: {(tryGetAfterRemove ? "FAILED" : "OK")}");
        Console.WriteLine($"Elapsed: {sw.ElapsedMilliseconds} ms");
        Console.WriteLine("========================================");
    }

    // 嵌套共享数据类型，仅用于测试
    private struct SharedSample
    {
        public int X;
        public int Y;
        public override string ToString() => $"SharedSample(X={X},Y={Y})";
    }

    // 新增：托管对象 UseData 存取测试（重加回原有测试）
    public static void RunManagedUseDataTest()
    {
        Console.WriteLine("=== CustomRunner: Managed UseData Test ===");
        using var world = new World();

        var sw = Stopwatch.StartNew();

        for (int i = 0; i < 1000; i++)
        {
            world.CreateEntity(new ManagedData() { Name = i.ToString(), Level = i });
        }

        int count = 0;
        foreach (var md in world.Query<ManagedData>())
        {
            count++;
        }
        sw.Stop();
        Console.WriteLine($"Created and queried {count} ManagedData entities in {sw.ElapsedMilliseconds} ms");
    }

    // 嵌套托管数据类型，仅用于测试
    private class ManagedData
    {
        public string? Name { get; set; }
        public int Level { get; set; }

        public override string ToString() => $"ManagedData(Name={Name}, Level={Level})";
    }
}