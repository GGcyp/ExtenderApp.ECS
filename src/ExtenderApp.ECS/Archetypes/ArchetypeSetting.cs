namespace ExtenderApp.ECS.Archetypes
{
    /// <summary>
    /// 原型设置类，包含原型系统的全局配置项。 目前仅包含默认 Chunk 大小，但未来可能扩展为包含更多与原型相关的配置选项。 该类为静态类，所有成员均为常量或静态字段，以便在整个系统中统一访问和修改原型相关的设置。
    /// </summary>
    internal static class ArchetypeSetting
    {
        /// <summary>
        /// 默认 Chunk 大小（以字节为单位）。 该值可以根据实际需求调整，但应保持在合理范围内以平衡内存使用与性能。 过大可能导致内存浪费，过小可能增加分配频率。 1KB 是一个常见的起点，适用于许多场景。
        /// </summary>
        public const int DefaultChunkCapacity = 1 * 1024;
    }
}