using ExtenderApp.ECS.Archetypes;

namespace ExtenderApp.ECS.Accessors
{
    /// <summary>
    /// 按块索引访问单列组件的访问器，基于外部提供的 <see cref="ArchetypeChunkList{T}"/> 表示某个 Archetype 的所有块。
    ///
    /// 语义：调用方可以按块序号（0 表示列表的第一个块）直接获取对应块的单块访问器 <see cref="ComponentAccessor{T}"/>
    /// 或按块级别枚举所有块进行批量处理。该类型不会遍历块内元素，仅负责块级定位与迭代。
    /// </summary>
    /// <typeparam name="T">组件类型（值类型且实现 <see cref="IComponent"/>）。</typeparam>
    internal struct ArchetypeChunkAccessor<T> where T : struct, IComponent
    {
        private readonly ArchetypeChunkList<T> _chunks;
        private readonly ulong _version;

        /// <summary>
        /// 使用指定的块列表初始化访问器。<paramref name="chunks"/> 表示某个 Archetype 在该列上的所有块（按链顺序或列表顺序）。
        /// </summary>
        /// <param name="chunks">表示目标原型所有块的列表，允许为 null（表示无块）。</param>
        /// <param name="version">版本号，用于检测访问器的有效性（可选）。</param>
        internal ArchetypeChunkAccessor(ArchetypeChunkList<T> chunks, ulong version = 0)
        {
            _chunks = chunks;
            _version = version;
        }

        /// <summary>
        /// 返回列表中块的数量（块节点数）。若未提供列表则返回 0。
        /// 注意：此数量表示块节点数，不等同于块内元素总数。
        /// </summary>
        public int Count => _chunks.Count;

        /// <summary>
        /// 按块序号（0 基）尝试获取对应块的 `ComponentAccessor{T}`。
        /// </summary>
        /// <param name="chunkIndex">目标块的序号（0 表示列表第一个块）。</param>
        /// <param name="accessor">当返回 true 时，输出的单块访问器，可用于在该块内按局部索引访问组件。</param>
        /// <returns>若指定序号存在且块类型匹配则返回 true，否则返回 false。</returns>
        public bool TryGetComponentAccessor(int chunkIndex, out ComponentAccessor<T> accessor)
        {
            accessor = default;
            if (_chunks.TryGetChunk(chunkIndex, out var chunk))
            {
                accessor = new ComponentAccessor<T>(chunk);
                return true;
            }
            return false;
        }

        /// <summary>
        /// 获取块级枚举器，用于按块顺序遍历所有存在的块。
        /// 枚举器为 struct，可避免堆分配，适合在热路径中循环处理每个块。
        /// </summary>
        /// <returns>返回一个 <see cref="Enumerator"/> 实例，用于块级遍历。</returns>
        public Enumerator GetEnumerator() => GetEnumerator(_version);

        public Enumerator GetEnumerator(ulong version) => new Enumerator(_chunks, version);

        /// <summary>
        /// 块级枚举器：每次 MoveNext 前进到下一个可用块（从索引 0 开始），
        /// Current 返回当前块对应的 <see cref="ComponentAccessor{T}"/>。
        /// </summary>
        public struct Enumerator
        {
            private readonly ArchetypeChunkList<T> _list;
            private ArchetypeChunk<T> _current;
            private int index;
            private ulong version;

            /// <summary>
            /// 获取当前块的单块访问器（在 MoveNext 返回 true 后有效）。
            /// </summary>
            public ComponentAccessor<T> Current => new(_current);

            /// <summary>
            /// 使用指定块列表初始化枚举器。
            /// </summary>
            /// <param name="list">要枚举的块列表（不能为空）。</param>
            /// <param name="version">版本号，用于检测枚举器的有效性（可选）。</param>
            internal Enumerator(ArchetypeChunkList<T> list, ulong version)
            {
                _list = list;
                this.version = version;
                index = 0;
                _current = default!;
            }

            /// <summary>
            /// 将枚举器推进到下一个块。若存在下一个块则返回 true，并使 <see cref="Current"/> 可用；否则返回 false。
            /// </summary>
            /// <returns>是否成功推进到下一块。</returns>
            public bool MoveNext()
            {
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
}