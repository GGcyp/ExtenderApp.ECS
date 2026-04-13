using ExtenderApp.ECS.Archetypes;

namespace ExtenderApp.ECS.Accessors
{
    /// <summary>
    /// 可写引用包装：在遍历时按索引获取组件的可写引用（ref）。
    /// 用于在不产生额外拷贝的情况下直接修改组件数据。
    /// </summary>
    /// <typeparam name="T">组件类型。</typeparam>
    public readonly struct RefRW<T>
    {
        /// <summary>
        /// 目标组件列对应的 ArchetypeChunk（引用类型）。
        /// </summary>
        private readonly ArchetypeChunk<T> _chunk;

        /// <summary>
        /// 组件在对应 chunk/列内的局部索引。
        /// </summary>
        private readonly int _index;

        /// <summary>
        /// 获取当前组件的可写引用（ref）。
        /// 注意：在持有该引用期间不要对底层 chunk 做结构性修改（如归还、释放或重新分配），
        /// 否则可能导致悬挂引用或未定义行为。
        /// </summary>
        public ref T Value => ref _chunk.GetComponentRef(_index);

        /// <summary>
        /// 内部构造函数：由框架在构建 Reader/Writer 时创建。
        /// </summary>
        /// <param name="chunk">目标组件列的 ArchetypeChunk（不能为空）。</param>
        /// <param name="index">组件在 current 内的局部索引。</param>
        internal RefRW(ArchetypeChunk<T> chunk, int index)
        {
            // 构造函数中不做过多运行时检查以减少开销，调用方应保证参数有效。
            _chunk = chunk;
            _index = index;
        }

        /// <summary>
        /// 将当前可写引用包装转换为只读引用包装。
        /// </summary>
        internal RefRO<T> AsReadOnly() => new(_chunk, _index);

        public static implicit operator RefRO<T>(RefRW<T> rw) => rw.AsReadOnly();

        public static implicit operator T(RefRW<T> rw) => rw.Value;
    }
}