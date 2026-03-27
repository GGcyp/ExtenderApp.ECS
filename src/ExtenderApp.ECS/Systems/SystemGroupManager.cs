using ExtenderApp.Contracts;

namespace ExtenderApp.ECS.Systems
{
    /// <summary>
    /// 系统管理器：维护系统组上下文的双向链表，并提供按名称定位、插入与删除能力。
    /// </summary>
    /// <remarks>
    /// 设计说明：
    /// - 使用头哨兵节点 <c>HeadContext</c> 简化插入/删除逻辑；
    /// - 用户节点统一挂在 Head 之后；
    /// - 支持按名称在目标节点前后插入，或直接尾插；
    /// - 不允许删除 Head 节点。
    /// </remarks>
    internal sealed class SystemGroupManager : DisposableObject
    {
    }
}