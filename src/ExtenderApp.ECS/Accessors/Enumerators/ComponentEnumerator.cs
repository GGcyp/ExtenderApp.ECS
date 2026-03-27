using ExtenderApp.ECS.Archetypes;

namespace ExtenderApp.ECS.Accessors
{
    /// <summary>
    /// 在单个块（chunk）内按局部索引枚举可写引用（RefRW&lt;T&gt;），用于在遍历中就地修改组件数据。
    /// </summary>
    /// <typeparam name="T">组件类型。</typeparam>
    internal struct ComponentEnumerator<T>
    {
        private readonly ArchetypeChunk<T> _currentChunk;
        private int localIndex;

        /// <summary>
        /// 获取当前项的可写引用（在 MoveNext 返回 true 后有效）。
        /// </summary>
        public readonly RefRW<T> Current => new RefRW<T>(_currentChunk, localIndex);

        /// <summary>
        /// 使用指定的块初始化枚举器。
        /// </summary>
        /// <param name="chunk">要遍历的块。</param>
        internal ComponentEnumerator(ArchetypeChunk<T> chunk)
        {
            _currentChunk = chunk;
            localIndex = -1;
        }

        /// <summary>
        /// 推进到下一个可写引用并设置 <see cref="Current"/>；无更多元素时返回 false。
        /// </summary>
        /// <returns>如果成功推进到下一个元素则返回 true，否则返回 false。</returns>
        public bool MoveNext()
        {
            var cur = _currentChunk;
            if (cur == null) return false;

            int idx = localIndex + 1;
            if (idx < cur.Count)
            {
                localIndex = idx;
                return true;
            }

            return false;
        }
    }
}