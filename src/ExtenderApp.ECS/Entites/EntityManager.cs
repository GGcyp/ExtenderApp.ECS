using System.Buffers;
using System.Collections;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;

namespace ExtenderApp.ECS.Entities
{
    /// <summary>
    /// 实体管理器：负责实体信息的分配、回收与生命周期管理。
    ///
    /// 主要职责：
    /// - 管理若干连续的实体段（EntitySegment），为实体分配全局 Id 与版本；
    /// - 提供单个与批量的实体创建与销毁接口；
    /// - 协助维护实体与 Archetype 之间的关联并处理索引变更；
    /// - 尽量复用已回收的段以减少频繁分配带来的开销。
    /// </summary>
    internal class EntityManager
    {
        /// <summary>
        /// 默认的段容量。
        /// </summary>
        private const int DefaultSegmentSize = 4 * 1024;

        /// <summary>
        /// 默认的有序段集合初始容量。根据实际使用情况可以调整以平衡内存占用与性能。
        /// </summary>
        private const int DefaultSegmentListSize = 128;

        /// <summary>
        /// 单词最大批量创建/销毁数量。过大可能导致单次开销较高，过小则增加调用次数。
        /// </summary>
        private const int MaxBatchSize = 20000;

        /// <summary>
        /// 使用有序字典（SortedList）存放段，键为段的 StartIndex。可通过二分查找快速定位包含某全局 Id 的段。
        /// </summary>
        private readonly SortedList<int, EntitySegment> _segments;

        /// <summary>
        /// 缓存的空闲段实例（当段被完全回收时会缓存以便重用），用于减少频繁创建新段的开销。
        /// </summary>
        private EntitySegment? _cachedSegment;

        /// <summary>
        /// 初始化新的 EntityManager 实例并添加首段。
        /// </summary>
        public EntityManager()
        {
            // 初始化首段
            var first = new EntitySegment(1, DefaultSegmentSize);
            _segments = new(DefaultSegmentListSize);
            _segments.Add(first.StartIndex, first);
        }

        #region Create

        /// <summary>
        /// 创建一个新实体（快捷方法）。等同于调用 <see cref="CreateEntity(Archetype?, out int)"/> 并忽略输出的 archetype 索引。
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Entity CreateEntity() => CreateEntity(null, out _);

        /// <summary>
        /// 创建一个新实体并可指定关联的 Archetype（快捷方法）。等同于调用 <see cref="CreateEntity(Archetype?, out int)"/> 并忽略输出的 archetype 索引。
        /// </summary>
        /// <param name="archetype">可选的 Archetype，若为 null 则不关联。</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Entity CreateEntity(Archetype? archetype) => CreateEntity(archetype, out _);

        /// <summary>
        /// 创建新实体信息并返回。 若当前段已满会在现有段中查找空位或在末尾创建新段。
        /// </summary>
        /// <param name="archetype">实体所属的 Archetype（可为空）。</param>
        /// <param name="archetypeIndex">输出实体在 Archetype 中的索引（若关联则返回有效索引）。</param>
        /// <returns>新分配的实体句柄（包含 Id 与 Version）。</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Entity CreateEntity(Archetype? archetype, out int archetypeIndex)
        {
            // 遍历所有段，从头开始寻找可用槽位（因为 Id 可回收，首段可能有空位）
            foreach (var seg in _segments.Values)
            {
                if (seg.TryRentEntityInfoIndex(out var localIndex))
                {
                    ref var info = ref seg.GetEntityInfo(localIndex);
                    InitializeEntityInfo(ref info, seg.StartIndex + localIndex, archetype, out archetypeIndex);
                    return info;
                }
            }

            // 若所有段都满，则在末尾追加新段并分配
            EntitySegment last = _segments.Values[_segments.Count - 1];
            EntitySegment newSeg = _cachedSegment ?? new(0, DefaultSegmentSize);
            _cachedSegment = null;
            newSeg.StartIndex = last.StartIndex + DefaultSegmentSize;
            _segments.Add(newSeg.StartIndex, newSeg);

            newSeg.TryRentEntityInfoIndex(out var newLocalIndex);
            ref var info2 = ref newSeg.GetEntityInfo(newLocalIndex);
            InitializeEntityInfo(ref info2, newSeg.StartIndex + newLocalIndex, archetype, out archetypeIndex);
            return info2;

            void InitializeEntityInfo(ref EntityInfo info, int globalId, Archetype? archetype, out int archetypeIndex)
            {
                archetypeIndex = 0;
                if (info.Id == 0)
                    info.Id = globalId;

                if (archetype != null)
                {
                    archetypeIndex = archetype.AddEntity(info);
                    info.Archetype = archetype;
                    info.ArchetypeIndex = archetypeIndex;
                }
            }
        }

