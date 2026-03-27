using ExtenderApp.ECS.Archetypes;
using ExtenderApp.ECS.Queries;

namespace ExtenderApp.ECS.Accessors
{
    /// <summary>
    /// 用于遍历 Archetype 的实体段，返回段级实体枚举器。
    /// </summary>
    internal struct ChunkEntityAccessorEnumerator
    {
        private readonly ArchetypeEntitySegmentInfoList _list;
        private readonly int _count;
        private int _segmentIndex;

        /// <summary>
        /// 当前段的实体枚举器（在 <see cref="MoveNext"/> 返回 true 后有效）。
        /// </summary>
        public ComponentEntityAccessor Current { get; private set; }

        internal ChunkEntityAccessorEnumerator(ArchetypeSegment? segment) : this(segment?.Archetype ?? throw new ArgumentNullException(nameof(segment)))
        {
        }

        internal ChunkEntityAccessorEnumerator(Archetype archetype) : this(archetype.Entities)
        {
        }

        /// <summary>
        /// 使用指定的实体段列表初始化枚举器。
        /// </summary>
        /// <param name="segmentInfoList">要遍历的实体段列表；若为 null 则表示空集合。</param>
        internal ChunkEntityAccessorEnumerator(ArchetypeEntitySegmentInfoList segmentInfoList)
        {
            _list = segmentInfoList;
            _count = segmentInfoList.Count;
            _segmentIndex = 0;
        }

        /// <summary>
        /// 推进到下一个包含实体的段并设置 <see cref="Current"/>；无更多段时返回 false。
        /// </summary>
        /// <returns>若成功推进并找到下一个非空段返回 true，否则返回 false。</returns>
        public bool MoveNext()
        {
            while (true)
            {
                if (_segmentIndex >= _count)
                    return false;

                var segmentInfo = _list[_segmentIndex];
                if (segmentInfo.Count > 0)
                {
                    Current = new(segmentInfo);
                    return true;
                }
                _segmentIndex++;
            }
        }
    }
}