using ExtenderApp.ECS.Archetypes;

namespace ExtenderApp.ECS.Accessors
{
    /// <summary>
    /// Archetype 实体分段访问器。
    ///
    /// 该结构体用于在给定的 <see cref="Archetype"/> 范围内按段（Chunk）访问实体，可通过全局索引直接读取某个实体，
    /// 也可以获取结构体枚举器按段顺序遍历该 Archetype 的所有实体。
    ///
    /// 设计说明：此访问器仅针对单个 Archetype 的实体集合进行访问。对外暴露轻量的值类型迭代器，避免堆分配，
    /// 适用于在查询或系统中高频遍历实体的场景。
    /// </summary>
    public struct ArchetypeChunkEntityAccessor
    {
        /// <summary>
        /// 表示 Archetype 的实体段信息列表（按段存储多个实体数组及其长度等元数据）。
        /// 若为 null 则表示目标 Archetype 不存在或当前没有实体段。
        /// </summary>
        private readonly ArchetypeEntitySegmentInfoList? segmentInfoList;

        /// <summary>
        /// 使用指定 Archetype 初始化实体访问器。
        /// 若传入 null，则访问器表示空集合。
        /// </summary>
        /// <param name="archetype">目标 Archetype。</param>
        internal ArchetypeChunkEntityAccessor(Archetype? archetype) : this(archetype?.Entities ?? default)
        {
        }

        /// <summary>
        /// 使用实体段列表初始化访问器。
        /// </summary>
        /// <param name="segmentInfoList">实体段列表（可能为 null）。</param>
        internal ArchetypeChunkEntityAccessor(ArchetypeEntitySegmentInfoList? segmentInfoList)
        {
            this.segmentInfoList = segmentInfoList;
        }

        /// <summary>
        /// 按全局索引获取实体的索引器。若索引不存在返回 <see cref="Entity.Empty"/>。
        /// </summary>
        /// <param name="index">实体全局索引。</param>
        /// <returns>对应位置的实体；不存在时返回 <see cref="Entity.Empty"/>。</returns>
        public readonly Entity this[int index] => GetEntity(index);

        /// <summary>
        /// 按全局索引获取实体。若索引无效返回 <see cref="Entity.Empty"/>。
        /// </summary>
        /// <param name="index">实体全局索引。</param>
        /// <returns>对应位置的实体；不存在时返回 <see cref="Entity.Empty"/>。</returns>
        public readonly Entity GetEntity(int index)
        {
            return segmentInfoList?.TryFindLocalIndexForGlobalIndex(index, out int localIndex, out int chunkIndex) == true
                ? segmentInfoList[chunkIndex].Entities[localIndex]
                : Entity.Empty;
        }

        /// <summary>
        /// 尝试按全局索引获取实体。
        /// </summary>
        /// <param name="index">实体全局索引。</param>
        /// <param name="entity">输出参数，索引存在时为对应实体，否则为 <see cref="Entity.Empty"/>。</param>
        /// <returns>获取成功返回 true；否则返回 false。</returns>
        public readonly bool TryGetEntity(int index, out Entity entity)
        {
            entity = Entity.Empty;
            if (segmentInfoList?.TryFindLocalIndexForGlobalIndex(index, out int localIndex, out int chunkIndex) != true)
                return false;

            entity = segmentInfoList[chunkIndex].Entities[localIndex];
            return true;
        }

        /// <summary>
        /// 获取按段遍历的结构体枚举器。
        /// </summary>
        /// <returns>一个可在调用处进行 foreach 遍历的结构体枚举器。</returns>
        public readonly Enumerator GetEnumerator() => new(segmentInfoList);

        /// <summary>
        /// Archetype 实体枚举器。
        /// 按段顺序依次遍历所有实体，支持无配额分配且为值类型以避免堆分配。
        /// </summary>
        public struct Enumerator
        {
            private List<ArchetypeEntitySegmentInfo>.Enumerator enumerator;
            private SegmentInfoEnumerator segmentInfoEnumerator;
            private bool hasInit;

            /// <summary>
            /// 当前实体（在 <see cref="MoveNext"/> 返回 true 后有效）。
            /// </summary>
            public Entity Current { get; private set; }

            /// <summary>
            /// 使用实体段列表初始化枚举器。
            /// </summary>
            /// <param name="segmentInfoList">实体段列表。</param>
            internal Enumerator(ArchetypeEntitySegmentInfoList? segmentInfoList)
            {
                enumerator = segmentInfoList?.GetEnumerator() ?? default;
                segmentInfoEnumerator = default;
                Current = Entity.Empty;
                hasInit = segmentInfoList != null;
            }

            /// <summary>
            /// 将枚举器推进到下一个实体。
            /// </summary>
            /// <returns>存在下一个实体返回 true；否则返回 false。</returns>
            public bool MoveNext()
            {
                if (!hasInit)
                    return false;

                while (true)
                {
                    if (segmentInfoEnumerator.MoveNext())
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