        /// <summary>
        /// 将新创建的实体批量写入到指定 <see cref="Span{Entity}"/>（不关联 Archetype）。
        /// </summary>
        /// <param name="span">用于接收实体结果的目标 Span。</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void CreateEntity(Span<Entity> span) => CreateEntity(span, default(Archetype));

        /// <summary>
        /// 将新创建的实体批量写入到指定 <see cref="Span{Entity}"/> 并将其关联到指定 Archetype（可为空）。
        /// </summary>
        /// <param name="span">用于接收实体结果的目标 Span。</param>
        /// <param name="archetype">目标 Archetype（可为空）。</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void CreateEntity(Span<Entity> span, Archetype? archetype)
        {
            int offset = 0;
            while (offset < span.Length)
            {
                int batchSize = Math.Min(MaxBatchSize, span.Length - offset);
                CreateEntityPrivate(span.Slice(offset, batchSize), archetype);
                offset += batchSize;
            }
        }

        /// <summary>
        /// 批量创建实体的私有实现：尝试从现有段分配槽位，必要时创建新段以满足需求。输入已按批次分割以避免过大批次。
        /// </summary>
        /// <param name="span">目标接收实体的 Span。</param>
        /// <param name="archetype">目标 Archetype（可为空）。</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void CreateEntityPrivate(Span<Entity> span, Archetype? archetype)
        {
            int[] indexArray = ArrayPool<int>.Shared.Rent(span.Length);
            try
            {
                Span<int> localIndexBuffer = indexArray.AsSpan(0, span.Length);
                int written = 0;

                foreach (var seg in _segments.Values)
                {
                    if (written >= span.Length)
                        break;

                    Span<int> requestSpan = localIndexBuffer.Slice(0, span.Length - written);
                    seg.TryRentEntityInfoIndexs(requestSpan, out int count);
                    if (count <= 0)
                        continue;

                    WriteEntities(seg, requestSpan.Slice(0, count), span.Slice(written, count), archetype);
                    written += count;
                }

                while (written < span.Length)
                {
                    EntitySegment last = _segments.Values[_segments.Count - 1];
                    EntitySegment newSeg = _cachedSegment ?? new(0, DefaultSegmentSize);
                    _cachedSegment = null;
                    newSeg.StartIndex = last.StartIndex + DefaultSegmentSize;
                    _segments.Add(newSeg.StartIndex, newSeg);

                    Span<int> requestSpan = localIndexBuffer.Slice(0, span.Length - written);
                    newSeg.TryRentEntityInfoIndexs(requestSpan, out int count);
                    if (count <= 0)
                        throw new InvalidOperationException("无法从新建实体段分配实体索引。");

                    WriteEntities(newSeg, requestSpan.Slice(0, count), span.Slice(written, count), archetype);
                    written += count;
                }
            }
            finally
            {
                ArrayPool<int>.Shared.Return(indexArray);
            }

            void WriteEntities(EntitySegment segment, Span<int> localIndices, Span<Entity> target, Archetype? targetArchetype)
            {
                for (int i = 0; i < localIndices.Length; i++)
                {
                    int localIndex = localIndices[i];
                    ref var info = ref segment.GetEntityInfo(localIndex);

                    if (info.Id == 0)
                        info.Id = segment.StartIndex + localIndex;

                    if (targetArchetype != null)
                    {
                        int archetypeIndex = targetArchetype.AddEntity(info);
                        info.Archetype = targetArchetype;
                        info.ArchetypeIndex = archetypeIndex;
                    }

                    target[i] = info;
                }
            }
        }

