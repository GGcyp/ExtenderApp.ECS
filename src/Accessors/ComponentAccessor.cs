using ExtenderApp.ECS.Archetypes;

namespace ExtenderApp.ECS.Accessors
{
    /// <summary>
    /// 在单个块（chunk）内访问组件数据，提供按局部索引读取、只读/可写引用包装及块内枚举功能。
    /// 此类型已公开以便 Job/系统层直接按块访问组件列。
    /// </summary>
    /// <typeparam name="T">组件类型。</typeparam>
    internal readonly struct ComponentAccessor<T>
    {
        private readonly ArchetypeChunk<T> _chunk;

        /// <summary>
        /// 当前块中元素数量。
        /// </summary>
        public int Count => _chunk.Count;

        /// <summary>
        /// 使用指定的块初始化 ComponentAccessor。
        /// </summary>
        internal ComponentAccessor(ArchetypeChunk<T> chunk)
        {
            _chunk = chunk;
        }

        /// <summary>
        /// 按局部索引返回组件副本。
        /// </summary>
        public T GetValue(int index) => _chunk.GetComponent(index);

        /// <summary>
        /// 按局部索引返回只读引用包装。
        /// </summary>
        public RefRO<T> GetRefRO(int index) => new(_chunk, index);

        /// <summary>
        /// 按局部索引返回可写引用包装。
        /// </summary>
        public RefRW<T> GetRefRW(int index) => new(_chunk, index);

        /// <summary>
        /// 获取块内可写引用枚举器。
        /// </summary>
        public ComponentEnumerator<T> GetEnumerator() => new(_chunk);
    }
}