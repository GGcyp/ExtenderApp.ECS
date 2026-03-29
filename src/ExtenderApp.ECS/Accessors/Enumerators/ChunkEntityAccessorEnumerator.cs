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
        /// 当前段的实体枚举器（在 <see cref="MoveNext" /> 返回 true 后有效）。
        /// </summary>
        public ComponentEntityAccessor Current { get; private set; }

        /// <summary>
        /// 使用指定的 ArchetypeSegment 初始化枚举器，枚举该 Archetype 中的所有实体段。
        /// </summary>
        /// <param name="segment">要枚举的 ArchetypeSegment。</param>
        internal ChunkEntityAccessorEnumerator(ArchetypeSegment? segment) : this(segment?.Archetype ?? throw new ArgumentNullException(nameof(segment)))
        {
        }

        /// <summary>
        /// 使用指定的 ArchetypeSegment 初始化枚举器，枚举该 Archetype 中的所有实体段。
        /// </summary>
        /// <param name="segment">要枚举的 ArchetypeSegment。</param>
        internal ChunkEntityAccessorEnumerator(ArchetypeSegment? segment, int count) : this(segment?.Archetype ?? throw new ArgumentNullException(nameof(segment)), count)
        {
        }

        /// <summary>
        /// 使用指定的 Archetype 初始化枚举器，枚举该 Archetype 中的所有实体段。
        /// </summary>
        /// <param name="archetype">要枚举的 Archetype。</param>
        internal ChunkEntityAccessorEnumerator(Archetype archetype) : this(archetype.Entities)
        {
        }

        /// <summary>
        /// 使用指定的 Archetype 初始化枚举器，枚举该 Archetype 中的所有实体段。
        /// </summary>
        /// <param name="archetype">要枚举的 Archetype。</param>
        /// <param name="count">要枚举的段数量（默认为 0，表示枚举整个列表）。</param>
        internal ChunkEntityAccessorEnumerator(Archetype archetype, int count) : this(archetype.Entities, count)
        {
        }

        /// <summary>
        /// 使用指定的实体段列表初始化枚举器。
        /// </summary>
        /// <param name="segmentInfoList">要遍历的实体段列表；若为 null 则表示空集合。</param>
        /// <param name="count">要枚举的段数量（默认为 0，表示枚举整个列表）。</param>
        internal ChunkEntityAccessorEnumerator(ArchetypeEntitySegmentInfoList segmentInfoList) : this(segmentInfoList, 0)
        {
        }

        /// <summary>
        /// 使用指定的实体段列表初始化枚举器。
        /// </summary>
        /// <param name="segmentInfoList">要遍历的实体段列表；若为 null 则表示空集合。</param>
        /// <param name="count">要枚举的段数量（默认为 0，表示枚举整个列表）。</param>
        internal ChunkEntityAccessorEnumerator(ArchetypeEntitySegmentInfoList segmentInfoList, int count)
        {
            _list = segmentInfoList;
            _count = count <= 0 ? segmentInfoList.Count : count;
            _segmentIndex = 0;
        }

        /// <summary>
        /// 从指定实体段下标起，在 <paramref name="segmentSpan" /> 个段范围内枚举（与组件列块批对齐）。
        /// </summary>
        internal ChunkEntityAccessorEnumerator(ArchetypeEntitySegmentInfoList segmentInfoList, int startSegmentIndex, int segmentSpan)
        {
            _list = segmentInfoList;
            _segmentIndex = startSegmentIndex;
            int end = segmentSpan <= 0 ? segmentInfoList.Count : startSegmentIndex + segmentSpan;
            _count = end < segmentInfoList.Count ? end : segmentInfoList.Count;
        }

        /// <summary>
        /// 推进到下一个包含实体的段并设置 <see cref="Current" />；无更多段时返回 false。
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
                    _segmentIndex++;
                    return true;
                }

                _segmentIndex++;
            }
        }
    }
}