        /// <summary>
        /// 将新创建的实体批量写入到指定 <see cref="Span{Entity}"/>，并关联用户数据。
        /// </summary>
        /// <param name="span">要被写入的实体跨度。</param>
        /// <param name="usedatas">与实体关联的用户数据跨度。</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void CreateEntity(Span<Entity> span, Span<object?> usedatas) => CreateEntity(span, usedatas, null);

        /// <summary>
        /// 将新创建的实体批量写入到指定 <see cref="Span{Entity}"/>，并关联用户数据与可选 Archetype。
        /// </summary>
        /// <param name="span">要被写入的实体跨度。</param>
        /// <param name="usedatas">与实体关联的用户数据跨度。</param>
        /// <param name="archetype">目标 Archetype（可为空）。</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void CreateEntity(Span<Entity> span, Span<object?> usedatas, Archetype? archetype)
        {
            int offset = 0;
            while (offset < span.Length)
            {
                int batchSize = Math.Min(MaxBatchSize, span.Length - offset);
                CreateEntityPrivate(span.Slice(offset, batchSize), usedatas.Slice(offset, batchSize), archetype);
                offset += batchSize;
            }
        }

        /// <summary>
        /// 批量创建并关联用户数据的私有实现：尝试从现有段分配槽位，必要时创建新段以满足需求。
        /// </summary>
        /// <param name="span">要被写入的实体跨度。</param>
        /// <param name="useDatas">与实体关联的用户数据跨度。</param>
        /// <param name="archetype">目标 Archetype（可为空）。</param>
        /// <exception cref="InvalidOperationException">在无法从新建段分配索引时抛出。</exception>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void CreateEntityPrivate(Span<Entity> span, Span<object?> useDatas, Archetype? archetype)
        {
            int[] indexArray = ArrayPool<int>.Shared.Rent(span.Length);
            try
            {
                Span<int> localIndexBuffer = indexArray.AsSpan(0, span.Length);
                int written = 0;

                foreach (var seg in _segments.Values)
                {
                    if (written >= span.Length)
                        break;

                    Span<int> requestSpan = localIndexBuffer.Slice(0, span.Length - written);
                    seg.TryRentEntityInfoIndexs(requestSpan, out int count);
                    if (count <= 0)
                        continue;

                    WriteEntities(seg, requestSpan.Slice(0, count), span.Slice(written, count), archetype);
                    written += count;
                }

                while (written < span.Length)
                {
                    EntitySegment last = _segments.Values[_segments.Count - 1];
                    EntitySegment newSeg = _cachedSegment ?? new(0, DefaultSegmentSize);
                    _cachedSegment = null;
                    newSeg.StartIndex = last.StartIndex + DefaultSegmentSize;
                    _segments.Add(newSeg.StartIndex, newSeg);

                    Span<int> requestSpan = localIndexBuffer.Slice(0, span.Length - written);
                    newSeg.TryRentEntityInfoIndexs(requestSpan, out int count);
                    if (count <= 0)
                        throw new InvalidOperationException("无法从新建实体段分配实体索引。");

                    WriteEntities(newSeg, requestSpan.Slice(0, count), span.Slice(written, count), archetype);
                    written += count;
                }
            }
            finally
            {
                ArrayPool<int>.Shared.Return(indexArray);
            }

            void WriteEntities(EntitySegment segment, Span<int> localIndices, Span<Entity> target, Archetype? targetArchetype)
            {
                for (int i = 0; i < localIndices.Length; i++)
                {
                    int localIndex = localIndices[i];
                    ref var info = ref segment.GetEntityInfo(localIndex);

                    if (info.Id == 0)
                        info.Id = segment.StartIndex + localIndex;

                    if (targetArchetype != null)
                    {
                        int archetypeIndex = targetArchetype.AddEntity(info);
                        info.Archetype = targetArchetype;
                        info.ArchetypeIndex = archetypeIndex;
                    }

                    target[i] = info;
                }
            }
        }

