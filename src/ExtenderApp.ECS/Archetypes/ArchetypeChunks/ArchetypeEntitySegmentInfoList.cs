using System.Buffers;
using System.Runtime.InteropServices;
using ExtenderApp.ECS.Components;

namespace ExtenderApp.ECS.Archetypes
{
    /// <summary>
    /// 实体段信息列表，提供按 Span 的高效 ref 访问。
    /// </summary>
    internal class ArchetypeEntitySegmentInfoList : List<ArchetypeEntitySegmentInfo>
    {
        /// <summary>
        /// 初始化 <see cref="ArchetypeEntitySegmentInfoList" /> 的新实例。
        /// </summary>
        /// <param name="capacity">初始容量。</param>
        public ArchetypeEntitySegmentInfoList(int capacity) : base(capacity)
        {
        }

        /// <summary>
        /// 获取当前列表对应的连续 Span 视图。
        /// </summary>
        public Span<ArchetypeEntitySegmentInfo> Span => CollectionsMarshal.AsSpan(this);

        /// <summary>
        /// 将实体添加到可用段；若不存在可用段则创建新段。
        /// </summary>
        /// <param name="entity">要添加的实体。</param>
        /// <param name="handle">可选组件句柄。</param>
        /// <param name="globalIndex">输出实体全局索引。</param>
        public void AddToSegment(Entity entity, ComponentHandle? handle, out int globalIndex)
        {
            if (!TryGetFirstAvailableSegmentIndex(out var segmentIndex))
            {
                CreateSegment();
                segmentIndex = Count - 1;
            }

            ref var segment = ref Span[segmentIndex];
            segment.TryAdd(entity, handle, out var localIndex);
            globalIndex = segment.StartIndex + localIndex;
        }

        /// <summary>
        /// 批量将实体写入可用段；若容量不足则自动创建新段。
        /// </summary>
        /// <param name="entities">待写入实体集合。</param>
        /// <param name="globalIndexSpan">输出分配到的实体全局索引集合。</param>
        /// <param name="count">本次写入数量。</param>
        /// <param name="globalIndex">输出第一个写入实体的全局索引。</param>
        public void AddToSegmentRange(Span<Entity> entities, Span<int> globalIndexSpan, int count, out int globalIndex)
        {
            globalIndex = -1;
            while (count > 0)
            {
                if (!TryGetFirstAvailableSegmentIndex(out var segmentIndex))
                {
                    CreateSegment();
                    segmentIndex = Count - 1;
                }

                ref var segment = ref Span[segmentIndex];
                var oldCount = segment.Count;
                segment.TryAdds(entities, globalIndexSpan, out int addCount);
                if (addCount == 0)
                    continue;

                if (globalIndex < 0)
                    globalIndex = segment.StartIndex + oldCount;

                entities = entities.Slice(addCount);
                globalIndexSpan = globalIndexSpan.Slice(addCount);

                count -= addCount;
            }
        }

        /// <summary>
        /// 批量将实体写入可用段；若容量不足则自动创建新段。
        /// </summary>
        /// <param name="entities">待写入实体集合。</param>
        /// <param name="globalIndexSpan">输出分配到的实体全局索引集合。</param>
        /// <param name="handles">待写入组件句柄集合。</param>
        /// <param name="count">本次写入数量。</param>
        /// <param name="globalIndex">输出第一个写入实体的全局索引。</param>
        public void AddToSegmentRange(Span<Entity> entities, Span<int> globalIndexSpan, Span<ComponentHandle?> handles, int count, out int globalIndex)
        {
            globalIndex = -1;
            while (count > 0)
            {
                if (!TryGetFirstAvailableSegmentIndex(out var segmentIndex))
                {
                    CreateSegment();
                    segmentIndex = Count - 1;
                }

                ref var segment = ref Span[segmentIndex];
                var oldCount = segment.Count;
                segment.TryAdds(entities, globalIndexSpan, handles, out int addCount);
                if (addCount == 0)
                    continue;

                if (globalIndex < 0)
                    globalIndex = segment.StartIndex + oldCount;

                entities = entities.Slice(addCount);
                handles = handles.Slice(addCount);
                globalIndexSpan = globalIndexSpan.Slice(addCount);

                count -= addCount;
            }
        }

