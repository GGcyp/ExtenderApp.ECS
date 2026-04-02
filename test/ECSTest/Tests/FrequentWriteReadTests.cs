using ECSTest.Components;
using ExtenderApp.ECS;
using ExtenderApp.ECS.Accessors;
using Xunit;

namespace ECSTest.Tests;

/// <summary>
/// 验证在频繁写入与读取同一实体组件时，读取到的值是否与上一次写入一致。
/// </summary>
public sealed class FrequentWriteReadTests : EcsTestContext
{
    /// <summary>
    /// 频繁写入/读取 Position 组件，断言每次读取的值等于最近一次写入。
    /// </summary>
    [Fact]
    public void FrequentWriteRead_ValueStable()
    {
        // 创建用于测试的 World 实例
        using var world = new World("TestWorld_FrequentWriteRead");

        // 初始组件值
        var initial = new Position { X = 0f, Y = 0f };

        // 在 World 上创建多个实体并附加初始组件
        int entityCount = 8; // 创建实体数量
        var entities = new Entity[entityCount]; // 存储创建的实体引用
        for (int e = 0; e < entityCount; e++)
        {
            entities[e] = world.CreateEntity(initial);
        }

        // 迭代次数：总写读次数按实体数做了缩减以保持总体开销合理
        int iterations = 2000;

        for (int i = 1; i <= iterations; i++)
        {
            for (int e = 0; e < entityCount; e++)
            {
                // 每轮写入的新值（包含实体索引用于区分不同实体）
                var toWrite = new Position { X = i * 1000 + e, Y = -(i * 1000 + e) };

                // 写入组件
                world.SetComponent(entities[e], toWrite);

                // 立即读取组件
                var readBack = world.GetComponent<Position>(entities[e]);

                // 断言读取到的值与写入值一致
                Assert.Equal(toWrite.X, readBack.X);
                Assert.Equal(toWrite.Y, readBack.Y);
            }
        }
    }

    /// <summary>
    /// 使用 Query&lt;Position&gt; 与 RefRW 路径进行高性能写入/读取验证，断言每次读取的值等于最近一次写入。
    /// </summary>
    [Fact]
    public void FrequentWriteRead_WithQuery_ValueStable()
    {
        // 创建用于测试的 World 实例（查询路径）
        using var world = new World("TestWorld_FrequentWriteRead_Query");

        // 在 World 上创建多个实体并附加初始组件以便 Query 返回多行
        int entityCount = 8; // 查询实体数量
        for (int e = 0; e < entityCount; e++)
        {
            world.CreateEntity(new Position { X = 0f, Y = 0f });
        }

        // 通过 Query 获取 Position 列的遍历器
        var query = world.Query<Position>();

        // 迭代次数：按实体数量调整以保持总体开销合理
        int iterations = 2000;

        for (int i = 1; i <= iterations; i++)
        {
            // 每轮写入的新值基于计数器，逐行写入并立即验证
            int rowIndex = 0;
            foreach (var row in query)
            {
                row.Deconstruct(out RefRW<Position> posRef);
                var toWrite = new Position { X = i * 1000 + rowIndex, Y = -(i * 1000 + rowIndex) };
                posRef.Value = toWrite;

                var readBack = posRef.Value;
                Assert.Equal(toWrite.X, readBack.X);
                Assert.Equal(toWrite.Y, readBack.Y);

                rowIndex++;
            }
        }
    }

    /// <summary>
    /// 使用 Query&lt;Velocity&gt; 路径进行写入/读取验证。
    /// </summary>
    [Fact]
    public void FrequentWriteRead_Query_VelocityOnly()
    {
        // 创建用于测试的 World 实例
        using var world = new World("TestWorld_FrequentWriteRead_Query_Velocity");

        // 在 World 上创建多个实体并附加 Velocity 组件
        int entityCount = 8;
        for (int e = 0; e < entityCount; e++)
            world.CreateEntity(new Velocity { Vx = 0f, Vy = 0f });

        // 获取 Velocity 查询
        var query = world.Query<Velocity>();

        int iterations = 1000;
        for (int i = 1; i <= iterations; i++)
        {
            int rowIndex = 0;
            foreach (var row in query)
            {
                row.Deconstruct(out RefRW<Velocity> vRef);
                var toWrite = new Velocity { Vx = i * 1.0f + rowIndex, Vy = -(i * 1.0f + rowIndex) };
                vRef.Value = toWrite;
                var readBack = vRef.Value;
                Assert.Equal(toWrite.Vx, readBack.Vx);
                Assert.Equal(toWrite.Vy, readBack.Vy);
                rowIndex++;
            }
        }
    }

    /// <summary>
    /// 使用 Query&lt;Position, Velocity&gt; 路径进行写入/读取验证。
    /// </summary>
    [Fact]
    public void FrequentWriteRead_Query_PositionVelocity()
    {
        // 创建用于测试的 World 实例
        using var world = new World("TestWorld_FrequentWriteRead_Query_PV");

        // 在 World 上创建多个实体并附加 Position 和 Velocity 组件
        int entityCount = 8;
        for (int e = 0; e < entityCount; e++)
            world.CreateEntity(new Position { X = 0f, Y = 0f }, new Velocity { Vx = 0f, Vy = 0f });

        var query = world.Query<Position, Velocity>();

        int iterations = 1000;
        for (int i = 1; i <= iterations; i++)
        {
            int rowIndex = 0;
            foreach ((RefRW<Position> pRef, RefRW<Velocity> vRef) in query)
            {
                var pWrite = new Position { X = i + rowIndex, Y = -(i + rowIndex) };
                var vWrite = new Velocity { Vx = (i + rowIndex) * 0.5f, Vy = -(i + rowIndex) * 0.5f };
                pRef.Value = pWrite;
                vRef.Value = vWrite;

                var pRead = pRef.Value;
                var vRead = vRef.Value;

                Assert.Equal(pWrite.X, pRead.X);
                Assert.Equal(pWrite.Y, pRead.Y);
                Assert.Equal(vWrite.Vx, vRead.Vx);
                Assert.Equal(vWrite.Vy, vRead.Vy);
                rowIndex++;
            }
        }
    }

