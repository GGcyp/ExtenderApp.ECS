using ExtenderApp.ECS.Archetypes;

namespace ExtenderApp.ECS.Accessors
{
    /// <summary>
    /// 在单个实体段（ArchetypeEntitySegmentInfo）内按序遍历实体，用于在块级或段级操作中逐个访问实体句柄。
    /// </summary>
    internal struct ComponentEntityEnumerator
    {
        private readonly ArchetypeEntitySegmentInfo _segmentInfo;
        private int index;

        /// <summary>
        /// 当前枚举到的实体（在 <see cref="MoveNext"/> 返回 true 后有效）。
        /// </summary>
        public Entity Current { get; private set; }

        /// <summary>
        /// 使用指定的实体段信息初始化枚举器。
        /// </summary>
        /// <param name="segmentInfo">要遍历的实体段信息。</param>
        internal ComponentEntityEnumerator(ArchetypeEntitySegmentInfo segmentInfo)
        {
            _segmentInfo = segmentInfo;
            index = -1;
            Current = Entity.Empty;
        }

        /// <summary>
        /// 推进到段内的下一个实体并设置 <see cref="Current"/>；无更多实体时返回 false。
        /// </summary>
        /// <returns>若成功推进到下一个实体则返回 true，否则返回 false。</returns>
        public bool MoveNext()
        {
            int nextIndex = index + 1;
            if (nextIndex >= _segmentInfo.Count)
                return false;

            Current = _segmentInfo.Entities[nextIndex];
            index = nextIndex;
            return true;
        }
    }
}