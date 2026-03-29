using ECSTest.Components;
using ExtenderApp.ECS;
using Xunit;

namespace ECSTest.CustomRuns;

/// <summary>
/// Create/Transfer/CRUD 等性能向路径的正确性断言（小规模），供 xUnit 与菜单冒烟共用。
/// </summary>
public static class CustomRunnerPerfVerification
{
    /// <summary>
    /// 校验 Create / Set / Get 多组件路径在给定规模下的校验和与抽样。
    /// </summary>
    public static void VerifyCreateSetGet(int n)
    {
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

        var entities = new List<Entity>(n);
        for (int i = 0; i < n; i++)
            entities.Add(world.CreateEntity(mask));

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
            world.SetComponent(entities[i], new HugePayload { Seed = i });
        }

        float expectedSum = 0f;
        float sum = 0f;
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
            var huge = world.GetComponent<HugePayload>(entities[i]);
            sum += p.X + v.Vx + h.Value + m.Value + r.Value + s.Value + a.X + t.Id + st.Value + huge.Seed;
            expectedSum += ExpectedCreateSetGetRow(i);
        }

        Assert.Equal(expectedSum, sum, 4);
    }

    private static float ExpectedCreateSetGetRow(int i)
    {
        return i + i * 0.001f + i + i * 2 + i * 0.01f + (1f + i * 0.001f) + i * 0.003f + (i % 8) + (i % 5) + i;
    }

    /// <summary>
    /// 校验多实体 CRUD 随机抽样与期望值一致。
    /// </summary>
    public static void VerifyMultipleEntitiesCrud(int n)
    {
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

        var entities = new List<Entity>(n);
        for (int i = 0; i < n; i++)
            entities.Add(world.CreateEntity(mask));

        for (int i = 0; i < n; i++)
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

        int samples = Math.Min(100, n);
        var rnd = new Random(42);
        for (int k = 0; k < samples; k++)
        {
            int i = rnd.Next(n);
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
            Assert.Equal(i + 0.5f, rp.X, 5);
            Assert.Equal(-i - 0.25f, rp.Y, 5);
            Assert.Equal(i * 0.01f, rv.Vx, 5);
            Assert.Equal(i * 0.02f, rv.Vy, 5);
            Assert.Equal(i + 1, rh.Value);
            Assert.Equal(i + 2, rm.Value);
            Assert.Equal(i * 0.05f, rr.Value, 5);
            Assert.Equal(2f + i * 0.001f, rs.Value, 5);
            Assert.Equal(i * 0.005f, ra.X, 5);
            Assert.Equal(i * 0.006f, ra.Y, 5);
            Assert.Equal(i % 4, rt.Id);
            Assert.Equal(i % 3, rst.Value);
            Assert.Equal(i * 10, rhp.Seed);
        }
    }

    /// <summary>
    /// 校验批量添加再移除一半 Position 后的存在性。
    /// </summary>
    public static void VerifyBulkAddRemove(int n)
    {
        using var world = new World();

        var entities = new List<Entity>(n);
        for (int i = 0; i < n; i++)
            entities.Add(world.CreateEntity());

        for (int i = 0; i < n; i++)
            world.AddComponent(entities[i], new Position { X = i, Y = i });

        for (int i = 0; i < n / 2; i++)
            world.RemoveComponent<Position>(entities[i]);

        for (int i = 0; i < n; i++)
        {
            if (i < n / 2)
                Assert.ThrowsAny<Exception>(() => world.GetComponent<Position>(entities[i]));
            else
                _ = world.GetComponent<Position>(entities[i]);
        }
    }

    /// <summary>
    /// 校验批量 <see cref="World.CreateEntity(System.Span{ExtenderApp.ECS.Entity})"/> 与批量销毁。
    /// </summary>
    public static void VerifyBatchCreate(int n)
    {
        using var world = new World();

        var entities = new Entity[n];

        world.CreateEntity(entities.AsSpan());

        int created = 0;
        for (int i = 0; i < entities.Length; i++)
        {
            if (!entities[i].IsEmpty)
                created++;
        }

        Assert.Equal(n, created);

        var sample = Math.Min(10, entities.Length);
        for (int i = 0; i < sample; i++)
            Assert.False(entities[i].IsEmpty);

        var validEntities = new List<Entity>(created);
        for (int i = 0; i < entities.Length; i++)
        {
            if (!entities[i].IsEmpty)
                validEntities.Add(entities[i]);
        }

        if (validEntities.Count > 0)
        {
            var destroyBuffer = validEntities.ToArray();
            world.DestroyEntity(destroyBuffer.AsSpan());
        }
    }

    /// <summary>
    /// 校验缺组件读取抛错、添加后可读。
    /// </summary>
    public static void VerifyExceptionBehavior()
    {
        using var world = new World();

        var e = world.CreateEntity();
        Assert.ThrowsAny<Exception>(() => world.GetComponent<Position>(e));

        world.AddComponent(e, new Position { X = 1, Y = 2 });
        var rp = world.GetComponent<Position>(e);
        Assert.Equal(1, rp.X);
        Assert.Equal(2, rp.Y);
    }

    /// <summary>
    /// 校验实体组件 Lookup / Operation 与给定规模下的校验和。
    /// </summary>
    public static void VerifyEntityComponentApi(int n)
    {
        using var world = new World();

        var e = world.CreateEntity();
        world.AddComponent(e, new Position { X = 1, Y = 2 });
        world.SetComponent(e, new Position { X = 3, Y = 4 });
        var p = world.GetComponent<Position>(e);
        Assert.Equal(3, p.X);
        Assert.Equal(4, p.Y);

        world.AddComponent(e, new Velocity { Vx = 7, Vy = 8 });
        var v = world.GetComponent<Velocity>(e);
        Assert.Equal(7, v.Vx);
        Assert.Equal(8, v.Vy);

        world.RemoveComponent<Velocity>(e);
        Assert.ThrowsAny<Exception>(() => world.GetComponent<Velocity>(e));

        var lookup = world.GetEntityComponentLookup(e);
        lookup.SetComponent(new Position { X = 11, Y = 22 });
        var lp = lookup.GetComponent<Position>();
        Assert.Equal(11, lp.X);
        Assert.Equal(22, lp.Y);
        Assert.True(lookup.TryGetComponent<Position>(out var tp) && tp.X == 11 && tp.Y == 22);

        var entities = new List<Entity>(n);
        var mask = new ComponentMask(ComponentType.Create<Position>());
        for (int i = 0; i < n; i++)
            entities.Add(world.CreateEntity(mask));
        for (int i = 0; i < n; i++)
            world.SetComponent(entities[i], new Position { X = i, Y = -i });

        double sum = 0;
        for (int i = 0; i < n; i++)
        {
            var rp = world.GetComponent<Position>(entities[i]);
            sum += rp.X * 3 + rp.Y;
        }

        double expected = 0;
        for (int i = 0; i < n; i++)
            expected += i * 3 + (-i);

        Assert.Equal(expected, sum, 9);
    }

    /// <summary>
    /// 小规模巨型组件对照：掩码 Create + Set + Get 的校验和与不含 Huge 的场景可区分。
    /// </summary>
    public static void VerifyHugeComponentComparisonSmall(int n)
    {
        Assert.True(n >= 4);
        double sumWithout = RunCreateSetGetScenarioChecksum(n, includeHuge: false);
        double sumWith = RunCreateSetGetScenarioChecksum(n, includeHuge: true);
        Assert.NotEqual(sumWithout, sumWith);
    }

    private static double RunCreateSetGetScenarioChecksum(int n, bool includeHuge)
    {
        using var world = new World();
        var mask = BuildMask(includeHuge);
        var entities = new List<Entity>(n);
        for (int i = 0; i < n; i++)
            entities.Add(world.CreateEntity(mask));

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

        double checksum = 0;
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

        return checksum;
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
}
