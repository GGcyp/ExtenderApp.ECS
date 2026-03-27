using ExtenderApp.ECS.Queries;

namespace ExtenderApp.ECS.Accessors
{
    /// <summary>
    /// 在给定的 Archetype 链表上按原型定位组件列，并为每个 Archetype 提供块级访问器（ChunkAccessor&lt;T&gt;）。
    /// 该类型用于在 Archetype 级别组织块遍历（适合调度器/Job 场景）。
    /// </summary>
    /// <typeparam name="T">组件类型</typeparam>
    internal readonly struct ArchetypeAccessor<T>
    {
        private readonly ArchetypeSegment? _nextSegment;
        private readonly ulong _version;

        /// <summary>
        /// 使用指定的 Archetype 链表头和版本号初始化访问器。
        /// </summary>
        internal ArchetypeAccessor(ArchetypeSegment? nextSegment, ulong version)
        {
            _nextSegment = nextSegment;
            _version = version;
        }

        /// <summary>
        /// 获取按 Archetype 定位并逐块返回 ComponentAccessor&lt;T&gt; 的枚举器。
        /// </summary>
        public ArchetypeAccessorEnumerator<T> GetEnumerator() => new(_nextSegment, _version);
    }
}