using ECSTest.Components;
using ExtenderApp.ECS;
using ExtenderApp.ECS.Accessors;
using Xunit;

namespace ECSTest.Tests;

/// <summary>
/// 针对 ECS 存储托管堆（ManagedArchetTypeChunk）场景的 class（引用类型）组件测试。
/// </summary>
public sealed class ManagedHeapClassComponentTests : EcsTestContext
{
    /// <summary>
    /// CreateEntity(class) 后应能 GetComponent 读回同一引用实例。
    /// </summary>
    [Fact]
    public void ManagedClassComponentCreateAndGetKeepsSameReference()
    {
        using var world = new World("TestWorld_ManagedClass_CreateGet");

        var created = new ManagedRefComponent { Id = 1, Tag = "created" };
        Console.WriteLine($"[ManagedClassComponentCreateAndGetKeepsSameReference] Input: Id={created.Id}, Tag={created.Tag}");
        var entity = world.CreateEntity(created);

        var fetched = world.GetComponent<ManagedRefComponent>(entity);
        Console.WriteLine($"[ManagedClassComponentCreateAndGetKeepsSameReference] Output: Id={fetched.Id}, Tag={fetched.Tag}");
        Assert.Same(created, fetched);
        Assert.Equal(1, fetched.Id);
        Assert.Equal("created", fetched.Tag);
        Console.WriteLine("[ManagedClassComponentCreateAndGetKeepsSameReference] Assertion passed: reference and values are correct.");
    }

    /// <summary>
    /// SetComponent(class) 覆盖后，应能读回新引用实例，且旧引用不被篡改。
    /// </summary>
    [Fact]
    public void ManagedClassComponentSetComponentReplacesReference()
    {
        using var world = new World("TestWorld_ManagedClass_SetReplace");

        var oldValue = new ManagedRefComponent { Id = 1, Tag = "old" };
        Console.WriteLine($"[ManagedClassComponentSetComponentReplacesReference] Initial: Id={oldValue.Id}, Tag={oldValue.Tag}");
        var entity = world.CreateEntity(oldValue);

        var newValue = new ManagedRefComponent { Id = 2, Tag = "new" };
        Console.WriteLine($"[ManagedClassComponentSetComponentReplacesReference] New value: Id={newValue.Id}, Tag={newValue.Tag}");
        world.SetComponent(entity, newValue);

        var fetched = world.GetComponent<ManagedRefComponent>(entity);
        Console.WriteLine($"[ManagedClassComponentSetComponentReplacesReference] Output: Id={fetched.Id}, Tag={fetched.Tag}");
        Assert.Same(newValue, fetched);
        Assert.NotSame(oldValue, fetched);

        Assert.Equal(2, fetched.Id);
        Assert.Equal("new", fetched.Tag);

        Assert.Equal(1, oldValue.Id);
        Assert.Equal("old", oldValue.Tag);
        Console.WriteLine("[ManagedClassComponentSetComponentReplacesReference] Assertion passed: reference replaced and old value unchanged.");
    }

    /// <summary>
    /// 通过 Query&lt;class&gt; 遍历时，允许就地替换槽位引用，并在遍历后可被 GetComponent 读回。
    /// </summary>
    [Fact]
    public void ManagedClassComponentQueryAllowsReplacingReference()
    {
        using var world = new World("TestWorld_ManagedClass_QueryReplace");

        var e1 = world.CreateEntity(new ManagedRefComponent { Id = 1, Tag = "a" });
        var e2 = world.CreateEntity(new ManagedRefComponent { Id = 2, Tag = "b" });
        Console.WriteLine("[ManagedClassComponentQueryAllowsReplacingReference] Created entities with ManagedRefComponent Ids: 1, 2");

        int replaced = 0;
        foreach (var row in world.Query<ManagedRefComponent>())
        {
            row.Deconstruct(out RefRW<ManagedRefComponent> comp);
            Console.WriteLine($"[ManagedClassComponentQueryAllowsReplacingReference] Before replace: Id={comp.Value.Id}, Tag={comp.Value.Tag}");
            comp.Value = new ManagedRefComponent { Id = 100 + comp.Value.Id, Tag = "replaced" };
            Console.WriteLine($"[ManagedClassComponentQueryAllowsReplacingReference] After replace: Id={comp.Value.Id}, Tag={comp.Value.Tag}");
            replaced++;
        }

        Assert.Equal(2, replaced);

        var c1 = world.GetComponent<ManagedRefComponent>(e1);
        var c2 = world.GetComponent<ManagedRefComponent>(e2);

        Assert.Equal(101, c1.Id);
        Assert.Equal(102, c2.Id);
        Assert.Equal("replaced", c1.Tag);
        Assert.Equal("replaced", c2.Tag);
        Console.WriteLine($"[ManagedClassComponentQueryAllowsReplacingReference] Final values: e1(Id={c1.Id}, Tag={c1.Tag}), e2(Id={c2.Id}, Tag={c2.Tag})");
        Console.WriteLine("[ManagedClassComponentQueryAllowsReplacingReference] Assertion passed: query-based replacements are correct.");
    }

    /// <summary>
    /// 验证结构体组件与 class 组件混合原型下，二者都能被正确存取与遍历。
    /// </summary>
    [Fact]
    public void ManagedClassComponentWithStructArchetypeWorksCorrectly()
    {
        using var world = new World("TestWorld_ManagedClass_MixedArchetype");

        ComponentType.Create<Position>();

        var e1 = world.CreateEntity(new ManagedRefComponent { Id = 10, Tag = "p1" });
        world.AddComponent(e1, new Position { X = 1f, Y = 2f });

        var e2 = world.CreateEntity(new ManagedRefComponent { Id = 20, Tag = "p2" });
        var operation = world.GetEntityComponentOperation(e2);
        operation.AddComponent(new Position { X = 3f, Y = 4f });
        operation.AddComponent(new Health { Value = 100 });

        Console.WriteLine("[ManagedClassComponentWithStructArchetypeWorksCorrectly] Created 2 entities with Position + ManagedRefComponent.");

        // 通过 Query<Position, ManagedRefComponent> 遍历并累加 X 与 Id。
        float sumX = 0f;
        int sumId = 0;
        int count = 0;

        var query = world.Query<Position, ManagedRefComponent>();
        foreach ((Position pos, ManagedRefComponent managed) in query)
        {
            Console.WriteLine($"[ManagedClassComponentWithStructArchetypeWorksCorrectly] Row before: Position=({pos.X},{pos.Y}), Id={managed.Id}, Tag={managed.Tag}");

            sumX += pos.X;
            sumId += managed.Id;
            count++;
        }

        Console.WriteLine($"[ManagedClassComponentWithStructArchetypeWorksCorrectly] Aggregated: count={count}, sumX={sumX}, sumId={sumId}");

        Assert.Equal(2, count);
        Assert.Equal(1f + 3f, sumX);
        Assert.Equal(10 + 20, sumId);
        Console.WriteLine("[ManagedClassComponentWithStructArchetypeWorksCorrectly] Assertion passed: struct+class mixed archetype values are correct.");
    }
}