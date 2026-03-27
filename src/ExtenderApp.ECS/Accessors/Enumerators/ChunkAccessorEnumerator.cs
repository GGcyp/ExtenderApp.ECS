using ExtenderApp.ECS.Archetypes;

namespace ExtenderApp.ECS.Accessors
{
    /// <summary>
    /// 在给定的块列表（ArchetypeChunkList&lt;T&gt;）内按块顺序遍历并为每个块提供一个 `ComponentAccessor&lt;T&gt;`。
    /// 用于在块级别对组件列进行迭代（可用于 Job/调度器将块作为工作单元分发）。
    /// </summary>
    internal struct ChunkAccessorEnumerator<T>
    {
        private readonly ArchetypeChunkList<T> _list;
        private ArchetypeChunk<T> _current;
        private ulong version;

        private int chunkIndex;

        /// <summary>
        /// 当前块对应的块级访问器（在 MoveNext 返回 true 后有效）。
        /// </summary>
        public ComponentAccessor<T> Current => new(_current);

        internal ChunkAccessorEnumerator(Archetype archetype, ulong version)
        {
            archetype.ComponentMask.TryGetEncodedPosition<T>(out var typeIndex);
            archetype.TryGetChunkList(typeIndex, out _list);
            this.version = version;
            chunkIndex = 0;
            _current = default!;
        }

        /// <summary>
        /// 使用指定的块列表和版本号初始化枚举器实例。
        /// </summary>
        /// <param name="list">要枚举的块列表。</param>
        /// <param name="version">用于跳过旧版本块的版本号（0 表示不跳过）。</param>
        internal ChunkAccessorEnumerator(ArchetypeChunkList<T> list, ulong version)
        {
            _list = list;
            this.version = version;
            chunkIndex = 0;
            _current = default!;
        }

        /// <summary>
        /// 推进到下一个符合条件的块并设置 <see cref="Current"/>；无更多块时返回 false。
        /// </summary>
        /// <returns>若成功推进到下一块则返回 true，否则返回 false。</returns>
        public bool MoveNext()
        {
            if (_list == null)
                return false;

            while (true)
            {
                if (!_list.TryGetChunk(chunkIndex++, out _current))
                    return false;

                if (version != 0 && version > _current.Version)
                    continue;

                break;
            }
            return true;
        }
    }
}