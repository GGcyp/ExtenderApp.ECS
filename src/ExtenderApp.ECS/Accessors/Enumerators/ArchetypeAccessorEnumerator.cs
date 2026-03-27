using ExtenderApp.ECS.Queries;

namespace ExtenderApp.ECS.Accessors
{
    /// <summary>
    /// 在查询匹配的 Archetype 链表上逐个定位包含目标组件列的 Archetype，并为每个 Archetype 提供对应的块级访问器（ChunkAccessor&lt;T&gt;）。
    /// </summary>
    internal struct ArchetypeAccessorEnumerator<T>
    {
        internal readonly ulong _version;
        private readonly ComponentType _componentType;
        private ArchetypeSegment? _nextSegment;

        /// <summary>
        /// 当前定位到的块级访问器（在 MoveNext 返回 true 后可用）。
        /// </summary>
        public ChunkAccessor<T> Current { get; private set; }

        /// <summary>
        /// 使用给定的 Archetype 链表头和版本号初始化定位器实例。
        /// </summary>
        internal ArchetypeAccessorEnumerator(ArchetypeSegment? nextSegment, ulong version)
        {
            _componentType = ComponentType.Create<T>();
            _version = version;
            _nextSegment = nextSegment;
        }

        /// <summary>
        /// 定位下一个包含目标组件列的 Archetype，并设置 <see cref="Current"/>；找到则返回 true，否则返回 false。
        /// </summary>
        public bool MoveNext()
        {
            while (_nextSegment != null)
            {
                var archetype = _nextSegment.Archetype;

                if (!archetype.ComponentMask.TryGetEncodedPosition(_componentType, out var columnIndex) ||
                    !archetype.TryGetChunkList<T>(columnIndex, out var list))
                {
                    _nextSegment = _nextSegment.Next;
                    continue;
                }

                Current = new(list, _version);
                _nextSegment = _nextSegment.Next;
                return true;
            }
            return false;
        }
    }
}