using ExtenderApp.ECS.Queries;

namespace ExtenderApp.ECS.Accessors
{
    /// <summary>
    /// 在给定的 Archetype 链表上按原型遍历，返回对应原型的段级实体访问器（ChunkEntityAccessor）。
    /// 用于按 Archetype 组织的实体段枚举场景。
    /// </summary>
    internal struct ArchetypeEntityAccessor
    {
        private readonly ArchetypeSegment? _nextSegment;

        /// <summary>
        /// 使用指定的 Archetype 链表头初始化访问器。
        /// </summary>
        /// <param name="nextSegment">要遍历的 Archetype 链表头（可能为 null）。</param>
        public ArchetypeEntityAccessor(ArchetypeSegment? nextSegment)
        {
            _nextSegment = nextSegment;
        }

        /// <summary>
        /// 获取按 Archetype 枚举的段级实体访问器枚举器（用于 foreach 遍历）。
        /// </summary>
        public ChunkEntityAccessorEnumerator GetEnumerator() => new(_nextSegment);
    }
}