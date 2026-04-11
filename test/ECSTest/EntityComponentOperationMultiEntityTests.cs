using ECSTest.Components;
using ExtenderApp.ECS;
using Xunit;

namespace ECSTest;

/// <summary>
/// 验证批量实体通过 <see cref="World.GetEntityComponentOperation"/> 写入组件后，读回数据与写入一致。
/// </summary>
public sealed class EntityComponentOperationMultiEntityTests
{
    /// <summary>
    /// 批量创建空实体后，对每个实体用 <c>AddComponent</c> 写入互不相同的 <see cref="TestPosition"/>，再逐实体校验读回值。
    /// </summary>
    /// <param name="count">实体数量。</param>
    [Theory]
    [InlineData(2)]
    [InlineData(32)]
    [InlineData(128)]
    [InlineData(1280)]
    [InlineData(12800)]
    public void ManyEntities_AddComponentPositionViaOperation_RoundTripsPerEntity(int count)
    {
        using var world = new World();
        Span<Entity> entities = stackalloc Entity[count];
        world.CreateEntity(entities);

        for (var i = 0; i < count; i++)
        {
            var expected = new TestPosition { X = i * 1.5f, Y = i * 2.5f + 0.25f };
            world.GetEntityComponentOperation(entities[i]).AddComponent(expected);
        }

        for (var i = 0; i < count; i++)
        {
            var read = world.GetComponent<TestPosition>(entities[i]);
            Assert.Equal(i * 1.5f, read.X, 5);
            Assert.Equal(i * 2.5f + 0.25f, read.Y, 5);
        }

        Assert.Equal(count, world.Query<TestPosition>().Count);
    }

    /// <summary>
    /// 多实体先加位置再加速度（触发 Archetype 迁移），通过操作对象链式写入，读回应与各自写入值一致且互不串扰。
    /// </summary>
    [Fact]
    public void ManyEntities_AddPositionThenVelocityViaOperation_Chain_RoundTripsPerEntity()
    {
        const int count = 64;
        using var world = new World();
        Span<Entity> entities = stackalloc Entity[count];
        world.CreateEntity(entities);

        for (var i = 0; i < count; i++)
        {
            var pos = new TestPosition { X = i, Y = -i };
            var vel = new TestVelocity { Dx = i * 0.1f, Dy = i * 0.2f };
            world.GetEntityComponentOperation(entities[i])
                .AddComponent(pos)
                .AddComponent(vel);
        }

        for (var i = 0; i < count; i++)
        {
            var p = world.GetComponent<TestPosition>(entities[i]);
            var v = world.GetComponent<TestVelocity>(entities[i]);
            Assert.Equal((float)i, p.X);
            Assert.Equal((float)-i, p.Y);
            Assert.Equal(i * 0.1f, v.Dx, 5);
            Assert.Equal(i * 0.2f, v.Dy, 5);
        }

        Assert.Equal(count, world.Query<TestPosition, TestVelocity>().Count);
    }
}
