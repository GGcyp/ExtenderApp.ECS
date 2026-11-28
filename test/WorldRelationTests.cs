using ECSTest.Components;
using ExtenderApp.ECS;
using ExtenderApp.ECS.Entities;
using Xunit;

namespace ECSTest;

/// <summary>
/// 覆盖 <see cref="World" /> 关系 API 的行为：目标行内存储、原地改写、迁移保留、按目标查询与悬挂目标语义。
/// </summary>
public sealed class WorldRelationTests
{
    [Fact]
    public void AddRelation_RoundTripsTarget()
    {
        using var world = new World();
        var child = world.CreateEntity();
        var parent = world.CreateEntity();

        world.AddRelation<TestChildOf>(child, parent);

        Assert.True(world.TryGetRelationTarget<TestChildOf>(child, out var target));
        Assert.Equal(parent, target);
    }

    [Fact]
    public void AddRelation_OverwriteTarget_ReplacesValue()
    {
        using var world = new World();
        var child = world.CreateEntity();
        var parent1 = world.CreateEntity();
        var parent2 = world.CreateEntity();

        world.AddRelation<TestChildOf>(child, parent1);
        world.AddRelation<TestChildOf>(child, parent2);

        Assert.True(world.TryGetRelationTarget<TestChildOf>(child, out var target));
        Assert.Equal(parent2, target);
    }

    [Fact]
    public void AddRelation_TwoEntitiesSameArchetype_DifferentTargets_Isolated()
    {
        using var world = new World();
        var child1 = world.CreateEntity();
        var child2 = world.CreateEntity();
        var parent1 = world.CreateEntity();
        var parent2 = world.CreateEntity();

        // 两个子实体同 Archetype 起步，各自指向不同父实体：目标值按行隔离，互不影响
        world.AddRelation<TestChildOf>(child1, parent1);
        world.AddRelation<TestChildOf>(child2, parent2);

        Assert.True(world.TryGetRelationTarget<TestChildOf>(child1, out var target1));
        Assert.True(world.TryGetRelationTarget<TestChildOf>(child2, out var target2));
        Assert.Equal(parent1, target1);
        Assert.Equal(parent2, target2);

        // 改写 child1 的目标不影响 child2
        world.AddRelation<TestChildOf>(child1, parent2);
        Assert.True(world.TryGetRelationTarget<TestChildOf>(child2, out target2));
        Assert.Equal(parent2, target2);
    }

    [Fact]
    public void AddRelation_ThenAddComponent_MigratesAndKeepsRelation()
    {
        using var world = new World();
        var child = world.CreateEntity();
        var parent = world.CreateEntity();

        world.AddRelation<TestChildOf>(child, parent);
        world.AddComponent(child, new TestPosition { X = 1, Y = 2 });

        Assert.True(world.TryGetRelationTarget<TestChildOf>(child, out var target));
        Assert.Equal(parent, target);

        var pos = world.GetComponent<TestPosition>(child);
        Assert.Equal(1f, pos.X);
        Assert.Equal(2f, pos.Y);
    }

    [Fact]
    public void RemoveComponent_WithRelation_KeepsRelation()
    {
        using var world = new World();
        var child = world.CreateEntity(new TestPosition { X = 3, Y = 4 });
        var parent = world.CreateEntity();

        world.AddRelation<TestChildOf>(child, parent);
        world.RemoveComponent<TestPosition>(child);

        // 回归：RemoveComponent 迁移不得静默丢失关系
        Assert.True(world.TryGetRelationTarget<TestChildOf>(child, out var target));
        Assert.Equal(parent, target);
    }

    [Fact]
    public void RemoveRelation_RemovesType()
    {
        using var world = new World();
        var child = world.CreateEntity();
        var parent = world.CreateEntity();

        world.AddRelation<TestChildOf>(child, parent);
        world.RemoveRelation<TestChildOf>(child);

        Assert.False(world.TryGetRelationTarget<TestChildOf>(child, out _));
    }

    [Fact]
    public void GetEntitiesWithRelation_FiltersByTarget()
    {
        using var world = new World();
        var child1 = world.CreateEntity();
        var child2 = world.CreateEntity();
        var child3 = world.CreateEntity();
        var parent1 = world.CreateEntity();
        var parent2 = world.CreateEntity();

        world.AddRelation<TestChildOf>(child1, parent1);
        world.AddRelation<TestChildOf>(child2, parent1);
        world.AddRelation<TestChildOf>(child3, parent2);

        var childrenOfParent1 = new List<Entity>();
        foreach (var entity in world.GetEntitiesWithRelation<TestChildOf>(parent1))
            childrenOfParent1.Add(entity);

        Assert.Equal(2, childrenOfParent1.Count);
        Assert.Contains(child1, childrenOfParent1);
        Assert.Contains(child2, childrenOfParent1);
        Assert.DoesNotContain(child3, childrenOfParent1);
    }

