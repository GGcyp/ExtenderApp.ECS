using ExtenderApp.ECS.Queries;

namespace ExtenderApp.ECS.Accessors
{
    /// <summary>
    /// 全局实体访问器：用于遍历某个 EntityQuery 命中的所有实体句柄。
    ///
    /// 该类型仅负责枚举实体句柄（不包含组件访问逻辑），通常由查询内部构造并返回给调用方用于 foreach 遍历。
    /// 为了避免堆分配，枚举器为值类型（struct），适用于热路径的遍历场景。
    /// </summary>
    internal struct EntityAccessor
    {
        /// <summary>
        /// 查询匹配的 Archetype 链表头（按匹配顺序链接的 ArchetypeSegment）。
        /// 若为 null 表示当前查询没有匹配到任何 Archetype 或实体。
        /// </summary>
        private readonly ArchetypeSegment? _nextSegment;

        /// <summary>
        /// 使用指定的 Archetype 链表创建实体访问器实例。
        /// </summary>
        /// <param name="nextSegment">要遍历的 Archetype 链表头（可能为 null）。</param>
        public EntityAccessor(ArchetypeSegment? nextSegment)
        {
            _nextSegment = nextSegment;
        }

        /// <summary>
        /// 获取结构体枚举器以遍历所有匹配的实体。
        /// 推荐在 foreach 中使用该方法以利用值类型枚举器避免装箱分配。
        /// </summary>
        /// <returns>一个新的 <see cref="EntityEnumerator"/> 实例。</returns>
        public EntityEnumerator GetEnumerator() => new(_nextSegment);

        /// <summary>
        /// 实体查询枚举器：按全局范围在匹配的 Archetype 与其内部段（chunk）上顺序遍历实体句柄。
        ///
        /// 实现细节：枚举器会在每个 Archetype 上构造对应的 <see cref="ArchetypeChunkEntityAccessor"/> 枚举器并逐段迭代，
        /// 以保持低分配和良好的局部性。
        /// </summary>
        public struct EntityEnumerator
        {
            private ArchetypeSegment? nextSegment;
            private ArchetypeChunkEntityAccessor.Enumerator _entityEnumerator;

            /// <summary>
            /// 当前枚举到的实体（在 <see cref="MoveNext"/> 返回 true 后有效）。
            /// </summary>
            public Entity Current { get; private set; }

            /// <summary>
            /// 使用指定的 Archetype 链表初始化枚举器。
            /// </summary>
            /// <param name="nextSegment">要遍历的 Archetype 链表头。</param>
            internal EntityEnumerator(ArchetypeSegment? nextSegment)
            {
                this.nextSegment = nextSegment;
                _entityEnumerator = default;
                Current = Entity.Empty;
            }

            /// <summary>
            /// 将枚举器推进到下一个实体。
            ///
            /// 迭代策略：先尝试从当前段的实体枚举器推进；若已耗尽则构造下一个 Archetype 的实体枚举器并继续尝试，
            /// 直至找到下一个实体或 Archetype 链表被遍历完毕。
            /// </summary>
            /// <returns>若存在下一个实体则返回 true；否则返回 false。</returns>
            public bool MoveNext()
            {
                while (true)
                {
                    if (_entityEnumerator.MoveNext())
                    {
                        Current = _entityEnumerator.Current;
                        return true;
                    }

                    if (nextSegment == null)
                        return false;

                    ArchetypeChunkEntityAccessor entityAccessor = new(nextSegment.Archetype);
                    _entityEnumerator = entityAccessor.GetEnumerator();
                    nextSegment = nextSegment.Next;
                }
            }
        }
    }
}