    /// <summary>
    /// 使用 Query&lt;Position, Velocity, Health, Mana&gt; 路径进行四组件写入/读取验证。
    /// </summary>
    [Fact]
    public void FrequentWriteRead_Query_FourComponents()
    {
        // 创建用于测试的 World 实例
        using var world = new World("TestWorld_FrequentWriteRead_Query_T4");

        // 在 World 上创建多个实体并附加四个组件
        int entityCount = 8;
        for (int e = 0; e < entityCount; e++)
            world.CreateEntity(
                new Position { X = 0f, Y = 0f },
                new Velocity { Vx = 0f, Vy = 0f },
                new Health { Value = 100 },
                new Mana { Value = 50 }
            );

        var query = world.Query<Position, Velocity, Health, Mana>();

        int iterations = 500;
        for (int i = 1; i <= iterations; i++)
        {
            int rowIndex = 0;
            foreach ((RefRW<Position> pRef, RefRW<Velocity> vRef, RefRW<Health> hRef, RefRW<Mana> mRef) in query)
            {
                var pWrite = new Position { X = i + rowIndex, Y = -(i + rowIndex) };
                var vWrite = new Velocity { Vx = (i + rowIndex) * 0.1f, Vy = -(i + rowIndex) * 0.1f };
                var hWrite = new Health { Value = i + rowIndex };
                var mWrite = new Mana { Value = (i + rowIndex) * 2 };

                pRef.Value = pWrite;
                vRef.Value = vWrite;
                hRef.Value = hWrite;
                mRef.Value = mWrite;

                var pRead = pRef.Value;
                var vRead = vRef.Value;
                var hRead = hRef.Value;
                var mRead = mRef.Value;

                Assert.Equal(pWrite.X, pRead.X);
                Assert.Equal(pWrite.Y, pRead.Y);
                Assert.Equal(vWrite.Vx, vRead.Vx);
                Assert.Equal(vWrite.Vy, vRead.Vy);
                Assert.Equal(hWrite.Value, hRead.Value);
                Assert.Equal(mWrite.Value, mRead.Value);
                rowIndex++;
            }
        }
    }

    /// <summary>
    /// 使用 Query&lt;Position, Velocity, Health, Acceleration, Team&gt; 路径进行五组件写入/读取验证。
    /// </summary>
    [Fact]
    public void FrequentWriteRead_Query_FiveComponents()
    {
        // 创建用于测试的 World 实例
        using var world = new World("TestWorld_FrequentWriteRead_Query_T5");

        // 在 World 上创建多个实体并附加五个组件
        int entityCount = 8;
        for (int e = 0; e < entityCount; e++)
            world.CreateEntity(
                new Position { X = 0f, Y = 0f },
                new Velocity { Vx = 0f, Vy = 0f },
                new Health { Value = 100 },
                new Acceleration { X = 0f, Y = 0f },
                new Team { Id = 1 }
            );

        var query = world.Query<Position, Velocity, Health, Acceleration, Team>();

        int iterations = 250;
        for (int i = 1; i <= iterations; i++)
        {
            int rowIndex = 0;
            for (int attempt = 0; attempt < 1; attempt++)
            {
                foreach (var row in query)
                {
                    row.Deconstruct(out RefRW<Position> pRef, out RefRW<Velocity> vRef, out RefRW<Health> hRef, out RefRW<Acceleration> aRef, out RefRW<Team> tRef);
                    var pWrite = new Position { X = i + rowIndex, Y = -(i + rowIndex) };
                    var vWrite = new Velocity { Vx = (i + rowIndex) * 0.2f, Vy = -(i + rowIndex) * 0.2f };
                    var hWrite = new Health { Value = i + rowIndex };
                    var aWrite = new Acceleration { X = (i + rowIndex) * 0.01f, Y = -(i + rowIndex) * 0.01f };
                    var tWrite = new Team { Id = (i + rowIndex) % 5 };

                    pRef.Value = pWrite;
                    vRef.Value = vWrite;
                    hRef.Value = hWrite;
                    aRef.Value = aWrite;
                    tRef.Value = tWrite;

                    var pRead = pRef.Value;
                    var vRead = vRef.Value;
                    var hRead = hRef.Value;
                    var aRead = aRef.Value;
                    var tRead = tRef.Value;

                    Assert.Equal(pWrite.X, pRead.X);
                    Assert.Equal(pWrite.Y, pRead.Y);
                    Assert.Equal(vWrite.Vx, vRead.Vx);
                    Assert.Equal(vWrite.Vy, vRead.Vy);
                    Assert.Equal(hWrite.Value, hRead.Value);
                    Assert.Equal(aWrite.X, aRead.X);
                    Assert.Equal(aWrite.Y, aRead.Y);
                    Assert.Equal(tWrite.Id, tRead.Id);
                    rowIndex++;
                }
            }
        }
    }
}