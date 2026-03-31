namespace ECSTest.Components;

/// <summary>
/// 托管引用组件（class）：
/// 用于验证 ECS 在托管堆存储（ManagedArchetTypeChunk）下对引用类型组件的读写、替换与遍历行为。
/// </summary>
public sealed class ManagedRefComponent
{
    /// <summary>
    /// 业务标识（用于断言区分实例）。
    /// </summary>
    public int Id { get; set; }

    /// <summary>
    /// 可选标签（用于调试与断言）。
    /// </summary>
    public string? Tag { get; set; }
}

