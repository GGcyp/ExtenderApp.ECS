using ExtenderApp.ECS.Archetypes;

namespace ExtenderApp.ECS.Accessors
{
    /// <summary>
    /// 在给定的块列表（ArchetypeChunkList&lt;T1&gt;）内按块顺序遍历并为每个块提供一个 `ComponentAccessor&lt;T1&gt;`。 用于在块级别对组件列进行迭代（可用于 Job/调度器将块作为工作单元分发）。
    /// </summary>
    internal struct ChunkAccessorEnumerator<T>
    {
        private const int DefaultSkipCount = 0;

        private readonly ArchetypeChunkList<T> _list;
        private ArchetypeChunk<T> _current;
        private ulong version;
        private readonly int _count;

        private int index;

        /// <summary>
        /// 当前块对应的块级访问器（在 MoveNext 返回 true 后有效）。
        /// </summary>
        public ComponentAccessor<T> Current => new(_current);

        /// <summary>
        /// 使用指定的 Archetype 和版本号初始化枚举器实例。枚举器将遍历 Archetype 中与类型 T1 相关的块，并跳过版本号小于指定版本的块（如果 version &gt; 0）。
        /// </summary>
        /// <param name="archetype">要枚举的 Archetype。</param>
        /// <param name="version">用于跳过旧版本块的版本号（0 表示不跳过）。</param>
        internal ChunkAccessorEnumerator(Archetype archetype, ulong version)
        {
            archetype.ComponentMask.TryGetEncodedPosition<T>(out var typeIndex);
            archetype.TryGetChunkList(typeIndex, out _list);
            this.version = version;
            index = 0;
            _current = default!;
            _count = _list.Count;
        }

        /// <summary>
        /// 在指定列块列表上从 <paramref name="startChunkListIndex" /> 起至多遍历 <paramref name="chunkListSpan" /> 个列表项（与实体段批对齐时使用）。
        /// </summary>
        internal ChunkAccessorEnumerator(ArchetypeChunkList<T> list, ulong version, int startChunkListIndex, int chunkListSpan)
        {
            _list = list;
            this.version = version;
            index = startChunkListIndex;
            _current = default!;
            int end = chunkListSpan <= 0 ? list.Count : startChunkListIndex + chunkListSpan;
            _count = end < list.Count ? end : list.Count;
        }

        /// <summary>
        /// 使用指定的块列表和版本号初始化枚举器实例。
        /// </summary>
        /// <param name="list">要枚举的块列表。</param>
        /// <param name="version">用于跳过旧版本块的版本号（0 表示不跳过）。</param>
        internal ChunkAccessorEnumerator(ArchetypeChunkList<T> list, ulong version) : this(list, version, DefaultSkipCount)
        {
        }

        /// <summary>
        /// 使用指定的块列表和版本号初始化枚举器实例。
        /// </summary>
        /// <param name="list">要枚举的块列表。</param>
        /// <param name="version">用于跳过旧版本块的版本号（0 表示不跳过）。</param>
        /// <param name="count">要枚举的块数量（默认为 0，表示枚举整个列表）。</param>
        internal ChunkAccessorEnumerator(ArchetypeChunkList<T> list, ulong version, int count)
        {
            _list = list;
            this.version = version;
            index = 0;
            _current = default!;
            _count = count <= DefaultSkipCount ? _list.Count : count;
        }

        /// <summary>
        /// 推进到下一个符合条件的块并设置 <see cref="Current" />；无更多块时返回 false。
        /// </summary>
        /// <returns>若成功推进到下一块则返回 true，否则返回 false。</returns>
        public bool MoveNext()
        {
            if (_list == null)
                return false;

            if (index > _count)
                return false;

            while (true)
            {
                if (!_list.TryGetChunk(index++, out _current))
                    return false;

                if (version != 0 && version > _current.Version)
                    continue;

                break;
            }
            return true;
        }
    }
}