        /// <summary>
        /// 根据全局索引查找段内局部索引与段索引。
        /// </summary>
        /// <param name="globalIndex">全局索引。</param>
        /// <param name="localIndex">输出局部索引。</param>
        /// <param name="chunkIndex">输出段索引。</param>
        /// <returns>找到返回 true；否则返回 false。</returns>
        public bool TryFindLocalIndexForGlobalIndex(int globalIndex, out int localIndex, out int chunkIndex)
        {
            localIndex = -1;
            chunkIndex = -1;
            if (Count == 0)
                return false;

            var span = Span;
            int lo = 0, hi = Count - 1, best = -1;
            while (lo <= hi)
            {
                int mid = (lo + hi) >> 1;
                int start = span[mid].StartIndex;
                if (start <= globalIndex)
                {
                    best = mid;
                    lo = mid + 1;
                }
                else
                {
                    hi = mid - 1;
                }
            }

            if (best == -1)
                return false;

            for (int i = best; i < Count && span[i].StartIndex <= globalIndex; i++)
            {
                var candidate = span[i];
                if (candidate.TryWithinSegment(globalIndex, out localIndex))
                {
                    chunkIndex = i;
                    return true;
                }
            }

            for (int i = best - 1; i >= 0; i--)
            {
                var candidate = span[i];
                if (candidate.TryWithinSegment(globalIndex, out localIndex))
                {
                    chunkIndex = i;
                    return true;
                }
                if (candidate.StartIndex < globalIndex - candidate.Count)
                    break;
            }

            return false;
        }

        /// <summary>
        /// 从实体段中按全局索引移除实体。
        /// </summary>
        /// <param name="globalIndex">实体全局索引。</param>
        /// <param name="localIndex">输出段内局部索引。</param>
        /// <param name="chunkIndex">输出实体段索引。</param>
        /// <param name="removedHandle">输出被移除实体对应句柄。</param>
        /// <param name="changedEntity">输出被移动到目标位置的实体。</param>
        /// <param name="changedHandle">输出被移动到目标位置的句柄。</param>
        /// <returns>移除成功返回 true；否则返回 false。</returns>
        public bool TryRemoveFromSegment(int globalIndex, out int localIndex, out int chunkIndex, out ComponentHandle? removedHandle, out Entity changedEntity, out ComponentHandle? changedHandle)
        {
            changedEntity = Entity.Empty;
            removedHandle = default!;
            changedHandle = default!;
            localIndex = -1;
            chunkIndex = -1;
            var span = Span;
            if (TryFindLocalIndexForGlobalIndex(globalIndex, out localIndex, out chunkIndex))
            {
                ref var segment = ref span[chunkIndex];
                segment.Remove(localIndex, out removedHandle, out changedEntity, out changedHandle);
                RemoveEmptySegments();
                return true;
            }
            return false;
        }

        /// <summary>
        /// 批量从实体段中移除实体。
        /// </summary>
        /// <param name="globalIndexs">全局索引集合。</param>
        /// <param name="localIndexSpan">输出段内局部索引集合。</param>
        /// <param name="chunkIndexSpan">输出段索引集合。</param>
        /// <param name="removedHandles">输出被移除实体句柄集合。</param>
        /// <param name="changedEntities">输出被移动实体集合。</param>
        /// <param name="changedHandles">输出被移动句柄集合。</param>
        /// <returns>全部移除成功返回 true；否则返回 false。</returns>
        public bool TryRemoveFromSegmentRange(Span<int> globalIndexs, Span<int> localIndexSpan, Span<int> chunkIndexSpan, Span<ComponentHandle?> removedHandles, Span<Entity> changedEntities, Span<ComponentHandle?> changedHandles)
        {
            var span = Span;
            int i = 0;
            foreach (var globalIndex in globalIndexs)
            {
                if (TryFindLocalIndexForGlobalIndex(globalIndex, out int localIndex, out int chunkIndex))
                {
                    ref var segment = ref span[chunkIndex];
                    localIndexSpan[i] = localIndex;
                    chunkIndexSpan[i] = chunkIndex;

                    segment.Remove(localIndex, out var removedHandle, out var changedEntity, out var changedHandle);
                    removedHandles[i] = removedHandle;
                    changedEntities[i] = changedEntity;
                    changedHandles[i] = changedHandle;
                    i++;
                    continue;
                }
                else
                    return false;
            }
            RemoveEmptySegments();
            return true;
        }

