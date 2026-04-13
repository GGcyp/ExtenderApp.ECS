using ExtenderApp.ECS.Archetypes;

namespace ExtenderApp.ECS.Accessors
{
    /// <summary>
    /// 只读引用包装：在遍历时按索引获取组件的只读引用（ref readonly）。
    /// 用于避免值类型拷贝并以安全方式暴露对组件的只读访问。
    /// </summary>
    /// <typeparam name="T">组件类型。</typeparam>
    public readonly struct RefRO<T>
    {
        /// <summary>
        /// 目标组件列对应的 ArchetypeChunk（可能为 null，表示无效包装）。
        /// </summary>
        private readonly ArchetypeChunk<T> _chunk;

        /// <summary>
        /// 组件在对应 chunk/列内的局部索引。
        /// </summary>
        private readonly int _index;

        /// <summary>
        /// 获取当前组件的只读引用（ref readonly）。
        /// 在访问之前请先检查 <see cref="IsValid"/>。
        /// </summary>
        public ref readonly T Value => ref _chunk.GetComponentRef(_index);

        /// <summary>
        /// 内部构造函数：由框架在构建 Reader/Writer 时创建。
        /// </summary>
        /// <param name="chunk">目标组件列的 ArchetypeChunk（不能为空）。</param>
        /// <param name="index">组件在 current 内的局部索引。</param>
        internal RefRO(ArchetypeChunk<T> chunk, int index)
        {
            _chunk = chunk;
            _index = index;
        }
    }
}