namespace ECSTest.CustomRuns;

/// <summary>
/// EntityQuery 构建、委托遍历与 RelationPair 演示。
/// </summary>
public static partial class CustomRunner
{
    /// <summary>
    /// EntityQuery 构建、筛选、委托与 foreach 解构的冒烟测试。
    /// </summary>
    public static void RunEntityQueryBuildTest()
    {
        Console.WriteLine("=== CustomRunner: EntityQuery Single Data Test ===");
        EntityQueryBuildVerification.Verify();
        Console.WriteLine("Result: OK");
        Console.WriteLine("========================================");
    }

    /// <summary>
    /// RelationPair / RelationMask 的基本用法演示（控制台输出）。
    /// </summary>
    public static void RunRelationPairCase(int _)
    {
        Console.WriteLine("=== CustomRunner: RelationPair 示例 ===");
        RelationPairVerification.Verify();
        Console.WriteLine("========================================");
    }
}