        /// <summary>
        /// 批量从实体段中移除实体。
        /// </summary>
        /// <param name="globalIndexs">全局索引集合。</param>
        /// <param name="localIndexSpan">输出段内局部索引集合。</param>
        /// <param name="chunkIndexSpan">输出段索引集合。</param>
        /// <param name="changedEntities">输出被移动实体集合。</param>
        /// <param name="changedHandles">输出被移动句柄集合。</param>
        /// <returns>全部移除成功返回 true；否则返回 false。</returns>
        public bool TryRemoveFromSegmentRange(Span<int> globalIndexs, Span<int> localIndexSpan, Span<int> chunkIndexSpan, Span<Entity> changedEntities, Span<ComponentHandle?> changedHandles)
        {
            var span = Span;
            int i = 0;
            foreach (var globalIndex in globalIndexs)
            {
                if (TryFindLocalIndexForGlobalIndex(globalIndex, out int localIndex, out int chunkIndex))
                {
                    ref var segment = ref span[chunkIndex];
                    localIndexSpan[i] = localIndex;
                    chunkIndexSpan[i] = chunkIndex;

                    segment.Remove(localIndex, out var removedHandle, out var changedEntity, out var changedHandle);
                    removedHandle?.Return();
                    changedEntities[i] = changedEntity;
                    changedHandles[i] = changedHandle;
                    i++;
                    continue;
                }
                else
                    return false;
            }
            RemoveEmptySegments();
            return true;
        }

        /// <summary>
        /// 创建并追加一个新实体段。 段容量会先按预设数组增长，超过预设后固定为 2048。
        /// </summary>
        private void CreateSegment()
        {
            var capacity = ArchetypeChunkManager.GetNextSize(Count);
            var startIndex = Count == 0 ? 0 : Span[Count - 1].StartIndex + ArchetypeChunkManager.GetPreviousSize(Count - 1);
            var array = ArrayPool<Entity>.Shared.Rent(capacity);
            var componentHandles = ArrayPool<ComponentHandle?>.Shared.Rent(capacity);
            Add(new(array, componentHandles, startIndex));
        }

        /// <summary>
        /// 清理末尾空段并归还实体数组到对象池。
        /// </summary>
        private void RemoveEmptySegments()
        {
            var span = Span;
            int count = Count;
            for (int i = count - 1; i >= 0; i--)
            {
                ref var segment = ref span[i];
                if (segment.Count > 0)
                    break;

                foreach (var handle in segment.ComponentHandles)
                    handle?.Return();

                ArrayPool<Entity>.Shared.Return(segment.Entities);
                ArrayPool<ComponentHandle?>.Shared.Return(segment.ComponentHandles);

                RemoveAt(i);
            }
        }

        /// <summary>
        /// 获取第一个仍有可用空间的段索引。
        /// </summary>
        /// <param name="segmentIndex">输出段索引。</param>
        /// <returns>找到返回 true；否则返回 false。</returns>
        private bool TryGetFirstAvailableSegmentIndex(out int segmentIndex)
        {
            segmentIndex = -1;
            if (Count == 0)
                return false;

            foreach (var segment in Span)
            {
                segmentIndex++;
                if (segment.HasFree)
                    return true;
            }
            return false;
        }
    }
}