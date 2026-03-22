using ExtenderApp.ECS.Archetypes;

namespace ExtenderApp.ECS.Accessors
{
    /// <summary>
    /// Archetype 实体段访问器。
    /// 用于按全局索引访问实体，或通过结构体枚举器遍历当前 Archetype 的所有实体。
    /// </summary>
    public struct ArchetypeChunkEntityAccessor
    {
        private readonly ArchetypeEntitySegmentInfoList segmentInfoList;

        /// <summary>
        /// 使用 Archetype 初始化实体访问器。
        /// </summary>
        /// <param name="archetype">目标 Archetype。</param>
        internal ArchetypeChunkEntityAccessor(Archetype archetype) : this(archetype.Entities)
        {
        }

        /// <summary>
        /// 使用实体段列表初始化访问器。
        /// </summary>
        /// <param name="segmentInfoList">实体段列表。</param>
        internal ArchetypeChunkEntityAccessor(ArchetypeEntitySegmentInfoList segmentInfoList)
        {
            this.segmentInfoList = segmentInfoList;
        }

        /// <summary>
        /// 按全局索引获取实体。
        /// </summary>
        /// <param name="index">实体全局索引。</param>
        /// <returns>对应位置的实体；不存在时返回 <see cref="Entity.Empty"/>。</returns>
        public readonly Entity this[int index] => GetEntity(index);

        /// <summary>
        /// 按全局索引获取实体。
        /// </summary>
        /// <param name="index">实体全局索引。</param>
        /// <returns>对应位置的实体；不存在时返回 <see cref="Entity.Empty"/>。</returns>
        public readonly Entity GetEntity(int index)
        {
            return segmentInfoList.TryFindLocalIndexForGlobalIndex(index, out int localIndex, out int chunkIndex)
                ? segmentInfoList[chunkIndex].Entities[localIndex]
                : Entity.Empty;
        }

        /// <summary>
        /// 尝试按全局索引获取实体。
        /// </summary>
        /// <param name="index">实体全局索引。</param>
        /// <param name="entity">输出实体。</param>
        /// <returns>获取成功返回 true；否则返回 false。</returns>
        public readonly bool TryGetEntity(int index, out Entity entity)
        {
            entity = Entity.Empty;
            if (!segmentInfoList.TryFindLocalIndexForGlobalIndex(index, out int localIndex, out int chunkIndex))
                return false;

            entity = segmentInfoList[chunkIndex].Entities[localIndex];
            return true;
        }

        /// <summary>
        /// 获取结构体枚举器。
        /// </summary>
        /// <returns>结构体枚举器。</returns>
        public readonly Enumerator GetEnumerator() => new(segmentInfoList);

        /// <summary>
        /// Archetype 实体枚举器。
        /// 按段顺序依次遍历所有实体。
        /// </summary>
        public struct Enumerator
        {
            private List<ArchetypeEntitySegmentInfo>.Enumerator enumerator;
            private SegmentInfoEnumerator segmentInfoEnumerator;
            private bool hasSegment;

            /// <summary>
            /// 当前实体（在 <see cref="MoveNext"/> 返回 true 后有效）。
            /// </summary>
            public Entity Current { get; private set; }

            /// <summary>
            /// 使用实体段列表初始化枚举器。
            /// </summary>
            /// <param name="segmentInfoList">实体段列表。</param>
            internal Enumerator(ArchetypeEntitySegmentInfoList segmentInfoList)
            {
                enumerator = segmentInfoList.GetEnumerator();
                segmentInfoEnumerator = default;
                hasSegment = false;
                Current = Entity.Empty;
            }

            /// <summary>
            /// 将枚举器推进到下一个实体。
            /// </summary>
            /// <returns>存在下一个实体返回 true；否则返回 false。</returns>
            public bool MoveNext()
            {
                while (true)
                {
                    if (hasSegment && segmentInfoEnumerator.MoveNext())
                    {
                        Current = segmentInfoEnumerator.Current;
                        return true;
                    }

                    if (!enumerator.MoveNext())
                    {
                        Current = Entity.Empty;
                        return false;
                    }

                    segmentInfoEnumerator = new(enumerator.Current);
                    hasSegment = true;
                }
            }
        }

        /// <summary>
        /// 单段实体枚举器。
        /// 用于遍历一个 <see cref="ArchetypeEntitySegmentInfo"/> 内的实体。
        /// </summary>
        public struct SegmentInfoEnumerator
        {
            private readonly ArchetypeEntitySegmentInfo _segmentInfo;
            private int index;

            /// <summary>
            /// 当前实体（在 <see cref="MoveNext"/> 返回 true 后有效）。
            /// </summary>
            public Entity Current { get; private set; }

            /// <summary>
            /// 使用实体段信息初始化枚举器。
            /// </summary>
            /// <param name="segmentInfo">实体段信息。</param>
            internal SegmentInfoEnumerator(ArchetypeEntitySegmentInfo segmentInfo)
            {
                _segmentInfo = segmentInfo;
                index = -1;
                Current = Entity.Empty;
            }

            /// <summary>
            /// 将枚举器推进到段内下一个实体。
            /// </summary>
            /// <returns>存在下一个实体返回 true；否则返回 false。</returns>
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
}