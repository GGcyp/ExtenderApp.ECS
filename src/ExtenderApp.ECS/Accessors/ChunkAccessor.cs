using ExtenderApp.ECS.Archetypes;

namespace ExtenderApp.ECS.Accessors
{
    /// <summary>
    /// 在单个 Archetype 的指定组件列上按块索引遍历，提供获取单块的 `ComponentAccessor&lt;T1&gt;` 与块级枚举能力。
    /// </summary>
    /// <typeparam name="T">组件类型。</typeparam>
    internal struct ChunkAccessor<T>
    {
        private readonly ArchetypeChunkList<T> _chunks;
        private readonly ulong _version;

        /// <summary>
        /// 使用指定的块列表初始化访问器。 <paramref name="chunks" /> 表示某个 Archetype 在该列上的所有块（按链顺序或列表顺序）。
        /// </summary>
        /// <param name="chunks">表示目标原型所有块的列表，允许为 null（表示无块）。</param>
        /// <param name="version">版本号，用于检测访问器的有效性（可选）。</param>
        internal ChunkAccessor(ArchetypeChunkList<T> chunks, ulong version = 0)
        {
            _chunks = chunks;
            _version = version;
        }

        /// <summary>
        /// 返回列表中块的数量（块节点数）。若未提供列表则返回 0。
        /// </summary>
        public int Count => _chunks.Count;

        /// <summary>
        /// 按块序号（0 基）尝试获取对应块的 `ComponentAccessor{T1}`。
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
        /// </summary>
        /// <returns>返回一个 <see cref="Enumerator" /> 实例，用于块级遍历。</returns>
        public ChunkAccessorEnumerator<T> GetEnumerator() => new(_chunks, _version);
    }
}