        #endregion Create

        #region Destroy

        /// <summary>
        /// 销毁指定实体信息。 若实体存在且版本匹配则释放其槽位并在必要时更新其 Archetype 中的索引。
        /// </summary>
        /// <param name="entity">要销毁的实体信息。</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void DestroyEntity(Entity entity)
        {
            if (entity.IsEmpty)
                return;

            if (!TryFindSegmentForEntity(entity, out var segment, out var localIndex))
                return;

            segment.ReturnEntity(localIndex, entity.Version, out var changedEntity, out var archetypeIndex);

            if (!changedEntity.IsEmpty)
            {
                TryChangedArchetypeIndex(changedEntity, archetypeIndex);
            }

            RemoveEmptySegment();
        }

        /// <summary>
        /// 批量销毁实体（分批处理以避免过大单次开销）。
        /// </summary>
        /// <param name="entities">待销毁实体集合。</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void DestroyEntity(ReadOnlySpan<Entity> entities)
        {
            int offset = 0;
            while (offset < entities.Length)
            {
                int batchSize = Math.Min(MaxBatchSize, entities.Length - offset);
                DestroyEntityPrivate(entities.Slice(offset, batchSize));
                offset += batchSize;
            }
        }

        /// <summary>
        /// 批量销毁实体的私有实现：遍历输入集合，查找对应段并回收槽位，最后尝试移除完全空闲的段。
        /// </summary>
        /// <param name="entities">待销毁实体集合。</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void DestroyEntityPrivate(ReadOnlySpan<Entity> entities)
        {
            foreach (var entity in entities)
            {
                if (entity.IsEmpty)
                    continue;
                if (!TryFindSegmentForEntity(entity, out var segment, out var localIndex))
                    continue;

                segment.ReturnEntity(localIndex, entity.Version, out var changedEntity, out var archetypeIndex);

                if (!changedEntity.IsEmpty)
                {
                    TryChangedArchetypeIndex(changedEntity, archetypeIndex);
                }
            }
            RemoveEmptySegment();
        }

        /// <summary>
        /// 删除当前所有实体均已销毁的段（非首段），并将其缓存以供未来重用。
        /// 仅当段完全空闲时才会被移除并缓存，部分空闲的段会保留以供后续分配使用。
        /// </summary>
        private void RemoveEmptySegment()
        {
            for (var i = _segments.Count - 1; i >= 1; i--)
            {
                if (_segments.Values[i].AliveCount != 0)
                    continue;

                var segment = _segments.Values[i];
                _segments.Remove(segment.StartIndex);
                _cachedSegment = segment;
            }
        }

        #endregion Destroy

        #region Changed

        /// <summary>
        /// 将指定实体在其所属 Archetype 中的索引更新为新的索引（仅在实体仍存活且版本匹配时生效）。
        /// </summary>
        /// <param name="entity">要变更的实体句柄。</param>
        /// <param name="archetypeIndex">新索引值。</param>
        public bool TryChangedArchetypeIndex(Entity entity, int archetypeIndex)
        {
            if (entity.IsEmpty)
                return false;

            if (TryFindSegmentForEntity(entity, out var entitySegment, out var localIndex))
            {
                ref var info = ref entitySegment.GetEntityInfo(localIndex);
                if (!info.IsAlive(entity))
                    return false;

                info.ArchetypeIndex = archetypeIndex;
                return true;
            }
            return false;
        }

