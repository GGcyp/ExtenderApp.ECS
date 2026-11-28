using ExtenderApp.ECS.Entities;
using Xunit;

namespace ECSTest;

/// <summary>
/// 关系类型测试标记：测试用父子关系类型（空结构体）。
/// </summary>
public struct TestChildOf
{
}

/// <summary>
/// 关系类型测试标记：测试用所属关系类型（空结构体）。
/// </summary>
public struct TestOwnedBy
{
}

/// <summary>
/// 覆盖 <see cref="RelationMask" />、<see cref="RelationType" /> 的位图运算、枚举与越界防护行为。
/// </summary>
public sealed class RelationMaskTests
{
    [Fact]
    public void SetBit_IndexOutOfRange_Throws()
    {
        var mask = new RelationMask();

        Assert.Throws<ArgumentOutOfRangeException>(() => mask.SetBit(64));
        Assert.Throws<ArgumentOutOfRangeException>(() => mask.SetBit(-1));
        Assert.Throws<ArgumentOutOfRangeException>(() => mask.ClearBit(64));
        Assert.Throws<ArgumentOutOfRangeException>(() => mask.ClearBit(-1));
    }

    [Fact]
    public void Add_On_Off_Remove_RoundTrips()
    {
        var mask = new RelationMask();
        var type = RelationType.Create<TestChildOf>();

        Assert.False(mask.On(type));
        Assert.True(mask.Off(type));

        mask.Add(type);
        Assert.True(mask.On(type));
        Assert.False(mask.Off(type));
        Assert.Equal(1, mask.RelationCount);

        mask.Remove(type);
        Assert.False(mask.On(type));
        Assert.True(mask.IsEmpty);
    }

    [Fact]
    public void Enumerator_ReturnsSetRelationTypes()
    {
        var mask = new RelationMask();
        mask.Add<TestChildOf>();
        mask.Add<TestOwnedBy>();

        var seen = new List<RelationType>();
        foreach (var relationType in mask)
            seen.Add(relationType);

        Assert.Equal(2, seen.Count);
        Assert.Contains(RelationType.Create<TestChildOf>(), seen);
        Assert.Contains(RelationType.Create<TestOwnedBy>(), seen);
    }

    [Fact]
    public void On_OtherMask_DetectsIntersection()
    {
        var a = new RelationMask();
        a.Add<TestChildOf>();

        var b = new RelationMask();
        b.Add<TestOwnedBy>();

        var both = new RelationMask();
        both.Add<TestChildOf>();
        both.Add<TestOwnedBy>();

        Assert.True(both.On(a));
        Assert.True(both.On(b));
        Assert.False(a.On(b));
        Assert.True(a.Off(b));
    }

    [Fact]
    public void RelationType_Empty_IsEmpty()
    {
        Assert.True(RelationType.Empty.IsEmpty);
        Assert.False(RelationType.Create<TestChildOf>().IsEmpty);
    }
}
