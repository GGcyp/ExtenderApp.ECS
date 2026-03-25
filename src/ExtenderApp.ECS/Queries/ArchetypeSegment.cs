namespace ExtenderApp.ECS.Queries
{
    /// <summary>
    /// 表示查询匹配结果中的一个节点（链表单元）。
    /// 每个节点持有一个与查询匹配的 <see cref="Archetype"/> 实例，并通过 <see cref="Next"/> 链接到下一个匹配的节点，
    /// 从而形成一个轻量级的单向链表，便于遍历所有匹配的 Archetype。
    ///
    /// 设计说明：使用链表而非数组可以减少在匹配重建时的内存分配与复制开销，
    /// 适合匹配结果通常以追加式构建且仅需顺序遍历的场景。
    /// </summary>
    internal class ArchetypeSegment
    {
        /// <summary>
        /// 节点持有的 Archetype 引用，表示一个满足查询条件的 Archetype。
        /// </summary>
        public Archetype Archetype;

        /// <summary>
        /// 指向链表中的下一个匹配节点；若为 null 表示当前为链表尾部。
        /// </summary>
        public ArchetypeSegment? Next { get; set; }

        /// <summary>
        /// 使用给定的 <paramref name="archetype"/> 创建一个新的链表节点。
        /// </summary>
        /// <param name="archetype">要包装的 Archetype 实例。</param>
        public ArchetypeSegment(Archetype archetype)
        {
            Archetype = archetype;
        }
    }
}