using ExtenderApp.ECS.Archetypes;

namespace ExtenderApp.ECS.Accessors
{
    /// <summary>
    /// 在单个实体段（ArchetypeEntitySegmentInfo）范围内按段提供实体访问能力，
    /// 可按局部索引读取实体并获取用于段内逐个遍历的枚举器。
    /// </summary>
    internal struct ComponentEntityAccessor
    {
        private readonly ArchetypeEntitySegmentInfo _segmentInfo;

        /// <summary>
        /// 当前段内的实体数量。
        /// </summary>
        public int Count => _segmentInfo.Count;

        /// <summary>
        /// 使用指定的实体段信息初始化访问器。
        /// </summary>
        public ComponentEntityAccessor(ArchetypeEntitySegmentInfo segmentInfo)
        {
            _segmentInfo = segmentInfo;
        }

        /// <summary>
        /// 按段内索引获取实体句柄（若索引越界行为由调用方负责）。
        /// </summary>
        public Entity GetValue(int index) => _segmentInfo.Entities[index];

        /// <summary>
        /// 获取用于遍历当前段内所有实体的结构体枚举器。
        /// </summary>
        public ComponentEntityEnumerator GetEnumerator() => new(_segmentInfo);
    }
}