    [Fact]
    public void TryGetRelationTarget_DeadTarget_IsAliveFalse()
    {
        using var world = new World();
        var child = world.CreateEntity();
        var parent = world.CreateEntity();

        world.AddRelation<TestChildOf>(child, parent);
        world.DestroyEntity(parent);

        // 悬挂目标：行内值保留，调用方以 IsAlive 惰性校验
        Assert.True(world.TryGetRelationTarget<TestChildOf>(child, out var target));
        Assert.Equal(parent, target);
        Assert.False(world.EManager.IsAlive(target));
    }

    [Fact]
    public void EntityQuery_WithRelationType_OnlyMatchesEntitiesWithRelation()
    {
        using var world = new World();
        var parent = world.CreateEntity();
        var with1 = world.CreateEntity();
        var with2 = world.CreateEntity();
        var without1 = world.CreateEntity();
        var without2 = world.CreateEntity();

        world.AddRelation<TestChildOf>(with1, parent);
        world.AddRelation<TestChildOf>(with2, parent);

        // 按关系类型查询（不含目标过滤）：只返回含该关系类型的实体
        var query = world.QueryBuilder().WithRelation<TestChildOf>().Build();

        var found = new List<Entity>();
        foreach (var entity in query)
            found.Add(entity);

        Assert.Equal(2, found.Count);
        Assert.Contains(with1, found);
        Assert.Contains(with2, found);
        Assert.DoesNotContain(without1, found);
        Assert.DoesNotContain(without2, found);
    }

    [Fact]
    public void EntityQuery_WithRelationType_AfterRemoveRelation_ExcludesEntity()
    {
        using var world = new World();
        var parent = world.CreateEntity();
        var child = world.CreateEntity();

        world.AddRelation<TestChildOf>(child, parent);
        var query = world.QueryBuilder().WithRelation<TestChildOf>().Build();

        int before = 0;
        foreach (var entity in query)
            before++;
        Assert.Equal(1, before);

        world.RemoveRelation<TestChildOf>(child);

        int found = 0;
        foreach (var entity in query)
            found++;
        Assert.Equal(0, found);
    }

    [Fact]
    public void CreateEntityWithRelation_SetsTargetAtCreation()
    {
        using var world = new World();
        var parent = world.CreateEntity();

        var child = world.CreateEntityWithRelation<TestChildOf>(parent);

        Assert.True(world.TryGetRelationTarget<TestChildOf>(child, out var target));
        Assert.Equal(parent, target);

        // 复用同一 Archetype，同表实体目标互不影响
        var child2 = world.CreateEntityWithRelation<TestChildOf>(parent);
        Assert.True(world.TryGetRelationTarget<TestChildOf>(child2, out var target2));
        Assert.Equal(parent, target2);
    }

    [Fact]
    public void GetEntitiesWithRelation_QueryThenDestroyHalf_QueryShrinks()
    {
        using var world = new World();
        var parent = world.CreateEntity();
        const int count = 1000;
        var children = new Entity[count];
        for (int i = 0; i < count; i++)
        {
            children[i] = world.CreateEntity();
            world.AddRelation<TestChildOf>(children[i], parent);
        }

        // 先收集再销毁（遍历期间不允许结构变更，先收集是安全模式）
        var snapshot = new List<Entity>();
        foreach (var entity in world.GetEntitiesWithRelation<TestChildOf>(parent))
            snapshot.Add(entity);
        Assert.Equal(count, snapshot.Count);

        for (int i = 0; i < count / 2; i++)
            world.DestroyEntity(snapshot[i]);

        int remaining = 0;
        foreach (var entity in world.GetEntitiesWithRelation<TestChildOf>(parent))
        {
            remaining++;
            Assert.DoesNotContain(entity, snapshot.Take(count / 2));
        }
        Assert.Equal(count / 2, remaining);
    }

    [Fact]
    public void GetEntitiesWithRelation_DestroyAllQueried_EmptyResult()
    {
        using var world = new World();
        var parent = world.CreateEntity();
        const int count = 500;
        for (int i = 0; i < count; i++)
        {
            var child = world.CreateEntity();
            world.AddRelation<TestChildOf>(child, parent);
        }

        var snapshot = new List<Entity>();
        foreach (var entity in world.GetEntitiesWithRelation<TestChildOf>(parent))
            snapshot.Add(entity);

        foreach (var entity in snapshot)
            world.DestroyEntity(entity);

        int remaining = 0;
        foreach (var entity in world.GetEntitiesWithRelation<TestChildOf>(parent))
            remaining++;
        Assert.Equal(0, remaining);
    }

