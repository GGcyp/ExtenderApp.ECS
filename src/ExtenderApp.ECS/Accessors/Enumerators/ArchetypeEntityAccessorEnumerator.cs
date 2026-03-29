using ExtenderApp.ECS.Queries;

namespace ExtenderApp.ECS.Accessors
{
    /// <summary>
    /// 按 Archetype 链表逐个遍历，每次返回对应 Archetype 的段级实体访问器（ChunkEntityAccessor）。
    /// </summary>
    internal struct ArchetypeEntityAccessorEnumerator
    {
        private ArchetypeSegment? nextSegment;

        /// <summary>
        /// 当前定位到的段级实体访问器（在 MoveNext 返回 true 后有效）。
        /// </summary>
        public ChunkEntityAccessor Current { get; private set; }

        /// <summary>
        /// 使用指定的 Archetype 链表头初始化枚举器。
        /// </summary>
        internal ArchetypeEntityAccessorEnumerator(ArchetypeSegment? nextSegment)
        {
            this.nextSegment = nextSegment;
        }

        /// <summary>
        /// 推进到下一个 Archetype 并设置 <see cref="Current" />；无更多项时返回 false。
        /// </summary>
        public bool MoveNext()
        {
            if (nextSegment == null)
                return false;

            Current = new(nextSegment.Archetype);
            nextSegment = nextSegment.Next;
            return true;
        }
    }
}