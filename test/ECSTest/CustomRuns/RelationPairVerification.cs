using ExtenderApp.ECS;
using ExtenderApp.ECS.Abstract;
using ExtenderApp.ECS.Entities;
using Xunit;

namespace ECSTest.CustomRuns;

/// <summary>
/// 校验 <see cref="RelationPair"/> 与 <see cref="RelationMask"/> 的基本语义。
/// </summary>
public static class RelationPairVerification
{
    /// <summary>
    /// 校验关系对与掩码的相等性与包含关系。
    /// </summary>
    public static void Verify()
    {
        using var world = new World();

        var child = world.CreateEntity();
        var parentA = world.CreateEntity();
        var parentB = world.CreateEntity();
        var target = world.CreateEntity();

        var childOf = RelationType.Create<ChildOf>();
        var targetRel = RelationType.Create<Target>();

        var childOfA = RelationPair.Create(childOf, parentA);
        var childOfB = RelationPair.Create(childOf, parentB);
        var targetPair = RelationPair.Create(targetRel, target);

        Assert.NotEqual(childOfA, childOfB);

        var relationMask = new RelationMask();
        relationMask.Add(childOf);
        relationMask.Add(targetRel);
        Assert.True(relationMask.On(childOf));
        Assert.True(relationMask.On(targetRel));

        var ownerRelations = new Dictionary<Entity, List<RelationPair>>
        {
            [child] = new List<RelationPair> { childOfA, targetPair }
        };

        Assert.Equal(2, ownerRelations[child].Count);

        ownerRelations[child][0] = childOfB;
        Assert.Equal(childOfB, ownerRelations[child][0]);
    }
}