    [Fact]
    public void GetEntitiesWithRelation_DeadTarget_StillReturnsStaleEntities()
    {
        using var world = new World();
        var parent = world.CreateEntity();
        var child = world.CreateEntity();
        world.AddRelation<TestChildOf>(child, parent);

        world.DestroyEntity(parent);

        // 悬挂语义固化：行内目标值不清除，按目标查询仍会返回指向已销毁目标的实体，
        // 调用方应对结果逐个 IsAlive 校验（与 TryGetRelationTarget 的惰性语义一致）。
        var found = new List<Entity>();
        foreach (var entity in world.GetEntitiesWithRelation<TestChildOf>(parent))
            found.Add(entity);

        Assert.Single(found);
        Assert.Equal(child, found[0]);
        Assert.False(world.EManager.IsAlive(parent));
    }

    [Fact]
    public void GetChildren_ReturnsEntitiesTargetingEntity()
    {
        using var world = new World();
        var parent = world.CreateEntity();
        var child1 = world.CreateEntity();
        var child2 = world.CreateEntity();
        var other = world.CreateEntity();
        var outsider = world.CreateEntity();

        world.AddRelation<TestChildOf>(child1, parent);
        world.AddRelation<TestChildOf>(child2, parent);
        world.AddRelation<TestChildOf>(other, outsider);

        var children = new List<Entity>();
        foreach (var entity in world.GetChildren(parent))
            children.Add(entity);

        Assert.Equal(2, children.Count);
        Assert.Contains(child1, children);
        Assert.Contains(child2, children);
        Assert.DoesNotContain(other, children);
    }

    [Fact]
    public void GetChildren_SameEntityMultipleRelationTypes_AddedOnce()
    {
        using var world = new World();
        var parent = world.CreateEntity();
        var child = world.CreateEntity();

        world.AddRelation<TestChildOf>(child, parent);
        world.AddRelation<TestOwnedBy>(child, parent);

        var children = new List<Entity>();
        foreach (var entity in world.GetChildren(parent))
            children.Add(entity);

        Assert.Single(children);
        Assert.Equal(child, children[0]);
    }

    [Fact]
    public void DestroyEntity_CascadeTrue_DeletesChildren()
    {
        using var world = new World();
        var parent = world.CreateEntity();
        var child = world.CreateEntity();
        world.AddRelation<TestChildOf>(child, parent);

        world.DestroyEntity(parent, cascade: true);

        Assert.False(world.EManager.IsAlive(parent));
        Assert.False(world.EManager.IsAlive(child));
    }

    [Fact]
    public void DestroyEntity_CascadeTrue_RecursiveMultiLevel()
    {
        using var world = new World();
        var a = world.CreateEntity();
        var b = world.CreateEntity();
        var c = world.CreateEntity();
        world.AddRelation<TestChildOf>(b, a);
        world.AddRelation<TestChildOf>(c, b);

        world.DestroyEntity(a, cascade: true);

        Assert.False(world.EManager.IsAlive(a));
        Assert.False(world.EManager.IsAlive(b));
        Assert.False(world.EManager.IsAlive(c));
    }

    [Fact]
    public void DestroyEntity_CascadeTrue_Cycle_TerminatesAndDeletesBoth()
    {
        using var world = new World();
        var a = world.CreateEntity();
        var b = world.CreateEntity();
        world.AddRelation<TestChildOf>(a, b);
        world.AddRelation<TestChildOf>(b, a);

        world.DestroyEntity(a, cascade: true);

        Assert.False(world.EManager.IsAlive(a));
        Assert.False(world.EManager.IsAlive(b));
    }

    [Fact]
    public void DestroyEntity_CascadeTrue_Batch_DeletesChildrenOfAll()
    {
        using var world = new World();
        var p1 = world.CreateEntity();
        var p2 = world.CreateEntity();
        var c1 = world.CreateEntity();
        var c2 = world.CreateEntity();
        world.AddRelation<TestChildOf>(c1, p1);
        world.AddRelation<TestChildOf>(c2, p2);

        world.DestroyEntity(new[] { p1, p2 }.AsSpan(), cascade: true);

        Assert.False(world.EManager.IsAlive(p1));
        Assert.False(world.EManager.IsAlive(p2));
        Assert.False(world.EManager.IsAlive(c1));
        Assert.False(world.EManager.IsAlive(c2));
    }

    [Fact]
    public void DestroyEntity_CascadeFalse_KeepsHangingChildren()
    {
        using var world = new World();
        var parent = world.CreateEntity();
        var child = world.CreateEntity();
        world.AddRelation<TestChildOf>(child, parent);

        world.DestroyEntity(parent);

        Assert.False(world.EManager.IsAlive(parent));
        Assert.True(world.EManager.IsAlive(child));
    }
}
