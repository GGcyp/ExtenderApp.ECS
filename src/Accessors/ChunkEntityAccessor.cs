using ExtenderApp.ECS.Archetypes;

namespace ExtenderApp.ECS.Accessors
{
    /// <summary>
    /// 在单个 Archetype 范围内按段（chunk）访问实体集合，提供按全局索引读取与按段枚举能力。
    /// </summary>
    internal readonly struct ChunkEntityAccessor
    {
        private readonly ArchetypeEntitySegmentInfoList? segmentInfoList;

        /// <summary>
        /// 使用指定 Archetype 初始化实体访问器。
        /// </summary>
        internal ChunkEntityAccessor(Archetype? archetype) : this(archetype?.Entities ?? default)
        {
        }

        /// <summary>
        /// 使用实体段列表初始化访问器。
        /// </summary>
        internal ChunkEntityAccessor(ArchetypeEntitySegmentInfoList? segmentInfoList)
        {
            this.segmentInfoList = segmentInfoList;
        }

        /// <summary>
        /// 按全局索引获取实体（若不存在返回 <see cref="Entity.Empty"/>）。
        /// </summary>
        public Entity this[int index] => GetEntity(index);

        /// <summary>
        /// 按全局索引获取实体（若不存在返回 <see cref="Entity.Empty"/>）。
        /// </summary>
        public Entity GetEntity(int index)
        {
            return segmentInfoList?.TryFindLocalIndexForGlobalIndex(index, out int localIndex, out int chunkIndex) == true
                ? segmentInfoList[chunkIndex].Entities[localIndex]
                : Entity.Empty;
        }

        /// <summary>
        /// 尝试按全局索引获取实体；若存在则返回 true 并输出实体。
        /// </summary>
        public bool TryGetEntity(int index, out Entity entity)
        {
            entity = Entity.Empty;
            if (segmentInfoList?.TryFindLocalIndexForGlobalIndex(index, out int localIndex, out int chunkIndex) != true)
                return false;

            entity = segmentInfoList[chunkIndex].Entities[localIndex];
            return true;
        }

        /// <summary>
        /// 获取按段遍历的结构体枚举器，用于逐段枚举实体。
        /// </summary>
        public ChunkEntityAccessorEnumerator GetEnumerator() => new(segmentInfoList);
    }
}