        /// <summary>
        /// 将指定实体切换到新的 Archetype 并更新其在目标 Archetype 中的索引（仅在实体仍存活且版本匹配时生效）。
        /// </summary>
        /// <param name="entity">要变更的实体句柄。</param>
        /// <param name="archetype">目标 Archetype（可为空）。</param>
        /// <param name="archetypeIndex">目标 Archetype 中的索引值。</param>
        public bool TryChangedArchetype(Entity entity, Archetype? archetype, int archetypeIndex)
        {
            if (entity.IsEmpty)
                return false;

            if (TryFindSegmentForEntity(entity, out var entitySegment, out var localIndex))
            {
                ref var info = ref entitySegment.GetEntityInfo(localIndex);
                if (!info.IsAlive(entity))
                    return false;

                info.Archetype = archetype;
                info.ArchetypeIndex = archetypeIndex;
                return true;
            }
            return false;
        }

        #endregion Changed

        #region Find And Get

        /// <summary>
        /// 检查指定实体信息是否存活。
        /// </summary>
        /// <param name="entity">要检查的实体句柄。</param>
        /// <returns>存活返回 true；否则返回 false。</returns>
        public bool IsAlive(Entity entity)
        {
            if (entity.IsEmpty)
                return false;

            if (TryFindSegmentForEntity(entity, out var current, out var localIndex))
                return current.GetEntityInfo(localIndex).IsAlive(entity);

            return false;
        }

        /// <summary>
        /// 尝试获取指定实体所属的 Archetype 及在 Archetype 中的索引。
        /// </summary>
        /// <param name="entity">要查询的实体句柄。</param>
        /// <param name="archetype">当返回 true 时，输出实体所属的 Archetype。</param>
        /// <param name="archetypeIndex">当返回 true 时，输出实体在 Archetype 中的索引。</param>
        /// <returns>找到且实体存活时返回 true；否则返回 false。</returns>
        public bool TryGetArchetype(Entity entity, out Archetype? archetype, out int archetypeIndex)
        {
            archetype = null!;
            archetypeIndex = -1;
            if (entity.IsEmpty)
                return false;

            if (TryFindSegmentForEntity(entity, out var entitySegment, out var localIndex))
            {
                ref var info = ref entitySegment.GetEntityInfo(localIndex);
                if (info.IsAlive(entity))
                {
                    archetype = info.Archetype;
                    archetypeIndex = info.ArchetypeIndex;
                    return true;
                }
            }
            return false;
        }

        /// <summary>
        /// 在有序段集合中查找包含指定实体的段，并返回对应段与段内局部索引。 使用二分查找快速定位可能包含指定全局 Id 的段。
        /// </summary>
        private bool TryFindSegmentForEntity(Entity entity, [NotNullWhen(true)] out EntitySegment entitySegment, out int localIndex)
        {
            entitySegment = null!;
            localIndex = -1;
            int globalIndex = entity.Id;

            // 二分查找：找到最大的 startIndex <= globalIndex
            var keys = _segments.Keys;
            int lo = 0, hi = keys.Count - 1, best = -1;
            while (lo <= hi)
            {
                int mid = (lo + hi) >> 1;
                int start = keys[mid];
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

            var seg = _segments.Values[best];
            if (seg.TryGetLocalIndex(entity, out localIndex))
            {
                entitySegment = seg;
                return true;
            }

            return false;
        }

        #endregion Find And Get

        /// <summary>
        /// 获取结构体枚举器（foreach 优先走该路径，避免装箱）。
        /// </summary>
        /// <returns>结构体枚举器。</returns>
        public AliveEntityEnumerator GetEnumerator()
        {
            return new AliveEntityEnumerator(this);
        }

        #region Enumerable

        /// <summary>
        /// 存活实体信息的结构体可枚举序列。
        /// </summary>
        public readonly struct AliveEntityEnumerable
        {
            private readonly EntityManager _manager;

            internal AliveEntityEnumerable(EntityManager manager)
            {
                _manager = manager;
            }

            /// <summary>
            /// 获取结构体枚举器。
            /// </summary>
            /// <returns>结构体枚举器。</returns>
            public AliveEntityEnumerator GetEnumerator() => new(_manager);
        }

        /// <summary>
        /// 存活实体信息的结构体枚举器。
        /// </summary>
        public struct AliveEntityEnumerator : IEnumerator<EntityInfo>
        {
            private readonly EntityManager _manager;

            /// <summary>
            /// 当前正在遍历的实体段引用。
            /// </summary>
            private EntitySegment? _currentSegment;

            /// <summary>
            /// 当前遍历段内的局部索引（用于 TryGetNextAliveEntity 传入/返回位置）。
            /// </summary>
            private int _localIndex;

            /// <summary>
            /// 枚举器锁定标志（目前保留以便未来扩展）。
            /// </summary>
            private bool _lockTaken;

            internal AliveEntityEnumerator(EntityManager manager)
            {
                _manager = manager;
                _currentSegment = manager._segments.Values[0];
                _localIndex = -1;
                _lockTaken = false;
                Current = EntityInfo.Empty;
            }

            /// <summary>
            /// 当前实体信息。
            /// </summary>
            public EntityInfo Current { get; private set; }

            object IEnumerator.Current => Current;

            /// <summary>
            /// 移动到下一个存活实体信息。
            /// </summary>
            /// <returns>存在下一个实体信息返回 true；否则返回 false。</returns>
            public bool MoveNext()
            {
                while (_currentSegment is not null)
                {
                    if (_currentSegment.TryGetNextAliveEntity(ref _localIndex, out var entityInfo))
                    {
                        Current = entityInfo;
                        return true;
                    }

                    // 移动到下一段并重置局部索引
                    var segList = _manager._segments.Values;
                    int idx = Array.IndexOf(segList.ToArray(), _currentSegment);
                    idx++;
                    _currentSegment = idx < segList.Count ? segList[idx] : null;
                    _localIndex = -1;
                }

                Current = EntityInfo.Empty;
                return false;
            }

            /// <summary>
            /// 将枚举器重置到初始位置。
            /// </summary>
            public void Reset()
            {
                _currentSegment = _manager._segments.Values[0];
                _localIndex = -1;
                Current = EntityInfo.Empty;
            }

            /// <summary>
            /// 释放枚举器占用的锁（若有）。
            /// </summary>
            public void Dispose()
            {
                _lockTaken = false;
            }
        }

        #endregion Enumerable

        #region EntitySegment

        /// <summary>
        /// 实体段：管理一段连续实体索引的版本与分配状态。
        /// </summary>
        [DebuggerDisplay("StartIndex={StartIndex}, AliveCount={AliveCount}")]
        private sealed class EntitySegment
        {
            private readonly EntityInfo[] _entityInfos;

            /// <summary>
            /// 空闲局部索引栈，按 LIFO 管理可重用的槽位。
            /// </summary>
            private readonly Stack<int> _freeStack;

            /// <summary>
            /// 段起始全局索引（用于计算实体的全局 Id）。
            /// </summary>
            public int StartIndex { get; set; }

            /// <summary>
            /// 获取当前段内存活实体数量。
            /// </summary>
            public int AliveCount => DefaultSegmentSize - _freeStack.Count;

            /// <summary>
            /// 使用指定起始索引初始化实体段。
            /// </summary>
            /// <param name="startIndex">段起始全局索引。</param>
            /// <param name="maxSize">段最大大小。</param>
            public EntitySegment(int startIndex, int maxSize)
            {
                CreateArray(maxSize, out _entityInfos);

                _freeStack = new(maxSize);
                StartIndex = startIndex;

                // 将所有有效的局部索引（0..maxSize-1）压入空闲栈，供后续分配使用
                for (var localIndex = maxSize - 1; localIndex >= 0; localIndex--)
                    _freeStack.Push(localIndex);
            }

            /// <summary>
            /// 创建指定长度的数组，并通过输出参数返回。
            /// </summary>
            /// <typeparam name="T">数组元素类型。</typeparam>
            /// <param name="length">数组长度。</param>
            /// <param name="array">返回创建的数组实例。</param>
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            private void CreateArray<T>(int length, out T[] array) => array = new T[length];

            /// <summary>
            /// 尝试从当前段分配一个实体槽位索引。
            /// </summary>
            /// <param name="localIndex">分配成功时返回段内局部索引。</param>
            /// <returns>分配成功返回 true；否则返回 false。</returns>
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public bool TryRentEntityInfoIndex(out int localIndex) => _freeStack.TryPop(out localIndex);

            /// <summary>
            /// 尝试从当前段批量分配实体槽位索引。
            /// </summary>
            /// <param name="localIndex">用于接收局部索引的 Span。</param>
            /// <param name="count">实际分配成功的数量（输出）。</param>
            /// <returns>当请求全部分配成功时返回 true；若在过程中遇到不足则返回 false 并中断。</returns>
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public bool TryRentEntityInfoIndexs(Span<int> localIndex, out int count)
            {
                count = 0;
                for (int i = 0; i < localIndex.Length; i++)
                {
                    if (_freeStack.TryPop(out int index))
                    {
                        localIndex[i] = index;
                        count++;
                    }
                    else
                        return false; // 返回实际分配的数量
                }
                return true; // 返回实际分配的数量
            }

            /// <summary>
            /// 回收指定局部索引的实体，更新版本并将槽位返回空闲栈。
            /// </summary>
            /// <param name="localIndex">要回收的段内局部索引。</param>
            /// <param name="version">待校验的实体版本。</param>
            /// <param name="changedEntity">如果回收成功且实体关联了 Archetype，则输出被修改的实体句柄；否则返回空实体。</param>
            /// <param name="archetypeIndex">如果回收成功且实体关联了 Archetype，则输出被修改的实体在 Archetype 中的索引；否则返回 -1。</param>
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public void ReturnEntity(int localIndex, uint version, out Entity changedEntity, out int archetypeIndex)
            {
                ref var info = ref _entityInfos[localIndex];
                changedEntity = Entity.Empty;
                archetypeIndex = -1;
                // 实体已被销毁或版本不匹配，忽略回收请求
                if (info.Id == 0 || info.Version != version)
                    return;

                info.Archetype?.TryRemoveEntity(info.ArchetypeIndex, out changedEntity);
                archetypeIndex = info.ArchetypeIndex;
                info.Archetype = default!;
                info.Version++;
                _freeStack.Push(localIndex);
            }

            /// <summary>
            /// 以引用形式获取指定段内局部索引对应的 EntityInfo，以便直接读取或写入。
            /// </summary>
            /// <param name="localIndex">段内局部索引。</param>
            /// <returns>对应的 <see cref="EntityInfo" /> 的引用。</returns>
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public ref EntityInfo GetEntityInfo(int localIndex) => ref _entityInfos[localIndex];

            /// <summary>
            /// 从指定局部索引之后查找下一个存活的实体信息（供枚举器使用）。
            /// </summary>
            /// <param name="localIndex">当前局部索引，成功时更新为命中索引。</param>
            /// <param name="entityInfo">找到的实体信息（输出）。</param>
            /// <returns>找到返回 true，否则返回 false。</returns>
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public bool TryGetNextAliveEntity(ref int localIndex, out EntityInfo entityInfo)
            {
                for (var i = localIndex + 1; i < _entityInfos.Length; i++)
                {
                    ref var info = ref _entityInfos[i];
                    if (!info.IsEmpty)
                    {
                        localIndex = i;
                        entityInfo = info;
                        return true;
                    }
                }

                entityInfo = EntityInfo.Empty;
                return false;
            }

            /// <summary>
            /// 尝试将全局索引转换为段内局部索引。 局部索引为 0-based（对应数组下标）。
            /// </summary>
            /// <param name="entity">要获取原型的实体句柄。</param>
            /// <param name="localIndex">转换成功时返回局部索引。</param>
            /// <returns>转换成功返回 true；否则返回 false。</returns>
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public bool TryGetLocalIndex(Entity entity, out int localIndex)
            {
                int globalIndex = entity.Id;
                localIndex = globalIndex - StartIndex;
                return (uint)localIndex < (uint)DefaultSegmentSize;
            }
        }

        #endregion EntitySegment
    }
}